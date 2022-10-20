using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Notifications.Wpf.Core.Controls
{
    /// <summary>
    /// Control that is used to provide an area where notification can be displayed
    /// </summary>
    public class NotificationArea : Control
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Position where notifications are displayed inside the notification area
        /// </summary>
        public NotificationPosition Position
        {
            get
            {
                return (NotificationPosition)GetValue(PositionProperty);
            }
            set
            {
                SetValue(PositionProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for Position.  This enables animation, styling, binding, etc...
        /// <summary>
        /// Dependency property of Position
        /// </summary>
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register(nameof(Position), typeof(NotificationPosition), typeof(NotificationArea),
                new PropertyMetadata(NotificationPosition.BottomRight));

        /// <summary>
        /// Maximum items that can be shown before the first one gets closed to
        /// make room for the new one
        /// </summary>
        public int MaxItems
        {
            get
            {
                return (int)GetValue(MaxItemsProperty);
            }
            set
            {
                SetValue(MaxItemsProperty, value);
            }
        }

        /// <summary>
        /// Dependency property of MaxItems
        /// </summary>
        public static readonly DependencyProperty MaxItemsProperty =
            DependencyProperty.Register(nameof(MaxItems), typeof(int), typeof(NotificationArea),
                new PropertyMetadata(int.MaxValue));

        /// <summary>
        /// Property that can be used when the identifier should be defined via a binding instead of the name property
        /// </summary>
        public string? BindableName
        {
            get
            {
                return (string?)GetValue(BindableNameProperty);
            }
            set
            {
                SetValue(BindableNameProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the BindableName
        /// </summary>
        public static readonly DependencyProperty BindableNameProperty =
            DependencyProperty.Register(nameof(BindableName), typeof(string), typeof(NotificationArea), new PropertyMetadata(null));

        /// <summary>
        /// Identifier that identifies the notification area
        /// </summary>
        public string Identifier
        {
            get
            {
                return BindableName ?? Name;
            }
        }

        private IList? _items;

        /// <summary>
        /// Constructor for the NotificationArea class
        /// </summary>
        public NotificationArea()
        {
            NotificationManager.AddArea(this);
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }

        private void Dispatcher_ShutdownStarted(object? sender, EventArgs e)
        {
            // Clean up resources
            NotificationManager.RemoveArea(this);
            _semaphore?.Dispose();
        }

        static NotificationArea()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NotificationArea),
                new FrameworkPropertyMetadata(typeof(NotificationArea)));
        }

        /// <summary>
        /// Gets called just before the UI element is displayed
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var itemsControl = GetTemplateChild("PART_Items") as Panel;
            _items = itemsControl?.Children;
        }

        /// <summary>
        /// Shows a toast inside the notification area
        /// </summary>
        /// <param name="content">The content that should be displayed</param>
        /// <param name="expirationTime">A <see cref="TimeSpan"/> after which the toast disappears</param>
        /// <param name="onClick">An action that is triggered when the toast is clicked. The notification identifier is supplied as argument</param>
        /// <param name="onClose">An action that is triggered when the toast closes. The notification identifier is supplied as argument</param>
        /// <param name="token">The cancellation token that should be used</param>
        [Obsolete("This method is deprecated. Please use the new ShowAsync that includes an identifier instead", false)]
        public async Task ShowAsync(object content, TimeSpan expirationTime, Action? onClick, Action? onClose, CancellationToken token = default)
        {
            await ShowAsync(Guid.NewGuid(), content, expirationTime, (i) => onClick?.Invoke(), (i) => onClose?.Invoke(), token);
        }

        /// <summary>
        /// Shows a toast inside the notification area
        /// </summary>
        /// <param name="identifier">The identifier used for the notification</param>
        /// <param name="content">The content that should be displayed</param>
        /// <param name="expirationTime">A <see cref="TimeSpan"/> after which the toast disappears</param>
        /// <param name="onClick">An action that is triggered when the toast is clicked. The notification identifier is supplied as argument</param>
        /// <param name="onClose">An action that is triggered when the toast closes. The notification identifier is supplied as argument</param>
        /// <param name="token">The cancellation token that should be used</param>
        public async Task ShowAsync(Guid identifier, object content, TimeSpan expirationTime, Action<Guid>? onClick, Action<Guid>? onClose, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            var notification = new Notification(identifier)
            {
                Content = content
            };

            notification.MouseLeftButtonDown += async (sender, args) =>
            {
                if (onClick != null)
                {
                    onClick.Invoke(identifier);

                    if (sender is Notification senderNotification)
                    {
                        await senderNotification.CloseAsync();
                    }
                }
            };

            notification.NotificationClosed += (sender, args) => onClose?.Invoke(identifier);
            notification.NotificationClosed += OnNotificationClosed;

            if (!IsLoaded || _items == null)
            {
                return;
            }

            var w = Window.GetWindow(this);
            var x = PresentationSource.FromVisual(w);

            if (x == null)
            {
                return;
            }

            try
            {
                await _semaphore.WaitAsync(token);

                try
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    _items.Add(notification);

                    if (_items.OfType<Notification>().Count(i => !i.IsClosing) > MaxItems)
                    {
                        await _items.OfType<Notification>().First(i => !i.IsClosing).CloseAsync();
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (expirationTime != TimeSpan.MaxValue)
            {
                await Task.Delay(expirationTime);
                await notification.CloseAsync();
            }
        }

        /// <summary>
        /// Closes a toast message inside the notification area, if it is currently visible
        /// </summary>
        /// <param name="identifier">The identifier of the notification</param>
        public async Task CloseAsync(Guid identifier)
        {
            var notifications = _items?.OfType<Notification>()?.Where(x => x.Identifier == identifier)?.ToList();

            await CloseNotificationsAsync(notifications);
        }

        /// <inheritdoc/>
        /// <summary>
        /// Closes all currently visible toast messages in the notification area
        /// </summary>
        public async Task CloseAllAsync()
        {
            var notifications = _items?.OfType<Notification>()?.ToList();

            await CloseNotificationsAsync(notifications);
        }

        private async Task CloseNotificationsAsync(IList<Notification>? notifications)
        {
            if (notifications != null && notifications.Count > 0)
            {
                foreach (var item in notifications)
                {
                    await item.CloseAsync();
                }
            }
        }

        private void OnNotificationClosed(object sender, RoutedEventArgs routedEventArgs)
        {
            var notification = sender as Notification;
            _items?.Remove(notification);
        }
    }

    /// <summary>
    /// Enum for specification of the position where toast messages are shown
    /// </summary>
    public enum NotificationPosition
    {
        /// <summary>
        /// Top left edge
        /// </summary>
        TopLeft,

        /// <summary>
        /// Top right edge
        /// </summary>
        TopRight,

        /// <summary>
        /// Bottom left edge
        /// </summary>
        BottomLeft,

        /// <summary>
        /// Bottom right edge
        /// </summary>
        BottomRight
    }
}