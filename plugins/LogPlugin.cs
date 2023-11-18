using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows.Documents;
using CefSharp.DevTools.CSS;


namespace SiteWatcher.Plugins{

    public class LogPlugin2: LogPlugin{}

    public class LogPlugin : IPlugin{
        private static PluginParams _params = new(){
            {"Global property",new PluginParam("Global property value","Test property fill it",false)},
            {"HideGlobalProperty",new PluginParam("Hiden global prefix value","Hidden prop",true)},
        };
        private PluginParams _itemparams = new(){
            {"Prefix",new PluginParam("Prefix value","Prefix for log output",false)},
            {"HidePrefix",new PluginParam("Hiden prefix value","Hidden prefix for logs",true)},
        };
        public static PluginParams Params {get=>_params;}
        public PluginParams ItemParams {get=>_itemparams;}

        public void Init(SiteWatcher.Watch item){
            Log($"OnInit {item.Name}");
        }
        public void OnDataPrepare(in PluginParamsData itemParams, PluginParamsData data){
            Log($"OnDataPrepare ({itemParams["Prefix"]}) {data}");
        }
        public void OnNotify(in PluginParamsData itemParams, PluginParamsData data){
            Log($"OnNotify ({itemParams["Prefix"]}) {data}");
        }
        public void OnError(in PluginParamsData itemParams, PluginParamsData data){
            Log($"OnError ({itemParams["Prefix"]}) {data}");
        }

    }
}