using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SiteWatcher{
    public class WatchSource:PropertyChangedBase,ICloneable{
        public string Url { get=>url; set=>SetField(ref url, value);}
        private string url="";
        public string Referer { get=>referer; set=>SetField(ref referer, value);}
        private string referer = "";
        public bool SimulateMouse { get=>simulateMouse; set=>SetField(ref simulateMouse, value);}
        private bool simulateMouse;
        private readonly TimeSpan maxWaitTimout = new(0,2,0);
        public TimeSpan WaitTimeout { get=>waitTimeout; set=>SetField(ref waitTimeout, value>maxWaitTimout?maxWaitTimout:value);}
        private TimeSpan waitTimeout = new TimeSpan(0,0,3);
        public BindingList<SourceSelector> Select { get; set;} = new(){};
        public bool CheckData { get=>checkData; set=>SetField(ref checkData, value);}
        private bool checkData = false;

        public object Clone(){
            WatchSource clone=(WatchSource)MemberwiseClone();
            clone.Select = new(Select.ToList().Select(x=>(x.Clone() as SourceSelector)??new SourceSelector("")).ToList());
            return clone;
        }
    }
    public class SelectorResult:PropertyChangedBase{
        public string Text { get=>text; set=>SetField(ref text, value);}
        private string text = "";
        public string Data { get=>data; set=>SetField(ref data, value);}
        private string data = "";

    }
    public enum SourceSelectorType{XPath=0,CSS=1 /* ,JavaScript=2 */}
    
    public class SourceSelector:PropertyChangedBase,ICloneable{
        public string Value { get=>sValue; set=>SetField(ref sValue, value);}
        private string sValue = "";
        public SourceSelectorType Type { get=>type; set=>SetField(ref type, value);}
        private SourceSelectorType type = SourceSelectorType.XPath;
        public string Filter { get=>filter; set=>SetField(ref filter, value);}
        private string filter="";
        public string Replace { get=>replace; set=>SetField(ref replace, value);}
        private string replace ="";
        public SourceSelector(string value, SourceSelectorType type = SourceSelectorType.XPath, string filter = ""){
            Value = value;
            Type = type;
            Filter = filter;
        }

        public string FilterData(string data){
            string result = data;
            if(!String.IsNullOrWhiteSpace(filter)){
                try {
                    result="";
                    Regex regex = new Regex(filter,RegexOptions.Singleline|RegexOptions.IgnoreCase);
                    foreach (Match match in regex.Matches(data)){
                        if(replace!=""){
                            string replaced = replace;
                            foreach (string groupName in regex.GetGroupNames()){
                                if(match.Groups[groupName].Captures.Count>0){
                                    replaced=replaced.Replace("{"+groupName+"}",match.Groups[groupName].Value);
                                }
                            }
                            result+=replaced;
                        }else{
                            string replaced = "";
                            for (var i = match.Groups.Count>1?1:0; i < match.Groups.Count; i++){
                                replaced+=(replaced.Length!=0?" ":"") + match.Groups[i].Value;
                            }
                            result+="\n"+replaced;
                        }
                    }
                } catch{
                }
            }
            return result;
        }

        public string ToScript(){
            switch (Type){
                case SourceSelectorType.XPath:
                    return @"var result = [];
                        if(!Array.isArray(parameters)) parameters = [parameters];
                        parameters.forEach(element => {
                            var items = document.evaluate(element, document, null, 0, null);
                            var item = items.iterateNext();
                            while (item) {
                                result.push({
                                    Text:item.innerText,
                                    Data:item.outerHTML,
                                });
                                item = items.iterateNext();
                            }
                        });
                        return result;";
                case SourceSelectorType.CSS:
                    return @"var result = [];
                        if(!Array.isArray(parameters)) parameters = [parameters];
                        parameters.forEach(element => {
                            let nodeList = document.querySelectorAll(element);
                            for (let i = 0; i < nodeList.length; i++) {
                                result.push({
                                    Text:nodeList[i].innerText,
                                    Data:nodeList[i].outerHTML,
                                });
                            }
                        });
                        return result;";
/*                 case SourceSelectorType.JavaScript:
                    return Value; */
                default:
                return @"var result = [];
                        return result;";
            }
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

}