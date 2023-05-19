using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Menees.Diffs;

namespace SiteWatcher
{
    [ValueConversion(typeof(TimeSpan), typeof(String))]
    public class HoursMinutesTimeSpanConverter : IValueConverter{
        private TimeSpan StrToTime(string str){
            int Days=0;
            int Hours=0;
            int Minutes=0;
            int Seconds=0;
            var match = Regex.Match(str.ToString()??"",@"(?:([0-9]+)[dд])",RegexOptions.IgnoreCase);
            if(match.Success) Days=Int16.Parse(match.Groups[1].Value);
            match = Regex.Match(str.ToString()??"",@"(?:([0-9]+)[hч])",RegexOptions.IgnoreCase);
            if(match.Success) Hours=Int16.Parse(match.Groups[1].Value);
            match = Regex.Match(str.ToString()??"",@"(?:([0-9]+)[mм])",RegexOptions.IgnoreCase);
            if(match.Success) Minutes=Int16.Parse(match.Groups[1].Value);
            match = Regex.Match(str.ToString()??"",@"(?:([0-9]+)[sс])",RegexOptions.IgnoreCase);
            if(match.Success) Seconds=Int16.Parse(match.Groups[1].Value);
            return new TimeSpan(Days,Hours,Minutes,Seconds);
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture){
            // TODO something like:
            TimeSpan val=(TimeSpan)value;
            return (val.Days>0?$"{val.Days}д":"")+(val.Hours>0?$"{val.Hours}ч":"")+(val.Minutes>0?$"{val.Minutes}м":"")+(val.Seconds>0?$"{val.Seconds}с":"");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture){
            TimeSpan time = StrToTime(value?.ToString()??"");
            TimeSpan min = StrToTime(parameter?.ToString()??"");
            return min>time?min:time;
        }
    }

[ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter: IValueConverter{
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture){
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture){
            throw new NotSupportedException();
        }

        #endregion
    }

    [ValueConversion(typeof(DateTime), typeof(String))]
    public class DateToNowRelevanceConverter : System.Windows.Markup.MarkupExtension,IValueConverter{
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture){
            // TODO something like:
            TimeSpan span=DateTime.Now - (DateTime)value;
            string result = "";
            if(span.Days>0) result = span.Days+" "+ chisl(span.Days,"день","дня","дней")+" назад";
            else if(span.Hours>0) result = span.Hours+" "+ chisl(span.Hours,"час","часа","часов")+" назад";
            else if(span.Minutes>0) result = span.Minutes+" "+ chisl(span.Minutes,"минута","минуты","минут")+" назад";
            else result = "только что";
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture){
             return null;
        }

        public override object ProvideValue(IServiceProvider serviceProvider){
            return this;
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    public class CutLeftConverter : System.Windows.Markup.MarkupExtension, IValueConverter{
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture){
            int minIndex = 150;
            int maxIndex = 255;
            string? parameterString = parameter as string;
            if (!string.IsNullOrEmpty(parameterString)){
                string[] parameters = parameterString.Split(new char[]{'-'});
                try{
                    minIndex = int.Parse(parameters[0]);
                }catch{}
                try{
                    if(parameters.Length>1)
                    maxIndex = int.Parse(parameters[1]);
                }catch{}
            }

            string str = value?.ToString()??"";
            int pos = str.IndexOf("\n",Math.Min(minIndex,str.Length));
            if(pos<0) pos = Math.Min(maxIndex,str.Length);
            return (str).Substring(0,pos);
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture){
            return null;
        }
        public override object ProvideValue(IServiceProvider serviceProvider){            
            return this;
        }
    }

