using System.Windows;

namespace SiteWatcher
{
    public abstract class BaseWindowModel<T>:PropertyChangedBase where T: Window
    {
        protected T window;

        public BaseWindowModel(T win){
            window=win;
            window.DataContext = this;
        }
    }
}