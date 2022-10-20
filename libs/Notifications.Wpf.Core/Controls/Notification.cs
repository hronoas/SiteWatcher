using Notifications.Wpf.Core.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Notifications.Wpf.Core.Controls
{
    /// <summary>
    /// Control that is used to display a notification
    /// </summary>
    [TemplatePart(Name = "PART_CloseButton", Type = typeof(Button))]
    public class Notification : ContentControl
    {
        private TimeSpan _closingAnimationTime = TimeSpan.Zero;

        /// <summary>
        /// A <see cref="Guid"/> that identifies the notification
        /// </summary>
        public Guid Identifier { get; private set; }

        /// <summary>
        /// True if notification is closing, false otherwise
        /// </summary>
        public bool IsClosing { get; set; }

        /// <summary>
        /// Routed event when notification is being closed
        /// </summary>
        public static readonly RoutedEvent NotificationCloseInvokedEvent = EventManager.RegisterRoutedEvent(
            "NotificationCloseInvoked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Notification));

        /// <summary>
        /// Routed event when notification has been closed
        /// </summary>
        public static readonly RoutedEvent NotificationClosedEvent = EventManager.RegisterRoutedEvent(
            "NotificationClosed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Notification));

        /// <summary>
        /// Constructor of the notification class
        /// </summary>
        /// <param name="identifier">The identifier that should be used for this notification</param>
        public Notification(Guid identifier)
        {
            Identifier = identifier;
        }

        static Notification()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Notification),
                new FrameworkPropertyMetadata(typeof(Notification)));
        }

        /// <summary>
        /// Event handler when close is invoked
        /// </summary>
        public event RoutedEventHandler NotificationCloseInvoked
        {
            add
            {
                AddHandler(NotificationCloseInvokedEvent, value);
            }

            remove
            {
                RemoveHandler(NotificationCloseInvokedEvent, value);
            }
        }

        /// <summary>
        /// Event handler when notification has been closed
        /// </summary>
        public event RoutedEventHandler NotificationClosed
        {
            add
            {
                AddHandler(NotificationClosedEvent, value);
            }

            remove
            {
                RemoveHandler(NotificationClosedEvent, value);
            }
        }

        /// <summary>
        /// Getter of the CloseOnClick property.
        /// </summary>
        /// <returns>True if notification should be closed on a click on it, false otherwise</returns>
        public static bool GetCloseOnClick(DependencyObject obj)
        {
            return (bool)obj.GetValue(CloseOnClickProperty);
        }

        /// <summary>
        /// Setter of the CloseOnClick property
        /// </summary>
        /// <param name="obj">The dependency object</param>
        /// <param name="value">True if notification should be closed on a click on it, false otherwise</param>
        public static void SetCloseOnClick(DependencyObject obj, bool value)
        {
            obj.SetValue(CloseOnClickProperty, value);
        }

        /// <summary>
        /// Dependency property for CloseOnClick
        /// </summary>
        public static readonly DependencyProperty CloseOnClickProperty =
            DependencyProperty.RegisterAttached("CloseOnClick", typeof(bool), typeof(Notification), new FrameworkPropertyMetadata(false, CloseOnClickChanged));

        private static void CloseOnClickChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is Button button)
            {
                var value = (bool)dependencyPropertyChangedEventArgs.NewValue;

                if (value)
                {
                    button.Click += async (sender, args) =>
                    {
                        var notification = VisualTreeHelperExtensions.GetParent<Notification>(button);

                        if (notification != null)
                        {
                            await notification.CloseAsync();
                        }
                    };
                }
            }
        }

        /// <summary>
        /// Gets called just before the UI element is displayed
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("PART_CloseButton") is Button closeButton)
            {
                closeButton.Click += OnCloseButtonOnClickAsync;
            }

            var storyboards = Template.Triggers.OfType<EventTrigger>()
                .FirstOrDefault(t => t.RoutedEvent == NotificationCloseInvokedEvent)?.Actions.OfType<BeginStoryboard>()
                .Select(a => a.Storyboard);

            _closingAnimationTime = new TimeSpan(storyboards?.Max(s => Math.Min((s.Duration.HasTimeSpan ? s.Duration.TimeSpan + (s.BeginTime ?? TimeSpan.Zero) : TimeSpan.MaxValue).Ticks,
                s.Children.Select(ch => ch.Duration.TimeSpan + (s.BeginTime ?? TimeSpan.Zero)).Max().Ticks)) ?? 0);
        }

        private async void OnCloseButtonOnClickAsync(object sender, RoutedEventArgs args)
        {
            if (sender is Button button)
            {
                button.Click -= OnCloseButtonOnClickAsync;
                await CloseAsync();
            }
        }

        /// <summary>
        /// Closes the notification
        /// </summary>
        public async Task CloseAsync()
        {
            if (IsClosing)
            {
                return;
            }

            IsClosing = true;

            RaiseEvent(new RoutedEventArgs(NotificationCloseInvokedEvent));
            await Task.Delay(_closingAnimationTime);
            RaiseEvent(new RoutedEventArgs(NotificationClosedEvent));
        }
    }
}