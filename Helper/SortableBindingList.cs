using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SiteWatcher{
    public class SortableBindingList<T> : BindingList<T>{
        public SortableBindingList(IList<T> list) : base(list) { }
        public void Sort() { sort(null, null); }
        public void Sort(IComparer<T> p_Comparer) { sort(p_Comparer, null); }
        public void Sort(Comparison<T> p_Comparison) { sort(null, p_Comparison); }
        private void sort(IComparer<T> p_Comparer, Comparison<T> p_Comparison){
            if(typeof(T).GetInterface(typeof(IComparable<T>).Name) != null || typeof(T).GetInterface(typeof(IComparable).Name) != null){
                bool originalValue = this.RaiseListChangedEvents;
                this.RaiseListChangedEvents = false;
                try{
                    List<T> items = (List<T>)this.Items;
                    if(p_Comparison != null) items.Sort(p_Comparison);
                    else items.Sort(p_Comparer);
                }finally{
                    this.RaiseListChangedEvents = originalValue;
                }
            }
        }
    }
}