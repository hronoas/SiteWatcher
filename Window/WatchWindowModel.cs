using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using CefSharp;
using CefSharp.DevTools.CacheStorage;
using CefSharp.Wpf;

namespace SiteWatcher
{
    public class WatchWindowModel : BaseWindowModel<WatchWindow>
    {
        public Watch Item {get;set;} = new();
        public SortableBindingList<WatchTag> Tags {get;set;} = new(new List<WatchTag>());
        private ChromiumWebBrowser webBrowser;
        public Command<string> UrlOpenCommand {get;set;}
        public Command<string> UrlUpdateCommand {get;set;}
        public Command SaveCommand {get;set;}
        public Command CancelCommand {get;set;}
        public Command SelectAddCommand {get;set;}
        public Command BeginSelectCommand {get;set;}
        public bool Selecting { get=>selecting; set=>SetField(ref selecting, value);}
        private bool selecting = false;
        private bool ignoreRedirect = false;
        public Command<SourceSelector> SelectDeleteCommand {get;set;}

        public Command UpdateTextBoxCommand {get;set;}
        
        private string jsObjectName = "callBackJS";

        public Command CloseWindowCommand {get;set;}
        public WatchWindowModel(Watch Source,List<WatchTag> alltags,WatchWindow win, bool ignoreFirstRedirect=false) : base(win){
            Item=(Watch)Source.Clone();
            Item.Source.Select.ListChanged+=(o,e)=>SelectAll();
            if(string.IsNullOrWhiteSpace(Item.Source.Url)){
                string clipboard = System.Windows.Clipboard.GetText();
                try{
                    Uri uri = new Uri(clipboard);
                    Item.Source.Url=clipboard;
                }catch{
                }
            }
            ignoreRedirect=ignoreFirstRedirect;
            UrlOpenCommand=new((s)=>{ Item.Source.Referer=""; ignoreRedirect=true; UrlOpen(s);});
            UrlUpdateCommand=new((s)=>{ ignoreRedirect=true; UrlOpen(s);});
            SaveCommand=new(o=>{Save();});
            CancelCommand=new(o=>{window.DialogResult=false; window.Close();});
            SelectAddCommand=new(o=>Item.Source.Select.Add(new("")));
            SelectDeleteCommand=new(s=>SelectDelete(s));
            CloseWindowCommand = new(o=>win.Close());
            BeginSelectCommand = new(o=>BeginSelect());
            UpdateTextBoxCommand = new(o=>{
                DependencyProperty prop = TextBox.TextProperty;
                BindingExpression binding = BindingOperations.GetBindingExpression(o as DependencyObject, prop);
                if (binding != null) 
                    binding.UpdateSource();
            });

            alltags.Select(t=>t.Clone()).ToList().ForEach(t=>{
                t.Selected = Item.Tags.Where(it=>t.Name==it.Name).Count()>0;
                Tags.Add(t);
            });
            Item.Tags.Where(wt=>!Tags.Any(t=>t.Name==wt.Name)).ToList().ForEach(t=>Tags.Add(t));
            Tags.ListChanged+=(o,e)=>ChangedField(nameof(Tags));
            if (!string.IsNullOrWhiteSpace(Item.Source.Url)) win.Loaded+=(o,e)=>UrlOpen(Item.Source.Url);
        }

        private RequestContext createBrowser(){
            var rc = new RequestContext(CheckBrowser.GetContextSettingsDefault(Item.UseProxy));                    
            webBrowser = new ChromiumWebBrowser();
            Border border = (window.FindName("BrowserBorder") as Border);
            webBrowser.RequestContext = rc;
            border.Child=webBrowser;
            webBrowser.LifeSpanHandler = new MyCustomLifeSpanHandler();
            webBrowser.TitleChanged+=(o,s)=>{if(String.IsNullOrEmpty(Item.Name)) Item.Name = s.NewValue.ToString()??"";};
            webBrowser.JavascriptObjectRepository.Register(jsObjectName,new CallBackJS(s=>{EndSelect(s);}),options: BindingOptions.DefaultBinder);
            webBrowser.AddressChanged+=AddressChanged;
            webBrowser.FrameLoadEnd+=FrameLoaded;
            return rc;
        }

        private void AddressChanged(object sender, DependencyPropertyChangedEventArgs e){
            if(ignoreRedirect){
                ignoreRedirect=false;
                return;
            }
            if(Item.Source.Url!=(string)e.NewValue && Cef.ParseUrl(Item.Source.Url)!=null) Item.Source.Referer=Item.Source.Url;
            Item.Source.Url=(string)e.NewValue;
        }

        private void SelectDelete(SourceSelector? s){
            if(s==null) Item.Source.Select.Clear();
            else Item.Source.Select.Remove(s);
        }

