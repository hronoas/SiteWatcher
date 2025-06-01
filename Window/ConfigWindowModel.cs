using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Media;
using System.Diagnostics;
using System.IO;

namespace SiteWatcher{
    public partial class ConfigWindowModel : BaseWindowModel<ConfigWindow>
    {
        public SortableBindingList<WatchTag> Tags { get; set; }
        public ProxyServer Proxy { get; set; }
        public string NotifiySound { get; set; }
        public bool CheckAllOnlyVisible { get; set; }
        public TimeSpan ErrorInterval { get; set; }
        public bool StartMinimized { get; set; }

        public bool AutoStart
        {
            get => IsSiteWatcherInStartup();
            set => ToggleAutoStart(value);
        }

        private bool IsSiteWatcherInStartup()
        {
            string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string exePath = Environment.ProcessPath;

            if (string.IsNullOrEmpty(exePath)) return false;

            foreach (string file in Directory.GetFiles(startupPath, "*.lnk"))
            {
                string targetPath = GetShortcutTargetPath(file);
                if (!string.IsNullOrEmpty(targetPath) &&
                    string.Equals(targetPath, exePath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public Command<bool> ToggleAutoStartCommand { get; set; }

        public bool WriteLog { get; set; }

        public Command ChooseNotifySoundCommand { get; set; }
        public Command SaveCommand { get; set; }
        public Command CancelCommand { get; set; }
        public Command OpenConfigFolder { get; set; }
        public Command OpenWatchesFolder { get; set; }
        public Command AddTagCommand { get; set; }
        public TelegramConfig Telegram { get; set; }
        public Command<WatchTag> RemoveTagCommand { get; set; }
        public Command CloseWindowCommand { get; set; }
        public ConfigWindowModel(List<WatchTag> tags, ConfigWindow win) : base(win)
        {
            win.DataContext = this;
            SaveCommand = new(o => { SaveAll(); window.DialogResult = true; window.Close(); });
            CancelCommand = new(o => { window.DialogResult = false; window.Close(); });
            OpenConfigFolder = new(o => { Process.Start("explorer.exe", Path.GetDirectoryName(AppConfig)); });
            OpenWatchesFolder = new(o => { Process.Start("explorer.exe", Path.GetDirectoryName(WatchesConfig)); });
            ChooseNotifySoundCommand = new(o => ChooseNotifySound());
            AddTagCommand = new(o => Tags.Add(new WatchTag()));
            RemoveTagCommand = new(t => Tags.Remove(t));
            CloseWindowCommand = new(o => win.Close());

            Tags = new(tags);
            Proxy = CurrentConfig.Proxy.Clone();
            Telegram = CurrentConfig.Telegram.Clone();
            NotifiySound = CurrentConfig.NotifySound;
            CheckAllOnlyVisible = CurrentConfig.CheckAllOnlyVisible;
            WriteLog = CurrentConfig.WriteLog;
            ErrorInterval = CurrentConfig.ErrorInterval;
            StartMinimized = CurrentConfig.StartMinimized;
            ToggleAutoStartCommand = new (o => ToggleAutoStart(o));
        }

        private void SaveAll()
        {
            CurrentConfig.NotifySound = NotifiySound;
            CurrentConfig.Proxy = Proxy.Clone();
            CurrentConfig.Telegram = Telegram;
            if (string.IsNullOrWhiteSpace(CurrentConfig.Telegram.Template)) CurrentConfig.Telegram.Template = SiteWatcherConfig.defaultTelegramTemplate;
            CurrentConfig.CheckAllOnlyVisible = CheckAllOnlyVisible;
            CurrentConfig.WriteLog = WriteLog;
            CurrentConfig.ErrorInterval = ErrorInterval;
            CurrentConfig.StartMinimized = StartMinimized;
            CurrentConfig.Tags.Clear();
            Tags.ToList().ForEach(t => CurrentConfig.Tags.Add(t));
        }
        public void ChooseNotifySound()
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                DefaultExt = "wav",
                Filter = "wav files (*.wav) | *.wav"
            };
            DialogResult result = fileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                string openFileName = fileDialog.FileName;
                try
                {
                    using (SoundPlayer soundPlayer = new SoundPlayer(openFileName))
                    {
                        soundPlayer.Play();
                        soundPlayer.Stop();
                        NotifiySound = openFileName;
                        ChangedField(nameof(NotifiySound));
                    }
                }
                catch
                {
                    System.Windows.MessageBox.Show("Неподдерживаемый формат файла", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        public void ToggleAutoStart(bool addToStartup) {
            string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string exePath = Environment.ProcessPath;

            if (string.IsNullOrEmpty(exePath)) return;

            if (addToStartup) {
                string baseName = "SiteWatcher";
                string ext = ".lnk";
                int counter = 0;
                string lnkPath;

                // Find a unique name for the shortcut
                do {
                    if (counter > 0) {
                        lnkPath = Path.Combine(startupPath, $"{baseName} ({counter}){ext}");
                    } else {
                        lnkPath = Path.Combine(startupPath, $"{baseName}{ext}");
                    }
                    counter++;
                } while (File.Exists(lnkPath) && GetShortcutTargetPath(lnkPath) != exePath);

                // Create a proper Windows shortcut to the executable
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(lnkPath);

                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;
                shortcut.IconLocation = $"{exePath},0";
                shortcut.Save();
            } else {
                // Delete all shortcuts that point to our executable
                foreach (string file in Directory.GetFiles(startupPath, "*.lnk")) {
                    try {
                        if (GetShortcutTargetPath(file) == exePath) {
                            File.Delete(file);
                        }
                    } catch {
                        // Skip any invalid shortcuts
                    }
                }
            }
        }

        private string GetShortcutTargetPath(string shortcutPath) {
            try {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                return shortcut.TargetPath;
            } catch {
                return null;
            }
        }
    }
}