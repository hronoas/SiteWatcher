using System;

namespace SiteWatcher{
    public class Checkpoint:PropertyChangedBase,IComparable<Checkpoint>,ICloneable{
        public DateTime Time { get=>time; set=>SetField(ref time, value);}
        private DateTime time=DateTime.Now;
        public string Text { get=>text; set=>SetField(ref text, value);}
        private string text="";
        
        public string Data { get=>data; set=>SetField(ref data, value);}
        private string data;

        public bool Marked { get=>marked; set=>SetField(ref marked, value);}
        private bool marked=false;

        
        public static CheckpointDiff operator -(Checkpoint? chp1, Checkpoint? chp2) => new(chp1, chp2);
        public static bool operator ==(Checkpoint? chp1, Checkpoint? chp2) => chp1?.Text == chp2?.Text && chp1?.Data == chp2?.Data;
        public static bool operator >(Checkpoint? chp1, Checkpoint? chp2) => chp1?.Time > chp2?.Time;
        public static bool operator <(Checkpoint? chp1, Checkpoint? chp2) => chp1?.Time < chp2?.Time;
        public static bool operator !=(Checkpoint? chp1, Checkpoint? chp2) => !(chp1 == chp2);
        public Checkpoint() { }
        public Checkpoint(string content,string data="")
        {
            Text = content;
            Data = data;
        }

        public int CompareTo(Checkpoint? second)
        {
            if (second is null) return -1;
            if (this.Time > second.Time)
                return 1;
            if (this.Time < second.Time)
                return -1;
            else
                return 0;
        }
        
        public override string ToString()=> Text;

        public object Clone()=> MemberwiseClone();

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (ReferenceEquals(obj, null)) return false;
            return this==(obj as Checkpoint);
        }
    }
}