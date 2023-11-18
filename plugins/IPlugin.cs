using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Documents;
using CefSharp.DevTools.CSS;


namespace SiteWatcher.Plugins{

    public enum ParamType{
        String = 1,
        Boolean = 2,
        DateTime = 3,
        Text = 4,
        Integer = 5
    }

    public class PluginParam{
        public string Value {get;set;}
        [JsonIgnore]
        public bool Hidden {get;set;} = false;
        [JsonIgnore]
        public string Help {get;set;} = "";
        [JsonIgnore]
        public ParamType Type {get;set;} = ParamType.String;
        public PluginParam(string value, string help="", bool hidden = false){
            Value = value;
            Help = help;
            Hidden = hidden;
        }
    }
    public class PluginParamsData:Dictionary<string, string>{}

    public class PluginParams:Dictionary<string, PluginParam>{
        public PluginParamsData ToData(){
            PluginParamsData data = new();
            foreach(var param in this)
                data.Add(param.Key,param.Value.Value);
            return data;
        }
    }
    public interface IPlugin{
        public static PluginParams? Params { get; }
        public PluginParams ItemParams { get; }
        public void Init(Watch item) => DoNothing();
        public void OnNotify(in PluginParamsData itemParams, PluginParamsData Data) => DoNothing();
        public void OnError(in PluginParamsData itemParams, PluginParamsData Data) => DoNothing();
        public void OnDataPrepare(in PluginParamsData itemParams, PluginParamsData Data) => DoNothing();
        private static void DoNothing(){}
    }

}