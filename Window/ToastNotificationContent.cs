using System;
using System.Windows;
using Notifications.Wpf.Core;
using Notifications.Wpf.Core.Controls;

namespace SiteWatcher
{
    public class WatchNotificationContent:NotificationContent{
        public Watch Item {get;set;}

        public void Click(){
            Item.Navigate(true);
        }
        
        public WatchNotificationContent(Watch obj){
            Item = obj;
            MarkRead=new Command(o=>{
                obj.Navigate(false);
            });

            Type=NotificationType.Information;
        }
        public Command MarkRead {get;set;}
    }

    public class ToastNotificationTemplateSelector : NotificationTemplateSelector
    {
        public ToastNotificationTemplateSelector()
        {
            
        }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is WatchNotificationContent)
            {
                return (container as FrameworkElement)?.FindResource("WatchNotificationTemplate") as DataTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}