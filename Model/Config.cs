using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SiteWatcher{
    public class SiteWatcherConfig{
        public Point WindowPosition {get;set;} = new Point(Screen.PrimaryScreen.WorkingArea.Width/2, Screen.PrimaryScreen.WorkingArea.Width/2);
        public Point WindowSize {get;set;} = new Point(500,500);
        public List<WatchTag> Tags {get;set;} = new();
        public int MaxProcesses {get;set;} = 3;

        public SiteWatcherConfig Clone(){
            return Deserialize<SiteWatcherConfig>(Serialize(this)??"{}")??new SiteWatcherConfig();
        }
    }
}