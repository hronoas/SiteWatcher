using System;
using System.Windows;

namespace SiteWatcher
{
    public class CallBackJS{

        private Action<string> OnRecive {get;set;}
        private Func<string,string>? BeforeExec {get;set;} = s=>s;

        public CallBackJS(Action<string> onRecive){
            this.OnRecive = onRecive;
        }

        public void send(string str){
            OnRecive?.Invoke(BeforeExec?.Invoke(str)??"");
        }
    }
    
}