        private async void FrameLoaded(object? sender, FrameLoadEndEventArgs e){
            if(e.Frame.IsMain){
                SelectAll();
                if(Selecting) BeginSelect();
            }
        }

        private void BeginSelect(){
            if(webBrowser==null) {
                MessageBox.Show("Откройте страницу перед выбором","Страница не загружена",MessageBoxButton.OK,MessageBoxImage.Error);
                return;
            }
            string script = ReadResource("inject_choose.js").ReadToEnd();  //H.Resources.inject_choose_js.AsString();
            string allscript = @"(async function() {"+
                $@"CefSharp.DeleteBoundObject('{jsObjectName}');
                CefSharp.RemoveObjectFromCache('{jsObjectName}');
                await CefSharp.BindObjectAsync('{jsObjectName}', 'bound');
                const callback={jsObjectName};"+
                script+
                @"})();";

            var isBound = webBrowser.JavascriptObjectRepository.IsBound(jsObjectName);
            if(isBound){
                if(webBrowser.CanExecuteJavascriptInMainFrame){ 
                    webBrowser.EvaluateScriptAsync(allscript);
                    Selecting=true;
                    return;
                }
                else MessageBox.Show("Дождитесь загрузки страницы.","Не удалось запустить скрипт",MessageBoxButton.OK,MessageBoxImage.Error);
            } 
            else MessageBox.Show("Не зарегистрирован объект обратного вызова","Ошибка",MessageBoxButton.OK,MessageBoxImage.Error);
            Selecting=false;
        }
        
        class jsSelector{
            public jsSelector(SourceSelector sel)
            {
                this.value = sel.Value;
                this.type = sel.Type.ToString();
            }

            public string value {get;set;}="";
            public string type {get;set;}="xpath";

        }
        private void SelectAll(){
            if(!webBrowser.CanExecuteJavascriptInMainFrame) return;
            List<jsSelector> list = new();
            Item.Source.Select.ToList().ForEach(selector=>{
                list.Add(new(selector));
            });
            string script = ReadResource("inject_select.js").ReadToEnd(); //H.Resources.inject_select_js.AsString();
            string allscript = @"(function(parameters){"+
                script+
                @"})("+Serialize(list)+")";
            if(webBrowser.CanExecuteJavascriptInMainFrame) webBrowser.EvaluateScriptAsync(allscript);
            else MessageBox.Show("Не удалось выбрать элементы через скрипт","Ошибка",MessageBoxButton.OK,MessageBoxImage.Error);
        }
        private void EndSelect(string selector){
            Selecting=false;
            window.Dispatcher.Invoke(()=>{
                Item.Source.Url=webBrowser.Address;
                Item.Source.Select.Add(new(selector,SourceSelectorType.XPath));
            });
        }
        private void Save(){
            Item.Tags.Clear();
            Tags.Where(t=>t.Selected??false).ToList().ForEach(t=>Item.Tags.Add(t));
            if(Item.Source.Select.Count==0){
                if(MessageBox.Show("Отслеживать всю страницу?","Не выбраны элементы для отслеживания",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.No) return;
                Item.Source.Select.Add(new("//body"));
            }
            window.DialogResult=true;
            window.Close();
        }
        async void UrlOpen(string Url){
            if(!String.IsNullOrWhiteSpace(Url)){
                
                if(!Url.ToLower().StartsWith("http")){
                    if(Regex.Match(Url,@"^[a-z0-9\.\-]+\.[a-z]{2,5}$",RegexOptions.IgnoreCase).Success) Url = "http://"+Url;
                    else Url = "https://www.google.com/search?q="+Url;
                } 
                Uri uri;
                try{
                    uri = new Uri(Url);
                    RequestContext rc = createBrowser();
                    await Cef.UIThreadTaskFactory.StartNew(delegate{
                        CheckBrowser.SetUseProxy(rc,Item.UseProxy);
                        webBrowser.RequestHandler = new ProxyHandler(CheckBrowser.proxy.user, CheckBrowser.proxy.password);
                    });
                    webBrowser.Load(Url);
                } catch (System.Exception ex) {
                    MessageBox.Show(ex.Message);
                }
            }else{
                MessageBox.Show("Пожалуйста, введите адрес страницы","Не введен адрес страницы для отслеживания",MessageBoxButton.OK,MessageBoxImage.Information);
            }
        }
    }

    public class MyCustomLifeSpanHandler : ILifeSpanHandler{
        public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser){
            browser.MainFrame.LoadUrl(targetUrl);
            newBrowser = null;
            return true;
        }
        
        public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser){
            // throw new NotImplementedException();
            return true;
        }

        public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser){
            // throw new NotImplementedException();
        }

        public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser){
            // throw new NotImplementedException();
        }
    }
}