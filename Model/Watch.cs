using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SiteWatcher{
    public class Watch:PropertyChangedBase,ICloneable,IComparable<Watch>{
        public bool Enabled { get=>enabled; set{SetField(ref enabled, value);InvalidateStatus();}}
        private bool enabled = true;
        public string Name { get=>name; set=>SetField(ref name, value);}
        private string name="";
        public string Comment { get=>comment; set=>SetField(ref comment, value);}
        private string comment = "";
        public WatchSource Source { get; set;} = new();
        public BindingList<WatchTag> Tags {get;set;} = new();
        public string Error { get=>error; set{SetField(ref error, value);InvalidateStatus();}}
        private string error="";
        public TimeSpan Interval { get=>interval; set=>SetField(ref interval, value);}
        private TimeSpan interval = new TimeSpan(3,0,0);
        public TimeSpan ErrorInterval { get=>errorInterval; set=>SetField(ref errorInterval, value);}
        private TimeSpan errorInterval = new TimeSpan(0,30,0);
        public DateTime LastCheck { get=>lastCheck; set=>SetField(ref lastCheck, value);}
        private DateTime lastCheck;
        public DateTime LastNotify { get=>lastNotify; set=>SetField(ref lastNotify, value);}
        private DateTime lastNotify;
        public bool RepeatNotify { get=>repeatNotify; set=>SetField(ref repeatNotify, value);}
        private bool repeatNotify = false;

        public DateTime LastSeen { get=>lastSeen; set{SetField(ref lastSeen, value);ChangedField(nameof(IsNew));InvalidateStatus();}}
        private DateTime lastSeen;
        public bool Notify { get=>notify; set=>SetField(ref notify, value);}
        private bool notify = true;
        public bool SoundNotify { get=>soundNotify; set=>SetField(ref soundNotify, value);}
        private bool soundNotify = true;
        public BindingList<Checkpoint> Checkpoints { get; set; } = new();
        public int MaxCheckpoints { get=>maxCheckpoints; set=>SetField(ref maxCheckpoints, value);}
        private int maxCheckpoints = 10;
        [JsonIgnoreAttribute()]
        public bool IsChecking { get=>isChecking; set{SetField(ref isChecking, value);InvalidateStatus();}}
        private bool isChecking = false;
        [JsonIgnoreAttribute()]
        public bool IsQueued { get=>isQueued; set{SetField(ref isQueued, value);InvalidateStatus();}}
        private bool isQueued = false;
        [JsonIgnoreAttribute()]
        public bool IsVisible { get=>isVisible; set=>SetField(ref isVisible, value);}
        private bool isVisible = true;

        private CheckpointDiff? _cachedDiff;
        private void InvalidateDiff() { _cachedDiff = null; }

        private WatchStatus? _cachedStatus;
        private void InvalidateStatus() { _cachedStatus = null; ChangedField(nameof(Status)); }

        public bool UseProxy { get=>useProxy; set=>SetField(ref useProxy, value);}
        private bool useProxy = false;

        public bool NotifyAfterError { get=>notifyAfterError; set=>SetField(ref notifyAfterError, value);}
        private bool notifyAfterError = false;
        public bool NotifyRepeatedError { get=>notifyRepeatedError; set=>SetField(ref notifyRepeatedError, value);}
        private bool notifyRepeatedError;
        public bool NotifyTelegram { get=>notifyTelegram; set=>SetField(ref notifyTelegram, value);}
        private bool notifyTelegram = false;

        public string TelegramChat { get=>telegramChat; set=>SetField(ref telegramChat, value);}
        private string telegramChat = "";
        public string TelegramTemplate { get=>telegramTemplate; set=>SetField(ref telegramTemplate, value);}
        private string telegramTemplate="";

        public void Check(Action onReady){
            if(IsChecking && (DateTime.Now-LastCheck) < new TimeSpan(0,5,0)) return;
            void onData(List<SelectorResult> results){
                string CheckpointText="";
                string CheckpointData="";
                results.ForEach(item=>{
                    if(!String.IsNullOrWhiteSpace(item.Text)) CheckpointText+=(CheckpointText==""?"":"\n")+item.Text;
                    if(!String.IsNullOrWhiteSpace(item.Data)) CheckpointData+=(CheckpointData==""?"":"\n")+item.Data;
                });
                if(Diff.Next.Text!=CheckpointText){
                    Checkpoints.RaiseListChangedEvents = false;
                    Checkpoints.Add(new Checkpoint(CheckpointText, CheckpointData));
                    var sorted = Checkpoints.OrderByDescending(c => c.Time).ToList();
                    var latestMarked = sorted.FirstOrDefault(c => c.Marked);
                    var toRemove = sorted.Skip(MaxCheckpoints)
                                        .Where(c => c != latestMarked)
                                        .ToList();
                    toRemove.ForEach(x=>Checkpoints.Remove(x));
                    Checkpoints.RaiseListChangedEvents = true;
                    InvalidateDiff();
                    ChangedField(nameof(Diff));
                    ChangedField(nameof(MarkDiff));
                    ChangedField(nameof(IsNew));
                    InvalidateStatus();
                }
                if(Checkpoints.Count<2){ // after first check
                        IsNeedNotify=false;
                        LastSeen=DateTime.Now;
                }
            }
            void onError(string result){
                Error=result;
            }
            LastCheck=DateTime.Now;
            void onFinally(object r,object e){
                onReady();
            }
            Error="";
            CheckBrowser.Check(this,onData,onError,onFinally);
        }

        

        public void Toggle(bool? newState=null){
            Enabled=(newState!=null)?newState??false:!Enabled;
        }
        public void Navigate(bool open=true){
            LastSeen=DateTime.Now;
            if(open && !String.IsNullOrWhiteSpace(Source.Url)) OpenUrl(Source.Url);
        }
        public Watch(){
            CheckpointTrace();
        }

        public void CheckpointTrace(){
            Checkpoints.AddingNew+=(o,e)=>{
                InvalidateDiff();
                ChangedField(nameof(Diff));
                ChangedField(nameof(MarkDiff));
                ChangedField(nameof(IsNew));
                InvalidateStatus();
                };
            Checkpoints.ListChanged+=(o,e)=>{
                if(e.ListChangedType==ListChangedType.ItemDeleted || e.ListChangedType==ListChangedType.ItemChanged){
                    InvalidateDiff();
                    ChangedField(nameof(Diff));
                    ChangedField(nameof(MarkDiff));
                    ChangedField(nameof(IsNew));
                    InvalidateStatus();
                }
            };
        }


        [JsonIgnoreAttribute()]
        public CheckpointDiff Diff{
            get{
                if(_cachedDiff != null) return _cachedDiff;
                if(Checkpoints.Count==0){
                    Checkpoint dummy = new Checkpoint(){Time=DateTime.MinValue};
                    _cachedDiff = new CheckpointDiff(dummy,dummy);
                }else{
                    Checkpoint max = Checkpoints.Max() ?? new();
                    Checkpoint prev = Checkpoints.Where(c => c < max).Max() ?? new() { Time = DateTime.MinValue };
                    _cachedDiff = max - prev;
                }
                return _cachedDiff;
            }
        }
        [JsonIgnoreAttribute()]
        public CheckpointDiff MarkDiff{
            get{
                Checkpoint next = Diff.Next;
                Checkpoint mark = Checkpoints.Where(c => c <= next && c.Marked).Max() ?? Diff.Prev;
                return next - mark;
            }
        }
        [JsonIgnoreAttribute()]
        public bool IsNew {get=>LastSeen<Diff.Next.Time;}
        [JsonIgnoreAttribute()]
        public bool IsNeedNotify {get=> Status==WatchStatus.New && (RepeatNotify || LastNotify<Diff.Next.Time); set=>LastNotify=value?DateTime.MinValue:DateTime.Now;}
        [JsonIgnoreAttribute()]
        public bool IsNeedCheck {
            get=>Enabled && (LastCheck+Interval<=DateTime.Now || (!string.IsNullOrWhiteSpace(Error) && LastCheck+ErrorInterval<=DateTime.Now));
            set=>LastCheck=value?(DateTime.Now-Interval):DateTime.Now;
        }
        [JsonIgnoreAttribute()]
        public string LastError {get;set;} = ""; //last error sent to notify
        [JsonIgnoreAttribute()]
        public WatchStatus Status { get {
            WatchStatus newStatus;
            if(isChecking) newStatus=WatchStatus.Checking;
            else if(isQueued) newStatus=WatchStatus.Queued;
            else if(!enabled) newStatus=WatchStatus.Off;
            else if(Error!="") newStatus=WatchStatus.Fail;
            else if(IsNew) newStatus=WatchStatus.New;
            else newStatus=WatchStatus.NoChanges;
            if(_cachedStatus != newStatus){
                _cachedStatus=newStatus;
                ChangedField(nameof(Status));
            }
            return newStatus;
        } }
        public void CopySettingsFrom(Watch w){
            Enabled=w.Enabled;
            Name=w.Name;
            Comment=w.Comment;
            Source=(WatchSource)w.Source.Clone(); ChangedField(nameof(Source));
            Interval=w.Interval;
            ErrorInterval=w.ErrorInterval;
            Tags.Clear();
            w.Tags.ToList().ForEach(t=>Tags.Add(t));
            Tags.ResetBindings();
            MaxCheckpoints=w.MaxCheckpoints;
            IsChecking=false;
            isQueued=false;
            Notify=w.Notify;
            UseProxy=w.UseProxy;
            NotifyTelegram=w.NotifyTelegram;
            LastNotify=w.LastNotify;
            RepeatNotify=w.RepeatNotify;
            TelegramChat=w.TelegramChat;
            TelegramTemplate=w.TelegramTemplate;
            SoundNotify=w.SoundNotify;
            NotifyAfterError=w.NotifyAfterError;
            NotifyRepeatedError=w.NotifyRepeatedError;
        }
        public object Clone(){
            Watch clone = (Watch)MemberwiseClone();
            clone._cachedDiff = null;
            clone._cachedStatus = null;
            clone.Checkpoints = new BindingList<Checkpoint>(Checkpoints.Select(x=>(Checkpoint)x.Clone()).ToList());
            clone.Tags = new BindingList<WatchTag>(Tags.Select(x=>x.Clone()).ToList());
            clone.CheckpointTrace();
            clone.Source = (WatchSource)(Source.Clone());
            clone.isChecking=false;
            clone.isQueued=false;
            return clone;
        }

        public int CompareTo(Watch? other){
            if (other == null) return -1;
            long now = DateTime.Now.Ticks;
            long thistime = Diff.Next.Time.Ticks-(Enabled?0:now);
            long othertime = other.Diff.Next.Time.Ticks-(other.Enabled?0:now);
            if (thistime > othertime)
                return 1;
            if (thistime < othertime)
                return -1;
            else
                return 0;
        }
    }
    public enum WatchStatus{Fail=-1,Off=0,NoChanges=1,New=2,Checking=3,Queued=4}
}
