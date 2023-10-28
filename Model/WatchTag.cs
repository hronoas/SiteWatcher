using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SiteWatcher{
    public class WatchTag:PropertyChangedBase{
        public string Name { get=>name; set=>SetField(ref name, value);}
        private string name ="";
        public string Description { get=>description; set=>SetField(ref description, value);}
        private string description = "";
        public bool? Selected { get=>selected; set=>SetField(ref selected, value);}
        private bool? selected = false;
        public int Count { get=>count; set=>SetField(ref count, value);}
        private int count = 0;

        public override string ToString(){
            string state = "";
            if(Selected??false) state="!";
            else if(Selected==null) state="!!";
            return state+Name+(String.IsNullOrWhiteSpace(Description)?"":":"+Description);
        }
        public static WatchTag FromString(string str){
            WatchTag tag = new();
            var match = Regex.Match(str,"^(!+)?([^:]+)(?::(.*))?$");
            if(match.Success){
                tag.Selected = match.Groups[1].Value=="!";
                if(match.Groups[1].Value=="!!") tag.Selected=null;
                tag.Name = match.Groups[2].Value;
                tag.Description = match.Groups[3].Value;
            }
            return tag;
        }

        public WatchTag Clone(){
            return MemberwiseClone() as WatchTag??new WatchTag();
        }
    }
    class WatchTagConverter : JsonConverter<WatchTag>{
        public override WatchTag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => WatchTag.FromString(reader.GetString());
        public override void Write(Utf8JsonWriter writer, WatchTag watchTag, JsonSerializerOptions options) => writer.WriteStringValue(watchTag.ToString());
    }

}