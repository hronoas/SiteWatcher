using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
namespace SiteWatcher{
    public partial class ConfigWindowModel : BaseWindowModel<ConfigWindow>{
        public SortableBindingList<WatchTag> Tags  {get;set;}
        public Command SaveCommand {get;set;}
        public Command CancelCommand {get;set;}
        public Command AddTagCommand {get;set;}
        public Command<WatchTag> RemoveTagCommand {get;set;}
        public Command CloseWindowCommand {get;set;}
        public ConfigWindowModel(List<WatchTag> tags,ConfigWindow win) : base(win){
            win.DataContext = this;
            Tags= new(tags);
            SaveCommand=new(o=>{window.DialogResult=true; window.Close();});
            CancelCommand=new(o=>{window.DialogResult=false; window.Close();});
            AddTagCommand=new(o=>Tags.Add(new WatchTag()));
            RemoveTagCommand=new(t=>Tags.Remove(t));
            CloseWindowCommand = new(o=>win.Close());
        
        }
    }
}