using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using SiteWatcher.Plugins;

namespace SiteWatcher{
    public partial class AppWindowModel : BaseWindowModel<AppWindow>
    {
        private readonly List<Type> _plugins = Share.Plugins;

        private static string GetDllSearchPattern() => Path.Combine("*", "*.dll");

        public void LoadPlugins(string directoryPath){
            try{
                // Validate input directory path
                if (String.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath)){
                    Log($"Invalid directory path: '{directoryPath}'");
                    return;
                }
                
                string[] dllFilePaths = Directory.GetFiles(directoryPath, GetDllSearchPattern());

                foreach (string dllFilePath in dllFilePaths){
                    if (!IsValidDll(dllFilePath)){
                        Log($"Not valid plugin file: {dllFilePath}");
                        continue;
                    }

                    var assembly = Assembly.LoadFrom(dllFilePath);

                    foreach (Type type in assembly.GetTypes()){
                        if (typeof(IPlugin).IsAssignableFrom(type)){

                            _plugins.Add(type);
                        }else{
                            Log($"Is not a plugin {dllFilePath}: {type}");
                        }
                    }
                }
            }catch (Exception ex){
                Log($"Error occurred while loading plugins: {ex.Message}");
            }
        }

        public IPlugin? InitPlugin(string type,Watch watch){
            return InitPlugin(_plugins.Where(t=>t.Name==type)?.FirstOrDefault(),watch);

        }
        public IPlugin? InitPlugin(Type? type,Watch watch){
            if(type==null) return null;
            var plugin = Activator.CreateInstance(type) as IPlugin;
            if (plugin != null){
                plugin.Init(watch);
                watch.plugins.Add(plugin);
            }
            return plugin;
        }
        public void InitPlugins(Watch watch){
            foreach (Type type in _plugins){
                InitPlugin(type,watch);
            }
        }
        private bool IsValidDll(string filePath){
            try{
                AssemblyName.GetAssemblyName(filePath);
                return true;
            }catch (BadImageFormatException){
                return false;
            }
        }
    }


}