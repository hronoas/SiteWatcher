using System;
using Notifications.Wpf.Core;

namespace SiteWatcher
{
    public partial class AppWindowModel : BaseWindowModel<AppWindow>{
       private readonly NotificationManager notificationManager = new NotificationManager();
        public void ShowToast(Watch watch){
            WatchNotificationContent WNC = new WatchNotificationContent(watch);
            notificationManager.ShowAsync(WNC,expirationTime:new TimeSpan(0,0,60), onClick:WNC.Click);
       }
    }

}