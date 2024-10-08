using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using HtmlAgilityPack;

namespace SiteWatcher
{
    public class Utils
    {

        public static string StripHtmlTags(string input, string savelinks_base = ""){
            if((new Regex(@"<[^>]+>")).Match(input).Success){
                return HtmlUtilities.FormatLineBreaks(input, savelinks_base);
            }else{
                return input;
            }
        }
        private static string[] args = Environment.GetCommandLineArgs();
        public static string? GetArgument(string option) => args
            .SkipWhile(i => i.ToLower() != option.ToLower())
            .Skip(1)
            .Take(1)
            .FirstOrDefault();

        private static string version ="";
        public static string Version { 
            get{
                if(String.IsNullOrEmpty(version)){
                    System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
                    version = fvi.FileVersion??"unk";
                }
                return version;
            }
        } 
        private static string description ="";
        public static string Description { 
            get{
                if(String.IsNullOrEmpty(description)){
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    var attributes = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyDescriptionAttribute), false);
                    if (attributes.Length == 0)
                        description=""; // No description found

                    var descriptionAttribute = (System.Reflection.AssemblyDescriptionAttribute)attributes[0];
                    description = descriptionAttribute.Description;
                }
                return description;
            }
        } 
        public static string AppExe { get; } = System.Reflection.Assembly.GetExecutingAssembly().Location;
        public static string AppName { get; } = Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static string AppPath { get; } = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)??".";
        public static string CurrentDir { get; } = Directory.GetCurrentDirectory();
        public static string WatchesConfig {get; } = getConfigFileName("Watches.json");
        public static string AppConfig {get; } = getConfigFileName("Config.json");
        public static string AppLog { get; } = getFileNameForConfig(AppName+".log");
        // Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),AppName,AppName+".log");
        public static string AppResources { get; } = Path.Combine(AppPath,"Resources");
        public static string AppCache { get; } = getFileNameForConfig("Cache");
        public static string AppIcons { get; } = getFileNameForConfig("Icons");

        private static SiteWatcherConfig? _currentConfig;
        private static string _oldConfig = "";
        public static SiteWatcherConfig CurrentConfig { 
            get{
                if(_currentConfig==null) {
                    if(File.Exists(AppConfig)){
                        _oldConfig = ReadAllText(AppConfig);
                        _currentConfig = Deserialize<SiteWatcherConfig>(_oldConfig)??new SiteWatcherConfig();
                    }else{
                        _currentConfig = new();
                    }
                }
                return _currentConfig;
            }
        }
        public static void SaveConfig(){
            string newConfig=Serialize(CurrentConfig);
            if(newConfig!= _oldConfig){
                RewriteFile(AppConfig,newConfig);
                _oldConfig=newConfig;
            }
        }

        private static string getFileNameForConfig(string file){
            return Path.GetDirectoryName(WatchesConfig)==AppPath?Path.Combine(AppPath,file):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),AppName,file);
        }
        public static void RewriteFile(string path, string contents, bool create_backup = true){
            var tempPath = Path.GetTempFileName();
            var data = System.Text.Encoding.UTF8.GetBytes(contents);

            using (var tempFile = File.Create(tempPath, 4096, FileOptions.WriteThrough))
                tempFile.Write(data, 0, data.Length);

            var backup = path + ".backup";
            if (File.Exists(path)){
                if (File.Exists(backup))
                    File.Delete(backup);
                File.Move(path,backup);
            }
            File.Move(tempPath,path);
            if (!create_backup) File.Delete(backup);
            //File.Replace(tempPath, path, backup); not working?
        }
        public static string ReadAllText(string file){
            using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var textReader = new StreamReader(fileStream);
            return textReader.ReadToEnd();
        }
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

        public static void Log(object str, string section = "")
        {
            bool writefile = _currentConfig!=null && CurrentConfig.WriteLog; // no write log before config loaded
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
