using Notifications.Wpf.Core.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Notifications.Wpf.Core
{
    /// <summary>
    /// Implementation of the <see cref="INotificationManager"/> interface. Provides methods to show toast messages
    /// </summary>
    public class NotificationManager : INotificationManager
    {
        private static readonly List<NotificationArea> Areas = new List<NotificationArea>();
        private static NotificationsOverlayWindow? _window;

        private readonly Dispatcher _dispatcher;
        private readonly NotificationPosition _mainNotificationPosition;

        /// <summary>
        /// Creates an instance of the <see cref="NotificationManager"/>
        /// </summary>
        public NotificationManager() : this(NotificationPosition.BottomRight, null)
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="NotificationManager"/>
        /// </summary>
        /// <param name="mainNotificationPosition">The position where notifications with no custom area should
        /// be displayed</param>
        public NotificationManager(NotificationPosition mainNotificationPosition) : this(mainNotificationPosition, null)
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="NotificationManager"/>
        /// </summary>
        /// <param name="dispatcher">The <see cref="Dispatcher"/> that should be used</param>
        public NotificationManager(Dispatcher? dispatcher) : this(NotificationPosition.BottomRight, dispatcher)
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="NotificationManager"/>
        /// </summary>
        /// <param name="mainNotificationPosition">The position where notifications with no custom area should
        /// be displayed</param>
        /// <param name="dispatcher">The <see cref="Dispatcher"/> that should be used</param>
        public NotificationManager(NotificationPosition mainNotificationPosition,
            Dispatcher? dispatcher)
        {
            _mainNotificationPosition = mainNotificationPosition;

            if (dispatcher == null)
            {
                dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            }

            _dispatcher = dispatcher;
        }

        /// <inheritdoc cref="INotificationManager.ShowAsync(string, string?, TimeSpan?, Action?, Action?, CancellationToken)"/>
        public async Task ShowAsync(string text, string? areaName = null, TimeSpan? expirationTime = null, Action? onClick = null,
            Action? onClose = null, CancellationToken token = default)
        {
            await ShowAsync(Guid.NewGuid(), text, areaName, expirationTime,
                (i) => onClick?.Invoke(), (i) => onClose?.Invoke(), token);
        }

        /// <inheritdoc cref="INotificationManager.ShowAsync(Guid, string, string?, TimeSpan?, Action{Guid}?, Action{Guid}?, CancellationToken)"/>
        public async Task ShowAsync(Guid identifier, string text, string? areaName = null, TimeSpan? expirationTime = null,
            Action<Guid>? onClick = null, Action<Guid>? onClose = null, CancellationToken token = default)
        {
            await InternalShowAsync(identifier, text, areaName, expirationTime,
                onClick, onClose, token);
        }

        /// <inheritdoc cref="INotificationManager.ShowAsync(NotificationContent, string?, TimeSpan?, Action?, Action?, CancellationToken)"/>
        public async Task ShowAsync(NotificationContent content, string? areaName = null, TimeSpan? expirationTime = null,
            Action? onClick = null, Action? onClose = null, CancellationToken token = default)
        {
            await ShowAsync(Guid.NewGuid(), content, areaName, expirationTime,
                (i) => onClick?.Invoke(), (i) => onClose?.Invoke(), token);
        }

        /// <inheritdoc cref="INotificationManager.ShowAsync(Guid, NotificationContent, string?, TimeSpan?, Action{Guid}?, Action{Guid}?, CancellationToken)"/>
        public async Task ShowAsync(Guid identifier, NotificationContent content, string? areaName = null, TimeSpan? expirationTime = null,
            Action<Guid>? onClick = null, Action<Guid>? onClose = null, CancellationToken token = default)
        {
            await InternalShowAsync(identifier, content, areaName, expirationTime,
                onClick, onClose, token);
        }

        /// <inheritdoc cref="INotificationManager.ShowAsync{TViewModel}(TViewModel, string?, TimeSpan?, Action?, Action?, CancellationToken)"/>
        public async Task ShowAsync<TViewModel>(TViewModel viewModel, string? areaName = null, TimeSpan? expirationTime = null,
            Action? onClick = null, Action? onClose = null, CancellationToken token = default)
            where TViewModel : INotificationViewModel
        {
            await ShowAsync(Guid.NewGuid(), viewModel, areaName, expirationTime,
                (i) => onClick?.Invoke(), (i) => onClose?.Invoke(), token);
        }

        /// <inheritdoc cref="INotificationManager.ShowAsync{TViewModel}(Guid, TViewModel, string?, TimeSpan?, Action{Guid}?, Action{Guid}?, CancellationToken)"/>
        public async Task ShowAsync<TViewModel>(Guid identifier, TViewModel viewModel, string? areaName = null, TimeSpan? expirationTime = null,
            Action<Guid>? onClick = null, Action<Guid>? onClose = null, CancellationToken token = default) where TViewModel : INotificationViewModel
        {
            viewModel.SetNotificationIdentifier(identifier);

            await InternalShowAsync(identifier, viewModel, areaName, expirationTime,
                onClick, onClose, token);
        }

        internal static void AddArea(NotificationArea area)
        {
            Areas.Add(area);
        }

        internal static void RemoveArea(NotificationArea area)
        {
            Areas.Remove(area);
        }

        private async Task InternalShowAsync(Guid identifier, object content, string? areaName, TimeSpan? expirationTime, Action<Guid>? onClick,
           Action<Guid>? onClose, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            if (!_dispatcher.CheckAccess())
            {
                await _dispatcher.BeginInvoke(
                    new Action(async () => await InternalShowAsync(identifier, content, areaName, expirationTime, onClick, onClose, token)));
                return;
            }

            if (expirationTime == null)
            {
                expirationTime = TimeSpan.FromSeconds(5);
            }

            if (string.IsNullOrEmpty(areaName))
            {
                areaName = "NotificationsOverlayWindow_NotifyArea";

                if (_window == null)
                {
                    var workArea = SystemParameters.WorkArea;

                    _window = new NotificationsOverlayWindow
                    {
                        Left = workArea.Left,
                        Top = workArea.Top,
                        Width = workArea.Width,
                        Height = workArea.Height,
                        //Owner = Application.Current.MainWindow
                    };

                    _window.SetNotificationAreaPosition(_mainNotificationPosition);
                    _window.Show();
                }
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            foreach (var area in Areas.Where(a => a.Identifier == areaName).ToList())
            {
                await area.ShowAsync(identifier, content, (TimeSpan)expirationTime, onClick, onClose, token);
            }
        }

        /// <inheritdoc cref="INotificationManager.CloseAsync(Guid)"/>
        public async Task CloseAsync(Guid identifier)
        {
            foreach (var area in Areas.ToList())
            {
                await area.CloseAsync(identifier);
            }
        }

        /// <inheritdoc cref="INotificationManager.CloseAllAsync"/>
        public async Task CloseAllAsync()
        {
            foreach (var area in Areas.ToList())
            {
                await area.CloseAllAsync();
            }
        }
    }
}