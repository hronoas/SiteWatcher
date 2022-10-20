namespace Notifications.Wpf.Core
{
    /// <summary>
    /// Class that holds information for the content of the notification
    /// </summary>
    public class NotificationContent
    {
        /// <summary>
        /// The title which should be displayed
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The message that should be displayed
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// The type of notification. Determines which style template should be used
        /// </summary>
        public NotificationType? Type { get; set; }
    }

    /// <summary>
    /// Enum for the specification of the notification type
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// Information level, blue toast with a information icon
        /// </summary>
        Information,

        /// <summary>
        /// Success level, green toast with a check icon
        /// </summary>
        Success,

        /// <summary>
        /// Warning level, orange toast with a warning icon
        /// </summary>
        Warning,

        /// <summary>
        /// Error level, red toast with a warning a X icon
        /// </summary>
        Error
    }
}