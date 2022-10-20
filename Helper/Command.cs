using System;
using System.Windows.Input;

namespace SiteWatcher
{
    public class Command : Command<object>
    {
        public Command(Action<object?> action) : base(action)
        {
        }
    }
    public class Command<T> : ICommand
    {
        #region Constructor
 
        public Command(Action<T?> action)
        {
            ExecuteDelegate = action;
        }
 
        #endregion
 
        #region Properties
 
        public Predicate<T?>? CanExecuteDelegate { get; set; }
        public Action<T?> ExecuteDelegate { get; set; }
 
        #endregion
 
 
        #region ICommand Members
 
        public bool CanExecute(object? parameter)
        {
            if(CanExecuteDelegate != null)
            {
                return CanExecuteDelegate((T)parameter);
            }
 
            return true;
        }
 
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
 
        public void Execute(object? parameter)
        {
            if (ExecuteDelegate != null)
            {
                ExecuteDelegate((T)parameter);
            }
        }
 
        #endregion
    }
    
}