    [ValueConversion(typeof(WatchStatus), typeof(String))]
    public class WatchStatusToStringConverter : System.Windows.Markup.MarkupExtension,IValueConverter{
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture){
            // TODO something like:
            WatchStatus status = (WatchStatus)value;
            switch (status){
                case WatchStatus.Fail: return "Ошибка";
                case WatchStatus.Queued: return "В очереди";
                case WatchStatus.Checking: return "Проверка";
                case WatchStatus.New: return "Новое";
                case WatchStatus.NoChanges: return "Нет изменений";
                case WatchStatus.Off: return "Отключено";
                default: return status.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture){
             return null;
        }

        public override object ProvideValue(IServiceProvider serviceProvider){
            return this;
        }
    }

    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringNullOrEmptyToVisibilityConverter : System.Windows.Markup.MarkupExtension, IValueConverter{
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture){
            return string.IsNullOrEmpty(value as string) 
                ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return null;
        }
        public override object ProvideValue(IServiceProvider serviceProvider){            
            return this;
        }
    }

    [ValueConversion(typeof(IEnumerable<WatchTag>), typeof(string))]
    public class ListWatchTagToStringConverter : System.Windows.Markup.MarkupExtension, IValueConverter{
        public static string defSplitter = ", ";
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture){
            string placeholder = parameter?.ToString()??"";
            IEnumerable<WatchTag>? val = value as IEnumerable<WatchTag>;
            if(val==null) return placeholder;
            IEnumerable<WatchTag>? tags = val.Where(t=>t.Selected??true);
            if(tags==null || tags.Count()==0) return placeholder;
            return String.Join(defSplitter,tags.Select(t=>((t.Selected==null?"!":"")+t.Name)));
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture){
            return null;
        }
        public override object ProvideValue(IServiceProvider serviceProvider){            
            return this;
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    public class UrlToFilenameConverter : System.Windows.Markup.MarkupExtension, IValueConverter{
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture){
            string filename = Path.Combine(AppIcons,AppName+".ico");
            if(!File.Exists(filename)){
                Directory.CreateDirectory(Path.GetDirectoryName(filename)??AppIcons);
                //File.WriteAllBytes(filename,H.Resources.SiteWatcher_ico.AsBytes());
                using (var stream = new FileStream(filename,FileMode.Create,FileAccess.Write))
                {
                    ReadResource("SiteWatcher.ico")?.BaseStream.CopyTo(stream);
                } 
            }
            try{
                string url= value as string??"";
                Uri uri = new(url);
                string newfilename = Path.Combine(AppIcons, uri.Host+".ico");
                if(!File.Exists(newfilename)){
                    Directory.CreateDirectory(Path.GetDirectoryName(newfilename)??AppIcons);
                    CheckBrowser.SaveIconAsync(url,newfilename);
                }else{
                    filename=newfilename;
                }
            }catch{
            }
            return filename;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture){
            return null;
        }
        public override object ProvideValue(IServiceProvider serviceProvider){            
            return this;
        }
    }

    public enum RunType{
        Normal=0,
        Delete=-2,
        Replace=-1,
        Change=1,
        Add=2
    }
    public class DiffTextOldToNewConverter : System.Windows.Markup.MarkupExtension,IMultiValueConverter{
        const string defSplitter="\n";
        private void addInlines(TextBlock targetTextBlock, string oldstring,string newstring,string splitter=defSplitter){
            List<DiffPart> diffs = DiffComparer.CompareStrings(oldstring,newstring,splitter);
            diffs.ForEach(d=>{
                string Text = Regex.Replace(d.Text,@"[\r\n]+","\n");
                string Other = Regex.Replace(d.Other,@"[\r\n]+","\n");
                if(Text!="\n"){
                    if(targetTextBlock.Inlines.Count>0) targetTextBlock.Inlines.Add(new Run(d.Afix));
                    switch (d.Type){
                        case DiffType.Delete:
                            targetTextBlock.Inlines.Add(new Run(Text){
                                Tag=RunType.Delete,
                            });
                            break;
                        case DiffType.Add:
                            targetTextBlock.Inlines.Add(new Run(Text){
                                Tag=RunType.Add
                            });
                            break;
                        case DiffType.Change:
                            if(Other=="\n")
                            targetTextBlock.Inlines.Add(new Run(Other){
                                Tag=RunType.Replace
                            });
                            targetTextBlock.Inlines.Add(new Run(Text){
                                Tag=RunType.Change
                            });
                            break;
                        default:
                            targetTextBlock.Inlines.Add(new Run(Text){
                                Tag=RunType.Normal
                            });
                            break;
                    }
                }
            });
        }
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture) {
            string oldstring = (value[0] as string)??"";
            string newstring = (value[1] as string)??"";
            string splitter = (parameter as string)??defSplitter;
            if (newstring != null)
            {
                var textBlock = new TextBlock();
                textBlock.TextWrapping = TextWrapping.NoWrap;
                
                addInlines(textBlock,oldstring,newstring,splitter);
                
                return textBlock;
            }

            return null;
        }

        public override object ProvideValue(IServiceProvider serviceProvider){
            return this;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture){
            return null;
        }
    }
}