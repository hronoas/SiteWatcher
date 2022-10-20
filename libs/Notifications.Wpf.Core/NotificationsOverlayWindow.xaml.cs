using Notifications.Wpf.Core.Controls;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Notifications.Wpf.Core
{
    /// <summary>
    /// Interaction logic for NotificationsOverlayWindow.xaml
    /// </summary>
    public partial class NotificationsOverlayWindow : Window
    {
        /// <summary>
        /// Constructor of the NotificationsOverlayWindow class
        /// </summary>
        
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        private const int GWL_EX_STYLE = -20;
        private const int WS_EX_APPWINDOW = 0x00040000, WS_EX_TOOLWINDOW = 0x00000080;
        public NotificationsOverlayWindow(){
            InitializeComponent();
            Loaded+=OnLoad;
        }

        private void OnLoad(object sender, RoutedEventArgs e){
            var helper = new WindowInteropHelper(this).Handle;
            SetWindowLong(helper, GWL_EX_STYLE, (GetWindowLong(helper, GWL_EX_STYLE) | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);
        }

        /// <summary>
        /// Sets the position where notifications are displayed
        /// </summary>
        /// <param name="notificationPosition">The position where toast should be displayed</param>
        public void SetNotificationAreaPosition(NotificationPosition notificationPosition)
        {
            NotificationsOverlayWindow_NotifyArea.Position = notificationPosition;
        }
    }
}