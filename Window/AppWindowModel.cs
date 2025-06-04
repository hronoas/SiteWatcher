using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Controls;

namespace SiteWatcher
{
    public partial class AppWindowModel : BaseWindowModel<AppWindow>
    {
        public SortableBindingList<Watch> Watches {get;set;}=new SortableBindingList<Watch>(new List<Watch>());
        public SortableBindingList<WatchTag> Tags {get;set;}=new(new List<WatchTag>());
        public bool TagsUpdating = false;
        public bool ShowNew { 
            get=>showNew; 
            set{
                if (SetField(ref showNew, value))
                    FilterWatches();
            }
        }

        public string currentFilterText = "";
        private bool showNew;

        public string? TextFilter { 
            get=>textFilter; 
            set{
                if(TagsUpdating) return;
                if (SetField(ref textFilter, value))
                    FilterWatches();
            }
        }
        private string? textFilter;
        private Stack<Watch> Trash {get;set;} = new();
        public bool Trashed {get{
            return Trash.Count>0;
        }}
        private ListView WatchList {get;set;}
        private Timer timer;
        public int TimerInterval { 
            get=>timerInterval; 
            set{
                SetField(ref timerInterval, value);
                timer?.Change(0,value*1000);
            }
        }
        private bool needSave = false;
        private int timerInterval = 60;
        private string oldConfig="";
        public Command CheckAllCommand{get;set;}
        public Command AddWatchCommand{get;set;}
        public Command CloseWindow{get;set;}
        public Command<Watch> DeleteWatchCommand{get;set;}
        public Command<Watch> CheckWatchCommand{get;set;}
        public Command<Watch> ToggleWatchCommand{get;set;}
        public Command<Watch> EditWatchCommand{get;set;}
        public Command<Watch> CopyWatchCommand{get;set;}
        public Command<Watch> NavigateWatchCommand{get;set;}
        public Command<Watch> NavigateAllWatchCommand{get;set;}
        public Command<Watch> ToggleReadWatchCommand{get;set;}
        public Command<Watch> CheckpointsCommand{get;set;}
        public Command UndeleteWatchCommand {get;set;}
        public Command ConfigCommand {get;set;}
        public Command ShowNewCommand {get;set;}
        public Command ClearFilterCommand {get;set;}
        
        public AppWindowModel(AppWindow win) : base(win){
            if (CurrentConfig==null) Log("Config not loaded!"); // force init config
            Log("Started");
            WatchList=win.WatchList;
            InitIcon();
            ConfigBackup();
            ConfigLoad();
            win.Closed+=(o,e)=>{ConfigSave();ConfigSave2();};
            CheckWatchCommand=new(w=>CheckSelectedWatch(w));
            AddWatchCommand=new(n=>AddWatch());
            EditWatchCommand =new(w=>EditWatch(w));
            DeleteWatchCommand = new(w=>DeleteSelectedWatch(w));
            CheckAllCommand = new(w=>CheckAll(CurrentConfig.CheckAllOnlyVisible));
            CopyWatchCommand = new(w=>CopyWatch(w));
            ToggleWatchCommand = new(w=>ToggleSelectedWatch(w));
            NavigateWatchCommand = new(w=>NavigateWatch(w));
            NavigateAllWatchCommand = new(w=>NavigateAllWatch(w));
            ToggleReadWatchCommand = new(w=>ToggleReadSelectedWatch(w));
            CheckpointsCommand = new(w=>CheckpointsWatch(w));
            UndeleteWatchCommand = new(o=>UndeleteWatch());
            ShowNewCommand = new(o=>{ShowNew=!ShowNew;});
            ClearFilterCommand = new(o=>{TextFilter=null;});
            ConfigCommand = new(o=>ShowConfig());
            CloseWindow = new(n=>win.Close());
            CreateTimer();
            Tags.ListChanged+=TagsChanged;
            win.Loaded += (s,e)=>RefreshList();
        }


        private void TagsChanged(object? sender, ListChangedEventArgs e){
            textFilter=null;
            ChangedField(nameof(Tags));
            RefreshList();
        }

        private void ShowConfig(){
            ConfigWindow win = new();
            Watches.Where(w=>w.Tags.Count>0).ToList().ForEach(w=>{
                w.Tags.Where(wt=>!Tags.Any(t=>t.Name==wt.Name)).ToList().ForEach(t=>Tags.Add(t));
            });
            ConfigWindowModel model = new(Tags.Select(t=>t.Clone()).ToList(), win);
            if(win.ShowDialog()??false){
                Tags.Clear();
                CurrentConfig.Tags.ToList().ForEach(t=>Tags.Add(t));
                NotifySound = CurrentConfig.NotifySound; // load wav
                CheckBrowser.proxy = CurrentConfig.Proxy.Clone();
                ConfigSave2();
            }
        }

