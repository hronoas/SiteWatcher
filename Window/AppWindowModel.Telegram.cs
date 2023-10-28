using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Media;

namespace SiteWatcher
{
    public partial class AppWindowModel : BaseWindowModel<AppWindow>{
        private const string defaultTelegramTemplate = "{error=}✅{/error}{error!=}❌{/error} <a href=\"{url}\">{name}</a>\n{error=}{changed}{/error}{error!=}⚠️ {error}{/error}";
        private TelegramConfig telegram = new (){Template=defaultTelegramTemplate};

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
        
        public static string AvailableTelegramReplace {get;set;} = "Доступные замены: "+string.Join(", ",defaultDataKeys.Select(d=>"{"+d.Key+"}"))+"\n"+
                                                                    "Проверкой значений: {status=Новое}ВНИМАНИЕ{/status} - выведет 'ВНИМАНИЕ' при статусе 'Новое'\n"+
                                                                    "Операторы сравнения:\n'=' - совпадает\n'!=' - не совпадает\n'~' - содержит\n'!~' - не содержит";

        public void SendTelegram(Watch watch){

            string template = string.IsNullOrEmpty(watch.TelegramTemplate)?telegram.Template:watch.TelegramTemplate;
            if(!string.IsNullOrEmpty(watch.Error) && !template.Contains("{error}")) return;

            Dictionary<string,string> data = new();
            foreach(KeyValuePair<string,Func<Watch,string>> kv in defaultDataKeys){
                data.Add(kv.Key,kv.Value(watch));
            };

            TelegramNotify.SendMessageAsync(telegram,data:data, text_template:watch.TelegramTemplate, chatId:watch.TelegramChat);
        }
    }

}