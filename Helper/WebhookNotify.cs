using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SiteWatcher{
    public class WebhookConfig{
        public string Method { get; set; } = "POST";
        public string Url { get; set; } = "";
        public string Body { get; set; } = "";
        public string Headers { get; set; } = "";
        public string BodyType { get; set; } = "json";
        public WebhookConfig Clone() => (WebhookConfig)MemberwiseClone();
    }

    public static class WebhookNotify{

        public static async Task<string> SendAsync(WebhookConfig config, Dictionary<string,string> data){
            string url = Replacer.replacePatterns(config.Url, data);
            if(string.IsNullOrWhiteSpace(url)){
                Log("No destination: empty webhook URL","webhook");
                return "No destination: empty webhook URL";
            }

            string method = config.Method.ToUpper();
            string body = "";
            if(method != "GET"){
                string bodyType = config.BodyType.ToLower();
                Dictionary<string,string>? encoded = null;
                if(bodyType == "json"){
                    encoded = new Dictionary<string,string>();
                    foreach(var kv in data) encoded.Add(kv.Key, EncodeJson(kv.Value));
                }else if(bodyType == "query"){
                    encoded = new Dictionary<string,string>();
                    foreach(var kv in data) encoded.Add(kv.Key, EncodeQuery(kv.Value));
                }
                body = Replacer.replacePatterns(config.Body, encoded ?? data);
            }

            using(var client = new HttpClient()){
                try{
                    var request = new HttpRequestMessage(){
                        RequestUri = new Uri(url),
                        Method = new HttpMethod(method)
                    };

                    string headersText = Replacer.replacePatterns(config.Headers, data);
                    string? contentTypeOverride = null;
                    if(!string.IsNullOrWhiteSpace(headersText)){
                        foreach(string line in headersText.Split('\n')){
                            string trimmed = line.Trim();
                            if(string.IsNullOrEmpty(trimmed)) continue;
                            int sep = trimmed.IndexOf(':');
                            if(sep > 0){
                                string key = trimmed.Substring(0, sep).Trim();
                                string val = trimmed.Substring(sep + 1).Trim();
                                if(key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)){
                                    contentTypeOverride = val;
                                }else{
                                    try{ request.Headers.Remove(key); }catch{}
                                    request.Headers.Add(key, val);
                                }
                            }
                        }
                    }

                    if(!string.IsNullOrEmpty(body)){
                        request.Content = new StringContent(body, Encoding.UTF8);
                        if(!string.IsNullOrEmpty(contentTypeOverride)){
                            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentTypeOverride);
                        }else if(request.Content.Headers.ContentType == null){
                            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        }
                    }

                    using(var response = await client.SendAsync(request)){
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var sb = new StringBuilder();
                        sb.AppendLine($"Status: {response.StatusCode} {(int)response.StatusCode} {response.ReasonPhrase}");
                        sb.AppendLine("--- Headers ---");
                        foreach(var h in response.Headers) sb.AppendLine($"{h.Key}: {string.Join(", ", h.Value)}");
                        if(!string.IsNullOrEmpty(responseBody)){
                            sb.AppendLine("--- Body ---");
                            sb.AppendLine(responseBody);
                        }
                        if(!response.IsSuccessStatusCode){
                            Log($"Webhook fail. URL: {url}, Status: {response.StatusCode}, Reason: {response.ReasonPhrase}","webhook");
                        }
                        return sb.ToString();
                    }
                }catch(Exception e){
                    Log($"Webhook error. URL: {url}, Error: {e}","webhook");
                    return $"Error: {e.Message}";
                }
            }
        }

        private static string EncodeJson(string value){
            if(string.IsNullOrEmpty(value)) return value;
            var sb = new StringBuilder();
            foreach(char c in value){
                switch(c){
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if(c < 0x20){
                            sb.AppendFormat("\\u{0:x4}", (int)c);
                        }else{
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        private static string EncodeQuery(string value){
            if(string.IsNullOrEmpty(value)) return value;
            return Uri.EscapeDataString(value).Replace("!", "%21").Replace("'", "%27").Replace("(", "%28").Replace(")", "%29").Replace("*", "%2A");
        }
    }
}
