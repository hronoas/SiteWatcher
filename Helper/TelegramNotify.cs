using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SiteWatcher{
    public class TelegramConfig{
        public string BotToken { get; set; } = "";
        public string ChatId { get; set; } = "";
        public string Template { get;set;} = "";
        public TelegramConfig Clone() {
            return (TelegramConfig)MemberwiseClone();
        }
    }
    
    public static class TelegramNotify{
        public static async Task SendMessageAsync(TelegramConfig config, string text_template="", Dictionary<string,string>? data = null, string? chatId = null){
            chatId = string.IsNullOrWhiteSpace(chatId)?config.ChatId:chatId;
            if (string.IsNullOrWhiteSpace(chatId)) { 
                Log("No chat id","telegram");
                return;
            }
            
            string messageText = string.IsNullOrWhiteSpace(text_template)?config.Template:text_template;
            if (string.IsNullOrWhiteSpace(messageText)){
                Log("Nothing to send","telegram");
                return;
            };

            if (data!=null){
                foreach (KeyValuePair<string,string> kv in data){
                    messageText = messageText.Replace($"{{{kv.Key}}}", kv.Value.ToString());
                }
            }
            
            string uri = $"https://api.telegram.org/bot{config.BotToken}/sendMessage?chat_id={chatId}&text={Uri.EscapeDataString(messageText)}";
            HttpClient httpClient = new();
            try{
                using (HttpResponseMessage response = await httpClient.GetAsync(uri)){
                    if (!response.IsSuccessStatusCode)
                        Log($"Send message fail. Chat:{chatId}. Status code: {response.StatusCode}, Reason phrase: {response.ReasonPhrase}","telegram");
                }    
            }catch (System.Exception e){
                Log($"Send message error: Chat: {chatId}, Error: {e} Chat:{chatId}.","telegram");
            }finally{
                httpClient.Dispose();
            }
            
        }
    }
}
