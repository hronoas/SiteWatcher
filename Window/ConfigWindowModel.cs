using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Media;

namespace SiteWatcher{
    public partial class ConfigWindowModel : BaseWindowModel<ConfigWindow>{
        public SortableBindingList<WatchTag> Tags  {get;set;}
        public ProxyServer Proxy {get;set;}
        public string NotifiySound {get;set;}
        public bool CheckAllOnlyVisible {get;set;}
        public TimeSpan ErrorInterval {get;set;}
        public bool WriteLog {get;set;}
        
        public Command ChooseNotifySoundCommand {get;set;}
        public Command SaveCommand {get;set;}
        public Command CancelCommand {get;set;}
        public Command AddTagCommand {get;set;}
        public TelegramConfig Telegram  {get;set;}
        public Command<WatchTag> RemoveTagCommand {get;set;}
        public Command CloseWindowCommand {get;set;}
        public ConfigWindowModel( List<WatchTag> tags, ConfigWindow win) : base(win){
            win.DataContext = this;
            SaveCommand=new(o=>{SaveAll(); window.DialogResult=true; window.Close();});
            CancelCommand=new(o=>{window.DialogResult=false; window.Close();});
            ChooseNotifySoundCommand=new(o=>ChooseNotifySound());
            AddTagCommand=new(o=>Tags.Add(new WatchTag()));
            RemoveTagCommand=new(t=>Tags.Remove(t));
            CloseWindowCommand = new(o=>win.Close());

            Tags= new(tags);
            Proxy = CurrentConfig.Proxy.Clone();
            Telegram = CurrentConfig.Telegram.Clone();
            NotifiySound = CurrentConfig.NotifySound;
            CheckAllOnlyVisible = CurrentConfig.CheckAllOnlyVisible;
            WriteLog = CurrentConfig.WriteLog;
            ErrorInterval = CurrentConfig.ErrorInterval;
        }

        private void SaveAll(){
            CurrentConfig.NotifySound=NotifiySound;
            CurrentConfig.Proxy=Proxy.Clone();
            CurrentConfig.Telegram=Telegram;
            if(string.IsNullOrWhiteSpace(CurrentConfig.Telegram.Template)) CurrentConfig.Telegram.Template=SiteWatcherConfig.defaultTelegramTemplate;
            CurrentConfig.CheckAllOnlyVisible=CheckAllOnlyVisible;
            CurrentConfig.WriteLog=WriteLog;
            CurrentConfig.ErrorInterval = ErrorInterval;
            CurrentConfig.Tags.Clear();
            Tags.ToList().ForEach(t=>CurrentConfig.Tags.Add(t));
        }
        public void ChooseNotifySound(){
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                DefaultExt = "wav",
                Filter = "wav files (*.wav) | *.wav"
            };
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