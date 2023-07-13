using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CefSharp;
using System.Linq;
using CefSharp.Enums;
using CefSharp.Handler;
using CefSharp.OffScreen;
using CefSharp.Structs;

namespace SiteWatcher
{
    public class CheckBrowser{
        public static void Init(){
#if ANYCPU
            //Only required for PlatformTarget of AnyCPU
            CefRuntime.SubscribeAnyCpuAssemblyResolver();
#endif
            var settings = new CefSettings(){
                CachePath = AppCache
            };

            //settings.BrowserSubprocessPath = @"runtimes\win-x64\native\CefSharp.BrowserSubprocess.exe";

            settings.CefCommandLineArgs.Add("enable-media-stream", "0");
            settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");
            settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");
            
            //settings.CefCommandLineArgs.Add("enable-media-stream");
            settings.CefCommandLineArgs.Add("disable-gpu", "1");
            settings.CefCommandLineArgs.Remove("enable-system-flash");
            settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling", "1");
            settings.CefCommandLineArgs.Add("disable-gpu-vsync", "1");
            //settings.CefCommandLineArgs.Add("mute-audio", "true");
            settings.CefCommandLineArgs.Add("disable-3d-apis", "1");
            settings.CefCommandLineArgs.Add("renderer-process-limit", "10");
            settings.CefCommandLineArgs.Add("js-flags", "--lite_mode");
            //settings.CefCommandLineArgs.Add("disable-image-loading", "1");
            settings.LogFile = AppLog;
            settings.LogSeverity = CefSharp.LogSeverity.Error;
            settings.IgnoreCertificateErrors = true;
            settings.SetOffScreenRenderingBestPerformanceArgs();

            if (!CefSharp.Cef.IsInitialized){
                CefSharp.Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            } 
/*             GC.KeepAlive(Q);
            GC.KeepAlive(tasks); */
        }

        public static void DeInit(){
            if (CefSharp.Cef.IsInitialized){
                CefSharp.Cef.Shutdown();
            }
        }

        private static List<CheckItem> Q = new();
        public static int parallelTasks = 3;
        private static List<Task> tasks =new();

        public class CheckItem
        {
            public Watch SourceWatch {get;set;}
            public Action<List<SelectorResult>>? onData;
            public Action<string>? onError;
            public Action<List<SelectorResult>,string>? onFinally;

            public CheckItem(Watch sourceWatch, Action<List<SelectorResult>>? onData, Action<string>? onError, Action<List<SelectorResult>, string>? onFinally)
            {
                SourceWatch = sourceWatch;
                this.onData = onData;
                this.onError = onError;
                this.onFinally = onFinally;
            }

            public static bool operator ==(CheckItem? i1,CheckItem? i2){
                return i1?.SourceWatch==i2?.SourceWatch;
                /* && i1?.onData==i2?.onData 
                && i1?.onError==i2?.onError 
                && i1?.onFinally==i2?.onFinally; */
            }
            public static bool operator !=(CheckItem i1,CheckItem i2) => !(i1==i2);

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (ReferenceEquals(obj, null))
                {
                    return false;
                }

                return this==(obj as CheckItem);
            }
        }

        private static Task createTask(){
            return Task.Run(async ()=>{
                    while(Q.Count>0){
                        CheckItem? i = Q[0];
                        Q.RemoveAt(0);
                        if(i!=null)i.SourceWatch.IsQueued=false;
                        await CheckAsync(i);
                    }
            });
        }
        private static void RunTasks(){
            for (var i = 0; i < parallelTasks; i++){
                if(i>(tasks.Count-1)) tasks.Add(createTask());
                else if(tasks[i]?.IsCompleted??true) tasks[i]=createTask();
            }
        }
        public static void Check(Watch sourceWatch, Action<List<SelectorResult>>? onData, Action<string>? onError,Action<List<SelectorResult>,string>? onFinally){
            CheckItem newCheck = new(sourceWatch,onData, onError, onFinally);
            if(Q.Contains(newCheck)){
                if(Q[0]!=newCheck){
                    newCheck.SourceWatch.IsQueued=false;
                    Q.Remove(newCheck);
                    newCheck.SourceWatch.IsQueued=true;
                    Q.Insert(0,newCheck);
                }
            }else{
                newCheck.SourceWatch.IsQueued=true;
                Q.Add(newCheck);
            }
            RunTasks();
        }