        private void UndeleteWatch(){
            if(Trashed){
                Watches.Add(Trash.Pop());
                ChangedField(nameof(Trashed));
            }
            needSave=true;
        }

        private void CreateTimer(){
            timer = new Timer(TimerCheck,null,0,TimerInterval*1000);
        }

        private void TimerCheck(object? state){
            ConfigSave();
            foreach (Watch watch in Watches){
                if(watch.IsNeedCheck){
                    CheckWatch(watch);
                }
            }
        }

        public void ToggleReadSelectedWatch(Watch? w){
            List<Watch> toMark = (w==null)? WatchList.SelectedItems.Cast<Watch>().ToList(): new(){w};
            toMark.ForEach(w=>{
                if(w.Status==WatchStatus.New) NavigateWatch(w,false);
                else w.LastSeen= w.Diff.Prev.Time;
            });
            needSave=true;
        }
        public void NavigateWatch(Watch? w,bool open=true){
            w?.Navigate(open);
            needSave=true;
        }

        public void NavigateAllWatch(Watch? w){
            List<Watch> toNavigate = (w==null)?WatchList.SelectedItems.Cast<Watch>().ToList():new(){w};
            toNavigate.ForEach(w=>{
                w?.Navigate();
            });
        }

        public void DeleteSelectedWatch(Watch? w){
            List<Watch> toDelete = (w==null)?WatchList.SelectedItems.Cast<Watch>().ToList():new(){w};
            toDelete.ForEach(w=>DeleteWatch(w));
        }
        public void DeleteWatch(Watch? w){
            if(w !=null && Watches.Contains(w)){
                Trash.Push(w);
                ChangedField(nameof(Trashed));
                Watches.Remove(w);
            }
            needSave=true;
        }
        private void CheckAll(bool onlyvisible=false){
            foreach (var watch in Watches){
                if(watch.Enabled && (!onlyvisible || watch.IsVisible))
                    CheckWatch(watch);
            }
        }
        public void ToggleSelectedWatch(Watch? w){
            List<Watch> toToggle = (w==null)?WatchList.SelectedItems.Cast<Watch>().ToList():new(){w};
            toToggle.ForEach(w=>{
                w.Toggle();
                CheckBrowser.Dequeue(w);
            });
            needSave=true;
            //FilterWatches();
        }

        private void CheckSelectedWatch(Watch? w){
            List<Watch> toCheck = (w==null)?WatchList.SelectedItems.Cast<Watch>().ToList():new(){w};
            toCheck.ForEach(w=>CheckWatch(w));
        }

        private void CheckWatch(Watch w){
            DateTime prevDate = w.Diff.Next.Time;
            WatchStatus oldStatus = w.Status;
            w.Check(()=> {
                if(w.IsNeedNotify){
                    if(w.Notify) ShowToast(w);
                    if(w.SoundNotify) PlaySound(w);
                    if(w.NotifyTelegram) SendTelegram(w);
                    if(w.NotifyRepeatedError) w.LastError="";
                    w.IsNeedNotify=false;
                }else if(w.LastError!=w.Error){
                    if(!string.IsNullOrEmpty(w.Error)){
                        if(w.NotifyTelegram) SendTelegram(w);
                        if(w.NotifyAfterError) w.IsNeedNotify=true;
                    }
                    w.LastError=w.Error;
                }
                if (prevDate<w.Diff.Next.Time){
                    needSave = true;
                }
                if (oldStatus!=w.Status || w.Status==WatchStatus.New || prevDate<w.Diff.Next.Time){
                    RefreshList();
                }
            });
        }
        private void ConfigBackup(int Count=5){
            if(File.Exists(WatchesConfig)){
                if(File.Exists(WatchesConfig+"."+Count) && File.Exists(WatchesConfig+"."+(Count-1))) File.Delete(WatchesConfig+"."+Count);
                for (int i = Count-1; i >0 ; i--){
                   if(File.Exists(WatchesConfig+"."+i)) File.Move(WatchesConfig+"."+i,WatchesConfig+"."+(i+1)); 
                }
                File.Copy(WatchesConfig,WatchesConfig+".1");
            }
        }

        private void ConfigLoad(){
            Watches.Clear();
            if(File.Exists(WatchesConfig))
                Deserialize<List<Watch>>(ReadAllText(WatchesConfig))?.ForEach(x=>Watches.Add(x));

            window.Width = CurrentConfig.WindowSize.X;
            window.Height = CurrentConfig.WindowSize.Y;
            window.Left = CurrentConfig.WindowPosition.X;
            window.Top = CurrentConfig.WindowPosition.Y;
            NotifySound = CurrentConfig.NotifySound; // load wav
            CheckBrowser.parallelTasks = Math.Min(Math.Max(CurrentConfig.MaxProcesses,1),20);
            CheckBrowser.proxy = CurrentConfig.Proxy.Clone();
            var b = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            if(window.Top>b.Height-50 || window.Left>b.Width) window.BringToForeground();
            Tags.Clear();
            CurrentConfig.Tags.ForEach(t=>Tags.Add(t));

        }

