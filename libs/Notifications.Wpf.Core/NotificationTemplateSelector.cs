using System.Windows;
using System.Windows.Controls;

namespace Notifications.Wpf.Core
{
    /// <summary>
    /// Class to help with the selection of the correct template for notifications
    /// </summary>
    public class NotificationTemplateSelector : DataTemplateSelector
    {
        private DataTemplate? _defaultStringTemplate;
        private DataTemplate? _defaultNotificationTemplate;

        public NotificationTemplateSelector()
        {
        }

        private void GetTemplatesFromResources(FrameworkElement container)
        {
            _defaultStringTemplate =
                    container?.FindResource("DefaultStringTemplate") as DataTemplate;
            _defaultNotificationTemplate =
                    container?.FindResource("DefaultNotificationTemplate") as DataTemplate;
        }

        /// <summary>
        /// Selects the correct template for the given arguments
        /// </summary>
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (_defaultStringTemplate == null && _defaultNotificationTemplate == null)
            {
                GetTemplatesFromResources((FrameworkElement)container);
            }

            if (item is string && _defaultStringTemplate != null)
            {
                return _defaultStringTemplate;
            }
            if (item is NotificationContent && _defaultNotificationTemplate != null)
            {
                return _defaultNotificationTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}