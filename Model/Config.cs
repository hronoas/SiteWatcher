using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SiteWatcher{
    public class SiteWatcherConfig{
        public Point WindowPosition {get;set;} = new Point(Screen.PrimaryScreen.WorkingArea.Width/2, Screen.PrimaryScreen.WorkingArea.Width/2);
        public Point WindowSize {get;set;} = new Point(500,500);
        public List<WatchTag> Tags {get;set;} = new();
        public int MaxProcesses {get;set;} = 3;
        public bool CheckAllOnlyVisible {get;set;} = false;
        public ProxyServer Proxy {get;set;} = new();
        public TelegramConfig Telegram {get;set;} = new();
        public string NotifySound {get;set;} = "";
        public TimeSpan ErrorInterval = new TimeSpan(0,5,0);

        public SiteWatcherConfig Clone(){
            return Deserialize<SiteWatcherConfig>(Serialize(this)??"{}")??new SiteWatcherConfig();
        }
    }
}