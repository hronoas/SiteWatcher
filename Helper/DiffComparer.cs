using System;
using System.Collections.Generic;
using System.Linq;
using Menees.Diffs;

namespace SiteWatcher{

    public enum DiffType{
        Normal=0,
        Change=1,
        Delete=-1,
        Add=2
    }

    public class DiffPart{
        public DiffPart(string text, DiffType type,string afix)
        {
            Text = text;
            Other = "";
            Type = type;
            Afix = afix;
        }

        public DiffPart(DiffPart source){
            Text = source.Text;
            Other = source.Other;
            Type = source.Type;
            Afix = source.Afix;
        }

        public string Text { get; set; }
        public string Other { get; set; }
        public DiffType Type {get;set ;}
        public string Afix { get; set; }

    }

    public struct DiffEditStr{
        public string A;
        public string B;
    }
    public static class DiffComparer{

        const string defSplitter="\n";
        public static List<DiffPart> CompareStrings(string oldString,string newString,string Splitter = defSplitter,int maxDiffLength=0, bool oneLevel=false){
            if(String.IsNullOrEmpty(oldString)) return (new List<DiffPart>(){new DiffPart(newString,DiffType.Add,Splitter)} );
            TextDiff diff = new(HashType.Crc32, true, true);
            string[] a = oldString.Split(Splitter);
            string[] b = newString.Split(Splitter);
            EditScript edit = diff.Execute(a,b);
            if(maxDiffLength>0 && edit.Where(e=>e.Length>maxDiffLength).Count()>0){
                return (new List<DiffPart>(){new DiffPart(newString,DiffType.Change,Splitter){Other=oldString}} );
            }

            (string A,string B) getEdit(string[] strA,string[] strB,Edit e){
                return (String.Join(Splitter,strA.Take(new Range(e.StartA,e.StartA+e.Length))),String.Join(Splitter,strB.Take(new Range(e.StartB,e.StartB+e.Length))));
            };
            void addresult(List<DiffPart> res,DiffPart? d){
                if(d==null) return;
                DiffPart? last = res.LastOrDefault();
                if(last!=null && last.Type==d.Type){
                    last.Text+=d.Afix+d.Text;
                    last.Other+=d.Afix+d.Other;
                }else{
                    res.Add(d);
                }
            }
            void trimresult(List<DiffPart?> res,int strat,int len){
                for (var x = strat; x < Math.Min(strat+len,res.Count); x++){
                    res[x]=null;
                }
            }
            List<DiffPart> result2=new(a.Select(s=>new DiffPart(s,DiffType.Normal,Splitter)));
            Dictionary<int,List<DiffPart>> subs=new();
            for (var i = 0; i < edit.Count; i++){
                Edit e= edit[i];
                (string Astr, string Bstr) = getEdit(a,b,e);
                switch (e.EditType){
                    case EditType.Delete:
                        DiffPart delete = new(Astr,DiffType.Delete,Splitter);
                        subs[e.StartA]=(new(){delete});
                        break;
                    case EditType.Insert:
                        DiffPart insert = new(Bstr,DiffType.Add,Splitter);
                        DiffPart normal = new(Astr,DiffType.Normal,Splitter);
                        subs[e.StartA]=(new(){insert,normal});
                        break;
                    case EditType.Change:
                        if(Splitter == defSplitter && e.Length==1 && !oneLevel){
                            List<DiffPart> sub = CompareStrings(Astr,Bstr," ",3);
                            sub[0].Afix=Splitter;
                            subs[e.StartA]=sub;
                        }else{
                            DiffPart change = new(Bstr,DiffType.Change,Splitter){Other=Astr};
                            subs[e.StartA]=new(){change};
                        }
                        break;
                    default:
                        DiffPart nonrmal = new(Astr,DiffType.Normal,Splitter);
                        subs[e.StartA]=new(){nonrmal};
                        break;
                }
                trimresult(result2,e.StartA,e.Length);
            }
            List<DiffPart> result = new();
            for (var i = 0; i < Math.Max(result2.Count, (subs.Keys.Count>0?subs.Keys.Max():-1)+1); i++){
                if(subs.ContainsKey(i)){
                    subs[i].ForEach(s=>addresult(result,s));
                }else if (i<result2.Count){
                    addresult(result,result2[i]);
                }
            }
            
            return result;
        }
    }
}