using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows.Documents;
using CefSharp.DevTools.CSS;


namespace SiteWatcher.Plugins{

    public class DataPlugin : IPlugin{
        private Watch? _watch;
        private static PluginParams _params = new();
        private PluginParams _itemparams = new();
        public static PluginParams Params {get=>_params;}
        public PluginParams ItemParams {get=>_itemparams;}

        private static Dictionary<string,Func<Watch,string>> defaultDataKeys = new(){
            {"status",(w)=>(new WatchStatusToStringConverter()).Convert(w.Status,typeof(string),"",CultureInfo.CurrentCulture) as string??""},
            {"name",(w)=>w.Name},
            {"url",(w)=>w.Source.Url},
            {"content",(w)=>w.Diff.Next.Text},
            {"comment",(w)=>w.Comment},
            {"error",(w)=>w.Error},
            {"tags",(w)=>string.Join(", ",w.Tags.Select(t=>t.Name))},
            {"changed",(w)=>{
                List<DiffPart> compare = DiffComparer.CompareStrings(w.Diff.Prev.Text,w.Diff.Next.Text,oneLevel:true);
                return string.Join("\n",compare.Where(d=>d.Type==DiffType.Add || d.Type==DiffType.Change).Select(d=>d.Text));
            }},
            {"deleted",(w)=>{
                List<DiffPart> compare = DiffComparer.CompareStrings(w.Diff.Prev.Text,w.Diff.Next.Text,oneLevel:true);
                return string.Join("\n",compare.Where(d=>d.Type==DiffType.Delete).Select(d=>d.Text));
            }}
        };

        public void Init(SiteWatcher.Watch item){
            _watch=item;
        }
        public void OnDataPrepare(in PluginParamsData itemParams, PluginParamsData data){
            if (_watch==null) return;
            foreach(KeyValuePair<string,Func<Watch,string>> kv in defaultDataKeys){
                data[kv.Key]=kv.Value(_watch);
            };
        }

    }
}