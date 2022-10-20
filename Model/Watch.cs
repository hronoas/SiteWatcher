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
        public bool Enabled { get=>enabled; set{SetField(ref enabled, value);ChangedField(nameof(Status));}}
        private bool enabled = true;
        public string Name { get=>name; set=>SetField(ref name, value);}
        private string name="";
        public WatchSource Source { get; set;} = new();
        public BindingList<WatchTag> Tags {get;set;} = new();
        public string Error { get=>error; set{SetField(ref error, value);ChangedField(nameof(Status));}}
        private string error="";
        public TimeSpan Interval { get=>interval; set=>SetField(ref interval, value);}
        private TimeSpan interval = new TimeSpan(3,0,0);
        public DateTime LastCheck { get=>lastCheck; set=>SetField(ref lastCheck, value);}
        private DateTime lastCheck;
        public DateTime LastSeen { get=>lastSeen; set{SetField(ref lastSeen, value);ChangedField(nameof(IsNew));ChangedField(nameof(Status));}}
        private DateTime lastSeen;
        public bool Notify { get=>notify; set=>SetField(ref notify, value);}
        private bool notify = true;
        public BindingList<Checkpoint> Checkpoints { get; set; } = new();
        public int MaxCheckpoints { get=>maxCheckpoints; set=>SetField(ref maxCheckpoints, value);}
        private int maxCheckpoints = 10;
        [JsonIgnoreAttribute()]
        public bool IsChecking { get=>isChecking; set{SetField(ref isChecking, value);ChangedField(nameof(Status));}}
        private bool isChecking = false;
        [JsonIgnoreAttribute()]
        public bool IsQueued { get=>isQueued; set{SetField(ref isQueued, value);ChangedField(nameof(Status));}}
        private bool isQueued = false;
        [JsonIgnoreAttribute()]
        public bool IsVisible { get=>isVisible; set=>SetField(ref isVisible, value);}
        private bool isVisible = true;


        public void Check(Action onReady){
            if(IsChecking && (DateTime.Now-LastCheck) < new TimeSpan(0,5,0)) return;
            void onData(List<SelectorResult> results){
                string CheckpointText="";
                string CheckpointData="";
                results.ForEach(item=>{
                    if(!String.IsNullOrWhiteSpace(item.Text)) CheckpointText+=(CheckpointText==""?"":"\n")+item.Text;
                    if(!String.IsNullOrWhiteSpace(item.Data)) CheckpointData+=(CheckpointData==""?"":"\n")+item.Data;
                });
                if(Source.CheckData?Diff.Next.Data!=CheckpointData:Diff.Next.Text!=CheckpointText){
                    Checkpoints.RaiseListChangedEvents=false;
                    Checkpoints.Add(new Checkpoint(CheckpointText,CheckpointData));
                    if(Checkpoints.Count>MaxCheckpoints){
                        Checkpoints.OrderBy(x=>x.Time).Reverse().Skip(MaxCheckpoints).ToList().ForEach(x=>Checkpoints.Remove(x));
                    }
                    Checkpoints.RaiseListChangedEvents=true;
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
                ChangedField(nameof(Diff));
                ChangedField(nameof(IsNew));
                ChangedField(nameof(Status));
                };
        }


        [JsonIgnoreAttribute()]
        public CheckpointDiff Diff{
            get{
                if(Checkpoints.Count==0){
                    Checkpoint dummy = new Checkpoint(){Time=DateTime.MinValue};
                    return new CheckpointDiff(dummy,dummy);
                }
                Checkpoint max = Checkpoints.Max() ?? new();
                Checkpoint prev = Checkpoints.Where(c => c < max).Max() ?? new() { Time = DateTime.MinValue };
                return max - prev;
            }
        }
        [JsonIgnoreAttribute()]
        public bool IsNew {get=>LastSeen<Diff.Next.Time;}
        [JsonIgnoreAttribute()]
        public WatchStatus Status { get {
            if(isChecking) return WatchStatus.Checking;
            if(isQueued) return WatchStatus.Queued;
            if(!enabled) return WatchStatus.Off;
            if(Error!="") return WatchStatus.Fail;
            if(IsNew) return WatchStatus.New;
            return WatchStatus.NoChanges;
        } }
        public void CopySettingsFrom(Watch w){
            Enabled=w.Enabled;
            Name=w.Name;
            Source=(WatchSource)w.Source.Clone(); ChangedField(nameof(Source));
            Interval=w.Interval;
            Tags.Clear();
            w.Tags.ToList().ForEach(t=>Tags.Add(t));
            Tags.ResetBindings();
            MaxCheckpoints=w.MaxCheckpoints;
            IsChecking=false;
            Notify=w.Notify;
        }
        public object Clone(){
            Watch clone = (Watch)MemberwiseClone();
            clone.Checkpoints = new BindingList<Checkpoint>(Checkpoints.Select(x=>(Checkpoint)x.Clone()).ToList());
            clone.Tags = new BindingList<WatchTag>(Tags.Select(x=>x.Clone()).ToList());
            clone.CheckpointTrace();
            clone.Source = (WatchSource)(Source.Clone());
            clone.isChecking=false;
            return clone;
        }

        public int CompareTo(Watch? other){
            if (other == null) return -1;
            DateTime thistime = Enabled?Diff.Next.Time:DateTime.MinValue;
            DateTime othertime = other.Enabled?other.Diff.Next.Time:DateTime.MinValue;
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