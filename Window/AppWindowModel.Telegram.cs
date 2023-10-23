using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Media;

namespace SiteWatcher
{
    public partial class AppWindowModel : BaseWindowModel<AppWindow>{
        private const string defaultTelegramTemplate = "{name} ({status})\n{url}\n{content}{error}";
        private TelegramConfig telegram = new (){Template=defaultTelegramTemplate};
        

        public void SendTelegram(Watch watch){
            Dictionary<string,string> data = new(){
                {"status",(new WatchStatusToStringConverter()).Convert(watch.Status,typeof(string),"",CultureInfo.CurrentCulture) as string??""},
                {"name",watch.Name},
                {"url",watch.Source.Url},
                {"content",string.IsNullOrWhiteSpace(watch.Error)?watch.Diff.Next.Text:""},
                {"comment",watch.Comment},
                {"error",watch.Error},
                {"tags",string.Join(", ",watch.Tags.Select(t=>t.Name))}
            };
            TelegramNotify.SendMessageAsync(telegram,data:data,chatId:watch.TelegramChat);
        }
    }

}