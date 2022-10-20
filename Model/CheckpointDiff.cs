using System;

namespace SiteWatcher{
    public class CheckpointDiff:PropertyChangedBase,ICloneable{
        private Checkpoint prev;
        private Checkpoint next;

        public Checkpoint Prev { get => prev; set => SetField(ref prev,value);}
        public Checkpoint Next { get => next; set => SetField(ref next,value); }
        public CheckpointDiff(Checkpoint? chp1, Checkpoint? chp2){
            if (chp2 > chp1){
                prev = chp1 ?? new();
                next = chp2 ?? new();
            }else{
                prev = chp2 ?? new();
                next = chp1 ?? new();
            }
        }
        public override string ToString(){
            return $"{Prev} -> {Next}";
        }
        public bool isChanged(){
            return !(Prev==Next);
        }
        public object Clone()=>new CheckpointDiff(Prev,Next);
    }
}