        private void ConfigSave(){ // save config to disk only with needSave is true
            if (!needSave) return;
            string newConfig=Serialize(Watches);
            if(newConfig!= oldConfig){
                RewriteFile(WatchesConfig,newConfig);
                oldConfig=newConfig;
            }
            needSave=false;
        }
        private void ConfigSave2(){
            CurrentConfig.WindowPosition = new((int)window.Left,(int)window.Top);
            CurrentConfig.WindowSize = new((int)window.ActualWidth,(int)window.ActualHeight);
            CurrentConfig.Tags=Tags.ToList();
            SaveConfig();
        }
        private void AddWatch(){
            WatchWindow win = new();
            WatchWindowModel model = new(new Watch(),Tags.ToList(), win);
            if(win.ShowDialog()??false){
                Watches.Add(model.Item);
                CheckWatch(model.Item);
                needSave=true;
            }
        }
        private void CopyWatch(Watch w){
            Watch n = (Watch)w.Clone();
            n.Checkpoints=new();
            n.LastCheck=DateTime.MinValue;
            WatchWindow win = new();
            WatchWindowModel model = new(n,Tags.ToList(), win, true);
            if(win.ShowDialog()??false){
                Watches.Add(model.Item);
                CheckWatch(model.Item);
                needSave=true;
            }
        }

        private void RefreshIcon(){
            if(Watches.Any(x=>x.Status==WatchStatus.Checking)) IconState = WatchStatus.Checking;
            else if(Watches.Any(x=>x.Status==WatchStatus.Fail && x.Enabled)) IconState = WatchStatus.Fail;
            else if(Watches.Any(x=>x.Status==WatchStatus.New && x.Enabled)) IconState = WatchStatus.New;
            else if(Watches.Any(x=>x.Status==WatchStatus.NoChanges && x.Enabled)) IconState = WatchStatus.NoChanges;
            else IconState = WatchStatus.Off;
        }

        private void FilterWatches(){
            Dictionary<string, int> counts = new();
            Tags.ToList().ForEach(t=>counts[t.Name]=Watches.Where(w=>w.Tags.Where(wt=>wt.Name==t.Name).Count()>0).Count());

            window.Dispatcher.Invoke(()=>{
                TagsUpdating=true;
                if(ShowNew){
                    textFilter=null;
                    currentFilterText = "Новые";
                    window.TagsList.Text = currentFilterText;
                    Watches.ToList().ForEach(w=>{
                        w.IsVisible = w.Status==WatchStatus.New;
                    });
                }else if(textFilter!=null){
                    if(window.TagsList.Text!=textFilter)
                    window.TagsList.Text=textFilter;
                    string lf = textFilter.ToLower();
                    Watches.ToList().ForEach(w=>{
                        w.IsVisible = w.Name.ToLower().Contains(lf) || w.Comment.ToLower().Contains(lf) || w.Source.Url.ToLower().Contains(lf);
                    });
                }else{
                    ListWatchTagToStringConverter converter = new();
                    currentFilterText = converter.Convert(Tags,typeof(String),"Все",System.Globalization.CultureInfo.CurrentCulture).ToString();
                    window.TagsList.Text = currentFilterText;
                    Tags.RaiseListChangedEvents=false;
                    Tags.ToList().ForEach(t=>t.Count=counts[t.Name]);
                    Tags.RaiseListChangedEvents=true;
                    Watches.Sort((x,y)=>{
                        return y.CompareTo(x);
                    });
                    bool nofilter = !Tags.Any(t=>t.Selected??true);
                    List<string> tagExclude = Tags.Where(t=>t.Selected==null).Select(t=>t.Name).ToList();
                    List<string> tagInclude = Tags.Where(t=>t.Selected??false).Select(t=>t.Name).ToList();
                    Watches.ToList().ForEach(w=>{
                        w.IsVisible = nofilter  || w.Tags.Count==0 && tagInclude.Count==0 ||
                        !w.Tags.Any(wt=>tagExclude.Contains(wt.Name)) 
                        && w.Tags.Any(wt=>tagInclude.Count==0 || tagInclude.Contains(wt.Name));
                    });
                }
                WatchList.Items.Refresh();
                TagsUpdating=false;
            });
        }
    
        private void RefreshList()
        {
            FilterWatches();
        }

        private void EditWatch(Watch w){
            WatchWindow win = new();
            WatchWindowModel model = new(w,Tags.ToList(),win, true);
            if(win.ShowDialog()??false){
                w.CopySettingsFrom(model.Item);
                needSave=true;
                RefreshList();
            }
        }
        public void CheckpointsWatch(Watch? w){
            if(w==null) return;
            CheckpointsWindow win = new();
            CheckpointsWindowModel model = new(w,win);
            win.Show();
            NavigateWatch(w,false);
            needSave=true;
        }
    }
}