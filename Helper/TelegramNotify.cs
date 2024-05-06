using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Drawing;
using CefSharp.DevTools.DOM;
using System.Windows.Forms.VisualStyles;
using System.Net;
using System.Threading;
using System.Windows.Controls;

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
            string page_url = (data?.ContainsKey("url")??false)?data["url"]:"";
            chatId = string.IsNullOrWhiteSpace(chatId)?config.ChatId:chatId;
            if (string.IsNullOrWhiteSpace(chatId)) { 
                Log("No destination: empty chat_id","telegram");
                return;
            }
            
            string message_template = string.IsNullOrWhiteSpace(text_template)?config.Template:text_template;
            string messageText = Replacer.replacePatterns(message_template,data);
            if (messageText.Length>4000) messageText=messageText.Substring(0,4000);

            if (string.IsNullOrWhiteSpace(messageText)){
                Log("Nothing to send: empty message","telegram");
                return;
            };
            
            string uri = $"https://api.telegram.org/bot{config.BotToken}/sendMessage?chat_id={chatId}&parse_mode=HTML&text={Uri.EscapeDataString(messageText)}";
            HttpClient httpClient = new();
            try{
                using (HttpResponseMessage response_html = await httpClient.GetAsync(uri)){
                    if (!response_html.IsSuccessStatusCode){
                        if(response_html.StatusCode==HttpStatusCode.BadRequest){
                            Dictionary<string,string>? strip_data = new(data?.Select(kv=>new KeyValuePair<string, string>(kv.Key,StripHtmlTags(kv.Value, page_url)))??Enumerable.Empty<KeyValuePair<string, string>>());
                            string messageText2 = Replacer.replacePatterns(message_template,strip_data);
                            uri = $"https://api.telegram.org/bot{config.BotToken}/sendMessage?chat_id={chatId}&parse_mode=HTML&text={Uri.EscapeDataString(messageText2)}";
                            Thread.Sleep(300);
                            using (HttpResponseMessage response_strip = await httpClient.GetAsync(uri)){
                                if(response_strip.StatusCode==HttpStatusCode.BadRequest){
                                    messageText = StripHtmlTags(messageText,page_url);
                                    uri = $"https://api.telegram.org/bot{config.BotToken}/sendMessage?chat_id={chatId}&text={Uri.EscapeDataString(messageText)}";
                                    Thread.Sleep(300);
                                    using (HttpResponseMessage response_text = await httpClient.GetAsync(uri)){
                                        if (!response_text.IsSuccessStatusCode){
                                            Log($"Send TEXT message fail. Chat:{chatId}. Status code: {response_html.StatusCode}, Reason phrase: {response_html.ReasonPhrase}","telegram");
                                        }
                                    }
                                }else{
                                    Log($"Send data stripped HTML message fail. Probably error in template. Chat:{chatId}. Status code: {response_html.StatusCode}, Reason phrase: {response_html.ReasonPhrase}","telegram");
                                }
                            }
                        }else{
                            Log($"Send HTML message fail. Chat:{chatId}. Status code: {response_html.StatusCode}, Reason phrase: {response_html.ReasonPhrase}","telegram");
                        }
                    }
                }    
            }catch (System.Exception e){
                Log($"Send message error: Chat: {chatId}, Error: {e}","telegram");
            }finally{
                httpClient.Dispose();
            }
            
        }
    }


    public static class Replacer{

        private static Regex tag_regex = new Regex(@"{(?<tag>[a-z]+)(?<attribs>!?[=~][^}]*)}(?<content>.*?){\/\1}",RegexOptions.Singleline|RegexOptions.IgnoreCase);
        private static Regex attribs_regex = new Regex(@"(?<mod>!)?(?=[=~])(?<comparator>[=~])(?:""(?<val1>.*?)(?<!\\)""|(?<val2>.*?)(?<!\\)(?=(!?)[=~]|$))",RegexOptions.Singleline|RegexOptions.IgnoreCase);
        private static Regex stag_regex = new Regex(@"{(?<tag>[a-z]+)}",RegexOptions.Singleline|RegexOptions.IgnoreCase);

        public static string replacePatterns(string pattern, Dictionary<string,string>? data=null){
            
            Match tag_match = tag_regex.Match(pattern);
            while(tag_match.Success){
                string tag_name = tag_match.Groups["tag"].Value.ToString().ToLower();
                string tag_attribs= tag_match.Groups["attribs"].Value.ToString();
                string tag_content = tag_match.Groups["content"].Value.ToString();
                string replace_text = "";
                if(data!=null && data.ContainsKey(tag_name))
                    replace_text = replaceCompares(tag_name,tag_attribs,tag_content,data);
                pattern = pattern.Replace(tag_match.Groups[0].Value,replace_text);
                tag_match = tag_regex.Match(pattern);
            }
            return replaceTags(pattern,data);
        }

        private static string replaceCompares(string tag, string attribs, string value, Dictionary<string,string> data){
            
            MatchCollection matches = attribs_regex.Matches(attribs);
            bool result = false;
            if(matches.Count!=0){
                foreach (Match attrib_match in matches){
                    string val = (!string.IsNullOrEmpty(attrib_match.Groups["val1"].Value.ToString())?attrib_match.Groups["val1"].Value.ToString():attrib_match.Groups["val2"].Value.ToString()).ToLower();
                    string check = data[tag].ToLower();
                    bool inverse = attrib_match.Groups["mod"].Value.ToString()=="!";
                    switch (attrib_match.Groups["comparator"].Value.ToString()){
                        case "=":
                            result |= inverse ? val!=check:val==check ;
                            break;
                        case "~":
                            result |= inverse ? !check.Contains(val): check.Contains(val);
                            break;
                        default: 
                            break;
                    }
                }
            }else if (data!=null && data.ContainsKey(tag) && !string.IsNullOrWhiteSpace(data[tag])) result = true;
            if(result)
                return replacePatterns(value,data);
            else 
                return "";
        }

        private static string replaceTags(string text, Dictionary<string,string>? data){
            
            foreach (Match tag_match in stag_regex.Matches(text)){
                string tag_name = tag_match.Groups["tag"].Value.ToString().ToLower();
                if (data!=null && data.ContainsKey(tag_name))
                    text = text.Replace('{'+tag_name+'}',data[tag_name]);
            }
            return text;
        }

    }
}
