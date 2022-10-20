using System.Windows;
using System.Windows.Media;

namespace Notifications.Wpf.Core.Utils
{
    internal static class VisualTreeHelperExtensions
    {
        public static T? GetParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);

            if (parent == null)
            {
                return null;
            }

            if (parent is T tParent)
            {
                return tParent;
            }

            return GetParent<T>(parent);
        }
    }
}