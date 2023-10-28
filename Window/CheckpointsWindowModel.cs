using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CefSharp.Wpf;

namespace SiteWatcher
{
    public class CheckpointsWindowModel : BaseWindowModel<CheckpointsWindow>
    {
        public Watch Item {get;set;}
        private Watch source {get;set;}
        public List<CheckpointDiff> Diffs {get;set;} = new();
        private ListView CheckpointsList;

        public Command DeleteSelectedCommand {get;set;}
        public Command SaveCommentCommand {get;set;}
        public Command CopyTextCommand {get;set;}
        public Command CloseWindowCommand {get;set;}
        
        public CheckpointsWindowModel(Watch Source,CheckpointsWindow win) : base(win){
            CheckpointsList = window.CheckpointsList;
            DeleteSelectedCommand=new(o=>DeleteSelected());
            CloseWindowCommand = new(o=>win.Close());
            SaveCommentCommand=new(o=>SaveComment());
            CopyTextCommand = new(o=>CopyTextSelected());
            source=Source;
            Item=(Watch)Source.Clone();
            Item.Checkpoints.Add(new("",""){Time=DateTime.MinValue});
            List<Checkpoint> list = Item.Checkpoints.ToList();
            list.Sort();
            for (var i = list.Count-1; i >0; i--){
                Diffs.Add(new CheckpointDiff(list[i],list[i-1]));
            }
        }

        private void DeleteSelected(){
            if(CheckpointsList.SelectedItems.Count==0) return;
            List<DateTime> toDelete = CheckpointsList.SelectedItems.Cast<CheckpointDiff>().Select(c=>c.Next.Time).ToList();
            toDelete.ForEach(c=>{
                var i = Diffs.Where(d=>d.Next.Time==c).FirstOrDefault();
                if(i!=null)Diffs.Remove(i);
                CheckpointsList.Items.Refresh();
                var u = source.Checkpoints.Where(d=>d.Time==c).FirstOrDefault();
                if(u!=null) source.Checkpoints.Remove(u);
            });
        }
        private void CopyTextSelected(){
            string result = "";
            if(CheckpointsList.SelectedItems.Count==0) return;
            List<DateTime> toCopy = CheckpointsList.SelectedItems.Cast<CheckpointDiff>().Select(c=>c.Next.Time).ToList();
            toCopy.ForEach(c=>{
                var i = Diffs.Where(d=>d.Next.Time==c).FirstOrDefault();
                if(i!=null) result += (result==""?"":"\n") + i.Next.Text;
            });
            try{
                //System.Windows.Clipboard.SetText(result);
                System.Windows.Forms.Clipboard.SetDataObject(result,false, 5,200);
            }catch{

            }
        }

        private void SaveComment(){
            if(!Item.Comment.Equals(source.Comment)) source.Comment=Item.Comment;
            window.Close();
        }
        
    
    }
}