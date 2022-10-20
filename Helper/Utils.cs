using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace SiteWatcher
{
    public class Utils
    {
        private static string[] args = Environment.GetCommandLineArgs();
        public static string? GetArgument(string option) => args
            .SkipWhile(i => i.ToLower() != option.ToLower())
            .Skip(1)
            .Take(1)
            .FirstOrDefault();
        public static string AppExe { get; } = System.Reflection.Assembly.GetExecutingAssembly().Location;
        public static string AppName { get; } = Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static string AppPath { get; } = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)??".";
        public static string CurrentDir { get; } = Directory.GetCurrentDirectory();
        public static string WatchesConfig {get; } = getConfigFileName("Watches.json");
        public static string AppConfig {get; } = getConfigFileName("Config.json");
        public static string AppLog { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),AppName,AppName+".log");
        public static string AppResources { get; } = Path.Combine(AppPath,"Resources");
        public static string AppCache { get; } = Path.GetDirectoryName(WatchesConfig)==AppPath?Path.Combine(AppPath,"Cache"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),AppName,"Cache");
        public static string AppIcons { get; } = Path.GetDirectoryName(WatchesConfig)==AppPath?Path.Combine(AppPath,"Icons"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),AppName,"Icons");

        private static string getConfigFileName(string file){
            string filenameLocal = Path.Combine(AppPath,file);
            string filenameRoaming = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),AppName,file);
            if(File.Exists(filenameLocal)){
                if(HasWritePermissionOnDir(Path.GetDirectoryName(filenameLocal))) return filenameLocal;
                else{
                    if(File.Exists(filenameRoaming)) return filenameRoaming;
                    else{
                        Directory.CreateDirectory(Path.GetDirectoryName(filenameRoaming));
                        File.Copy(filenameLocal,filenameRoaming);
                        return filenameRoaming;
                    }
                }
            } else {
                string dir = Path.GetDirectoryName(filenameRoaming);
                if(!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return filenameRoaming;
            }
        }

        public static StreamReader? ReadResource(string name)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            //foreach (string s in assembly.GetManifestResourceNames()) Debug.WriteLine("Resource: "+s);
            
            string resourcePath = "SiteWatcher.Resources."+name;
            if(!assembly.GetManifestResourceNames().Any(s=>s==resourcePath)) return null;

            return new StreamReader(assembly.GetManifestResourceStream(resourcePath));
        }

        public static bool HasWritePermissionOnDir(string path){
            try{
                string testfile = Path.Combine(path,"test.write");
                int i=0;
                while(File.Exists(testfile+i.ToString())) i++;
                testfile=testfile+i.ToString();
                File.WriteAllText(testfile,"test string");
                File.Delete(testfile);   
            }catch (System.Exception ex){
                return false;
            }
            return true;
        }

        public static void Log(object str, string section = "", bool writefile = false)
        {
            section = section == "" ? "" : "[" + section + "] ";
            string outstr = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss ") + section + str.ToString();
            Console.Error.WriteLine(outstr);
            if (writefile) File.AppendAllText(AppLog, outstr + "\n");
        }
        public class JsonNamingPolicyLower : JsonNamingPolicy
        {
            public override string ConvertName(string name) => name.ToLower();
        }
        public static JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = new JsonNamingPolicyLower(),
            AllowTrailingCommas = true,
            Converters = {new WatchTagConverter()}
        };
        public static T? Deserialize<T>(string Data) {
            try
            {
                return JsonSerializer.Deserialize<T>(Data, JsonOptions) ;
            }
            catch (System.Exception e)
            {
                Log(e.Message,"error");
                return default(T);
            }
        }
        public static string Serialize(object o) => JsonSerializer.Serialize(o, JsonOptions);

        public static void OpenUrl(string url)
        {
            try{
                Process.Start(url);
            }catch{
                try{
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)){
                        //url = url.Replace("&", "^&");
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)){
                        Process.Start("xdg-open", url);
                    }else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)){
                        Process.Start("open", url);
                    }else {
                        throw;
                    }
                }catch{
                    MessageBox.Show($"Can't open: {url}");
                }
            }
        }

        public static string chisl(int num, string str1, string str2,string str5){
            string s = str5;
            if (num % 10 == 1) s = str1;
            if (num % 10 >= 2 && num % 10 <= 4) s = str2;
            return s;
        }

        public static T? GetParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);

            if (parent == null) return null;

            if (parent is T tParent)
                return tParent;

            return GetParent<T>(parent);
        }  
    }
    
}