        public static void Dequeue(Watch sourceWatch){
            Q.Where((q)=>q.SourceWatch==sourceWatch).ToList().ForEach((q)=>{
                q.SourceWatch.IsQueued=false;
                Q.Remove(q);
            });
        }

        public static async Task simulateMovementAsync(ChromiumWebBrowser browser){
            var wbHost = browser.GetBrowser().GetHost();
            wbHost.SendMouseMoveEvent(145, 251, false, CefEventFlags.None);
            await Task.Delay(300);
            wbHost.SendMouseMoveEvent(190, 625, false, CefEventFlags.None);
            await Task.Delay(300);
            wbHost.SendMouseWheelEvent(10,10,0,1000,CefEventFlags.None);
            await Task.Delay(3000);
        }
        public static async Task CheckAsync(CheckItem? item){
            if(item==null) return;
            item.SourceWatch.IsChecking=true;
            string errors = "";
            List<SelectorResult> results = new();
            WatchSource Source = item.SourceWatch.Source;
            if(Cef.ParseUrl(Source.Url)==null){
                errors+="wrong url";
                item.SourceWatch.IsChecking=false;
                item.onError?.Invoke(errors);
                item.onFinally?.Invoke(results,errors);
                return;
            }
            var browserSettings = new BrowserSettings{
                WindowlessFrameRate = 1
            };
            TimeSpan timeout = new TimeSpan(0,0,15);
            using (var browser = new ChromiumWebBrowser(Source.Referer==""?Source.Url:Source.Referer, browserSettings)){
                await browser.WaitForInitialLoadAsync();
                if(Source.SimulateMouse) await simulateMovementAsync(browser);
                int waitTimeout =  Math.Max(Math.Min((int)Source.WaitTimeout.TotalMilliseconds,2*60000),0); //Max 2 minutes
                if(Source.Referer=="") await Task.Delay(waitTimeout);
                else {
                    int tryes = 5;
                    while(tryes>0){
                        try{
                            await browser.EvaluateScriptAsync($"window.location=\"{Source.Url}\";",timeout);
                            tryes=0;
                        }catch{
                            tryes-=1;
                            await Task.Delay(500);
                        }
                    }
                    await browser.WaitForInitialLoadAsync();
                    if(Source.SimulateMouse) await simulateMovementAsync(browser);
                    await Task.Delay(waitTimeout);
                }
                await Cef.UIThreadTaskFactory.StartNew(()=>{});
                var onUi = Cef.CurrentlyOnThread(CefThreadIds.TID_UI);

                if(browser.CanExecuteJavascriptInMainFrame){
                    for (var i = 0; i < Source.Select.Count; i++){
                        var selector = Source.Select[i];
                        string jsCode=@"JSON.stringify((function(parameters){"+
                        selector.ToScript()+
                        "})("+Serialize(selector.Value)+"))";
                        try{
                            var response = await browser.EvaluateScriptAsync(jsCode,timeout);
                            if(!response.Success){
                                errors+=$"wrong selector {selector.Value}\n";
                            }else{
                                if (response.Result != null){
                                    List<SelectorResult> res = Deserialize<List<SelectorResult>>(response.Result.ToString()??"[]")??new List<SelectorResult>();
                                    res.ForEach(r=>{
                                        if(selector.Filter!="") r.Text=selector.FilterData(r.Data);
                                        results.Add(r);
                                    });
                                }else{
                                    errors+="empty response";
                                }
                            }
                        }catch (System.Exception ex){
                            errors+="eval timout: "+ex.Message+"";
                        }
                    }
                }else{
                    errors+="can't load page";
                }
                item.SourceWatch.IsChecking=false;
                if(!String.IsNullOrEmpty(errors)) item.onError?.Invoke(errors);
                else if(results.Count==0){
                    errors+="empty result";
                    item.onError?.Invoke(errors);
                }
                if(results.Count>0) item.onData?.Invoke(results);
                item.onFinally?.Invoke(results,errors);
            }
        }


        public static async Task SaveIconAsync(string url,string filename){
            UrlParts parts = Cef.ParseUrl(url);
            if(parts==null) return;
            try {
                HttpClient client = new HttpClient();
                using(HttpResponseMessage resp =  await client.GetAsync($"{parts.Origin}favicon.ico"))
                using (var fileStream = File.Create(filename))
                    {
                        resp.Content.ReadAsStream().CopyTo(fileStream);
                        fileStream.Flush();
                    }
            } catch {
            }
        }
    }
}