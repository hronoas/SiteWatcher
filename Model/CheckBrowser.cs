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
using System.Security.Cryptography.X509Certificates;
using System.Windows.Documents;
using System.Configuration;

namespace SiteWatcher
{
    public class CheckBrowser{

        public static CefSettings GetSettingsDefault(bool useProxy=false){
            var settings = new CefSettings(){
                CachePath = AppCache+(useProxy?"\\proxy":"")
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
            return settings;
        }
        public static void Init(){
#if ANYCPU
            //Only required for PlatformTarget of AnyCPU
            CefRuntime.SubscribeAnyCpuAssemblyResolver();
#endif
            var settings = GetSettingsDefault();

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
        private static List<Task> tasks = new();

        public static ProxyServer proxy = new();

        public class CheckItem{
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

        public static bool SetUseProxy(IRequestContext rc, bool useProxy){ //use proxy if useProxy and server is set
            rc.GetAllPreferences(true);
            var dict = new Dictionary<string, object>();
            if (useProxy && !String.IsNullOrWhiteSpace(proxy.server)){
                dict.Add("mode", "fixed_servers");
                dict.Add("server", proxy.server);
            }else{
                dict.Add("mode", "direct");
            }
            string error2;
            bool success = rc.SetPreference("proxy", dict, out error2);
            if (!success){
                Console.WriteLine("something happen with the prerence set up" + error2);
            }
            return success;
        }

        public static RequestContextSettings GetContextSettingsDefault(bool useProxy=false){ //change CachePath if useProxy
            RequestContextSettings settings = new()
            {
                CachePath = AppCache + (useProxy ? "\\proxy" : ""),                
            };
            

            return settings;
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
            //var rc = new RequestContext(Cef.GetGlobalRequestContext());
            bool needProxy = item.SourceWatch.UseProxy && !String.IsNullOrWhiteSpace(proxy.server);
            var rc = new RequestContext(GetContextSettingsDefault(item.SourceWatch.UseProxy));
            
            using (var browser = new ChromiumWebBrowser(Source.Referer==""?Source.Url:Source.Referer, browserSettings,requestContext:rc)){
                if (needProxy){
                    await Cef.UIThreadTaskFactory.StartNew(delegate{
                            SetUseProxy(rc,true);
                    });
                    browser.RequestHandler = new ProxyHandler(proxy.user, proxy.password);
                }else{
                    await Cef.UIThreadTaskFactory.StartNew(delegate{
                            SetUseProxy(rc,false);
                    });
                }

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
                                        if(selector.Filter!="") r.Text=selector.FilterData(Source.CheckData?r.Data:r.Text);
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

    public class ProxyServer {
        public string server {get;set;}
        public string user {get;set;}
        public string password {get;set;}

        // Default constructor with default values
        public ProxyServer() {
            this.server = "";
            this.user = "";
            this.password = "";
        }

        // Parameterized constructor
        public ProxyServer(string server, string user, string password) {
            this.server = server;
            this.user = user;
            this.password = password;
        }
        public ProxyServer Clone() {
            return (ProxyServer)MemberwiseClone();
        }
    }
    public class ProxyHandler : IRequestHandler{
        private string userName;
        private string password;
        public ProxyHandler(string userName, string password){
            this.userName = userName;
            this.password = password;
        }

        bool IRequestHandler.OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect){
            return false;
        }
        bool IRequestHandler.OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture){
            return OnOpenUrlFromTab(browserControl, browser, frame, targetUrl, targetDisposition, userGesture);
        }
        protected virtual bool OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture){
            return false;
        }
        bool IRequestHandler.OnCertificateError(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback){
            return false;
        }
        bool IRequestHandler.GetAuthCredentials(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback){
            if (isProxy == true){
                callback.Continue(userName, password);
                return true;
            }
            return false;
        }
        public bool OnSelectClientCertificate(IWebBrowser browserControl, IBrowser browser, bool isProxy, string host, int port,
            X509Certificate2Collection certificates, ISelectClientCertificateCallback callback){
            return false;
        }
        void IRequestHandler.OnRenderProcessTerminated(IWebBrowser browserControl, IBrowser browser, CefTerminationStatus status){}
        bool IRequestHandler.OnQuotaRequest(IWebBrowser browserControl, IBrowser browser, string originUrl, long newSize, IRequestCallback callback){
            return false;
        }
        public void OnResourceRedirect(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request,
            IResponse response, ref string newUrl){
        }
        void IRequestHandler.OnRenderViewReady(IWebBrowser browserControl, IBrowser browser){}
        void IRequestHandler.OnDocumentAvailableInMainFrame(IWebBrowser chromiumWebBrowser, IBrowser browser){}
        IResourceRequestHandler IRequestHandler.GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling){
            return null;
        }
    }
}