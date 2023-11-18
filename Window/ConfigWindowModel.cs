using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Media;
using System.ComponentModel;
using System.Collections.ObjectModel;
using SiteWatcher.Plugins;
using System.Reflection;

namespace SiteWatcher{
    public partial class ConfigWindowModel : BaseWindowModel<ConfigWindow>{
        public SortableBindingList<WatchTag> Tags  {get;set;}
        public ProxyServer Proxy {get;set;}
        public string NotifiySound {get;set;}
        public bool CheckAllOnlyVisible {get;set;}
        public Command ChooseNotifySoundCommand {get;set;}
        public Command SaveCommand {get;set;}
        public Command CancelCommand {get;set;}
        public Command AddTagCommand {get;set;}
        public TelegramConfig Telegram  {get;set;}
        public Command<WatchTag> RemoveTagCommand {get;set;}
        public Command CloseWindowCommand {get;set;}
        public Dictionary<string,PluginParamsData> PluginsConfig {get;set;} = new();

        public class CPlugin : INotifyPropertyChanged{
            public string Key {get;set;}
            public ObservableCollection<CParam> Value {get;set;}
            public event PropertyChangedEventHandler? PropertyChanged;
        }
        public class CParam : INotifyPropertyChanged{
            public string Key {get;set;}
            public PluginParam Param {get;set;}
            public string Value {get;set;}
            
            public event PropertyChangedEventHandler? PropertyChanged;
        }
        public ObservableCollection<CPlugin> PluginsParams { 
            get{
                if(pluginsParams==null) pluginsParams = GetPluginsParams();
                return pluginsParams;
            } set=>SetField(ref pluginsParams, value);}
        private ObservableCollection<CPlugin> pluginsParams;
        public ObservableCollection<CPlugin> GetPluginsParams(){
            ObservableCollection<CPlugin> ret = new();
            foreach (var pluginType in Share.Plugins){
                string pluginName = pluginType.Name;
                CPlugin pParams = new(){Key=pluginName, Value = new ()};
                PluginParamsData? pData;
                PluginsConfig.TryGetValue(pluginName,out pData);
                PluginParams? pluginParams = (PluginParams?)pluginType.GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p=>p.Name=="Params"&&p.PropertyType==typeof(PluginParams)).Single().GetValue(null);
                PluginParamsData? pluginParamsData = pluginParams?.ToData();
                if(pluginParamsData!=null && pluginParams!=null) foreach (var item in pluginParamsData){
                    if(pluginParams[item.Key].Hidden) continue;
                    CParam cParam = new(){Key=item.Key,Param=pluginParams[item.Key],Value=item.Value};
                    if(pData!=null){
                        string? val;
                        pData.TryGetValue(item.Key,out val);
                        if(val!=null) cParam.Value=val;
                    }
                    pParams.Value.Add(cParam);
                }
                if(pParams.Value.Count>0) ret.Add(pParams);
            }
            return ret;
        }

        public ConfigWindowModel(List<WatchTag> tags, string NotifyFile, ProxyServer proxy, TelegramConfig telegram, Dictionary<string,PluginParamsData> pluginsConfig, bool CheckAllOnlyVisibleS, ConfigWindow win) : base(win){
            win.DataContext = this;
            Tags= new(tags);
            Proxy = proxy.Clone();
            Telegram = telegram.Clone();
            PluginsConfig = Deserialize<Dictionary<string,PluginParamsData>>(Serialize(pluginsConfig))??new();
            NotifiySound = NotifyFile;
            CheckAllOnlyVisible = CheckAllOnlyVisibleS;
            SaveCommand=new(o=>{window.DialogResult=true; window.Close();});
            CancelCommand=new(o=>{window.DialogResult=false; window.Close();});
            ChooseNotifySoundCommand=new(o=>ChooseNotifySound());
            AddTagCommand=new(o=>Tags.Add(new WatchTag()));
            RemoveTagCommand=new(t=>Tags.Remove(t));
            CloseWindowCommand = new(o=>win.Close());
        
        }
        public void ChooseNotifySound(){
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.DefaultExt = "wav";
            fileDialog.Filter = "wav files (*.wav) | *.wav";
            DialogResult result = fileDialog.ShowDialog();
             if(result == DialogResult.OK) {
                string openFileName = fileDialog.FileName;
                try{
                    using(SoundPlayer soundPlayer = new SoundPlayer(openFileName)){
                        soundPlayer.Play();
                        soundPlayer.Stop();
                        NotifiySound = openFileName;
                        ChangedField(nameof(NotifiySound));
                    }
                } catch {
                    System.Windows.MessageBox.Show("Неподдерживаемый формат файла","Ошибка",MessageBoxButton.OK,MessageBoxImage.Error);
                }
            }
        }
    }
}