using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SiteWatcher
{
    public partial class AppWindowModel : BaseWindowModel<AppWindow>
    {
        public static string AvailableWebhookReplace { get; set; } = "Доступные замены: " + string.Join(", ", defaultDataKeys.Select(d => "{" + d.Key + "}")) + "\n" +
                                                                     "Проверкой значений: {status=Новое}ВНИМАНИЕ{/status} - выведет 'ВНИМАНИЕ' при статусе 'Новое'\n" +
                                                                     "Операторы сравнения:\n'=' - совпадает\n'!=' - не совпадает\n'~' - содержит\n'!~' - не содержит";

        public void SendWebhook(Watch watch){

            var cfg = CurrentConfig.Webhook;
            string method = string.IsNullOrEmpty(watch.WebhookMethod) ? cfg.Method : watch.WebhookMethod;
            string url = string.IsNullOrEmpty(watch.WebhookUrl) ? cfg.Url : watch.WebhookUrl;
            string body = string.IsNullOrEmpty(watch.WebhookBody) ? cfg.Body : watch.WebhookBody;
            string headers = string.IsNullOrEmpty(watch.WebhookHeaders) ? cfg.Headers : watch.WebhookHeaders;
            string bodyType = string.IsNullOrEmpty(watch.WebhookBodyType) ? cfg.BodyType : watch.WebhookBodyType;

            if(string.IsNullOrWhiteSpace(url)) return;

            Dictionary<string,string> data = new();
            foreach(KeyValuePair<string,Func<Watch,string>> kv in defaultDataKeys){
                data.Add(kv.Key, kv.Value(watch));
            }

            var webhookCfg = new WebhookConfig{
                Method = method,
                Url = url,
                Body = body,
                Headers = headers,
                BodyType = bodyType
            };

            _ = WebhookNotify.SendAsync(webhookCfg, data);
        }
    }
}
