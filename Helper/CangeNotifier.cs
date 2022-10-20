using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SiteWatcher
{
    public class PropertyChangedBase : INotifyPropertyChanged{

		public event PropertyChangedEventHandler? PropertyChanged;
		protected bool SetField<T>(ref T field, T value, [CallerMemberName]string? propertyName=null)
    	{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			ChangedField(propertyName);
			return true;
    	}

		public void ChangedField(string? propertyName)
    	{
			if(propertyName!=null)
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    	}
	}
}