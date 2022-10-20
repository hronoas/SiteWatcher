using System;
using System.Threading;
using System.Threading.Tasks;

namespace Notifications.Wpf.Core
{
    /// <summary>
    /// Interface of the notification manager
    /// </summary>
    public interface INotificationManager
    {
        /// <summary>
        /// Shows a toast message
        /// </summary>
        /// <param name="text">The text that should be displayed</param>
        /// <param name="areaName">The name of the area in which the toast should appear</param>
        /// <param name="expirationTime">A <see cref="TimeSpan"/> after which the toast disappears</param>
        /// <param name="onClick">An action that is triggered when the toast is clicked</param>
        /// <param name="onClose">An action that is triggered when the toast closes</param>
        /// <param name="token">The cancellation token that should be used</param>
        Task ShowAsync(string text, string? areaName = null, TimeSpan? expirationTime = null, Action? onClick = null, Action? onClose = null,
                CancellationToken token = default);

        /// <summary>
        /// Shows a toast message
        /// </summary>
        /// <param name="identifier">The identifier used for the notification</param>
        /// <param name="text">The text that should be displayed</param>
        /// <param name="areaName">The name of the area in which the toast should appear</param>
        /// <param name="expirationTime">A <see cref="TimeSpan"/> after which the toast disappears</param>
        /// <param name="onClick">An action that is triggered when the toast is clicked. The notification identifier is supplied as argument</param>
        /// <param name="onClose">An action that is triggered when the toast closes. The notification identifier is supplied as argument</param>
        /// <param name="token">The cancellation token that should be used</param>
        Task ShowAsync(Guid identifier, string text, string? areaName = null, TimeSpan? expirationTime = null, Action<Guid>? onClick = null, Action<Guid>? onClose = null,
                CancellationToken token = default);

        /// <summary>
        /// Shows a toast message
        /// </summary>
        /// <param name="content">The <see cref="NotificationContent"/> that should be displayed</param>
        /// <param name="areaName">The name of the area in which the toast should appear</param>
        /// <param name="expirationTime">A <see cref="TimeSpan"/> after which the toast disappears</param>
        /// <param name="onClick">An action that is triggered when the toast is clicked</param>
        /// <param name="onClose">An action that is triggered when the toast closes</param>
        /// <param name="token">The cancellation token that should be used</param>
        Task ShowAsync(NotificationContent content, string? areaName = null, TimeSpan? expirationTime = null, Action? onClick = null, Action? onClose = null,
                CancellationToken token = default);

        /// <summary>
        /// Shows a toast message
        /// </summary>
        /// <param name="identifier">The identifier used for the notification</param>
        /// <param name="content">The <see cref="NotificationContent"/> that should be displayed</param>
        /// <param name="areaName">The name of the area in which the toast should appear</param>
        /// <param name="expirationTime">A <see cref="TimeSpan"/> after which the toast disappears</param>
        /// <param name="onClick">An action that is triggered when the toast is clicked. The notification identifier is supplied as argument</param>
        /// <param name="onClose">An action that is triggered when the toast closes. The notification identifier is supplied as argument</param>
        /// <param name="token">The cancellation token that should be used</param>
        Task ShowAsync(Guid identifier, NotificationContent content, string? areaName = null, TimeSpan? expirationTime = null, Action<Guid>? onClick = null, Action<Guid>? onClose = null,
                CancellationToken token = default);

        /// <summary>
        /// Shows a toast message
        /// </summary>
        /// <typeparam name="TViewModel">Must implement <see cref="INotificationViewModel"/></typeparam>
        /// <param name="viewModel">A Caliburn Micro view model that should be used. Must implement <see cref="INotificationViewModel"/></param>
        /// <param name="areaName">The name of the area in which the toast should appear</param>
        /// <param name="expirationTime">A <see cref="TimeSpan"/> after which the toast disappears</param>
        /// <param name="onClick">An action that is triggered when the toast is clicked</param>
        /// <param name="onClose">An action that is triggered when the toast closes</param>
        /// <param name="token">The cancellation token that should be used</param>
        Task ShowAsync<TViewModel>(TViewModel viewModel, string? areaName = null, TimeSpan? expirationTime = null, Action? onClick = null, Action? onClose = null,
                CancellationToken token = default) where TViewModel : INotificationViewModel;

        /// <summary>
        /// Shows a toast message
        /// </summary>
        /// <typeparam name="TViewModel">Must implement <see cref="INotificationViewModel"/></typeparam>
        /// <param name="identifier">The identifier used for the notification</param>
        /// <param name="viewModel">A Caliburn Micro view model that should be used. Must implement <see cref="INotificationViewModel"/></param>
        /// <param name="areaName">The name of the area in which the toast should appear</param>
        /// <param name="expirationTime">A <see cref="TimeSpan"/> after which the toast disappears</param>
        /// <param name="onClick">An action that is triggered when the toast is clicked. The notification identifier is supplied as argument</param>
        /// <param name="onClose">An action that is triggered when the toast closes. The notification identifier is supplied as argument</param>
        /// <param name="token">The cancellation token that should be used</param>
        Task ShowAsync<TViewModel>(Guid identifier, TViewModel viewModel, string? areaName = null, TimeSpan? expirationTime = null, Action<Guid>? onClick = null, Action<Guid>? onClose = null,
                CancellationToken token = default) where TViewModel : INotificationViewModel;

        /// <summary>
        /// Closes a toast message, if it is currently visible
        /// </summary>
        /// <param name="identifier">The identifier of the notification</param>
        Task CloseAsync(Guid identifier);

        /// <summary>
        /// Closes all currently visible toast messages
        /// </summary>
        Task CloseAllAsync();
    }
}