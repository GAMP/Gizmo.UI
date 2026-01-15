using System.Drawing;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// Notifications service.
    /// </summary>
    public interface INotificationsService
    {
        public event EventHandler<NotificationsChangedArgs>? NotificationsChanged;
        public event EventHandler<NotificationHostSizeRequestArgs>? SizeRequest;

        /// <summary>
        /// Gets visible notifications.
        /// </summary>
        /// <returns></returns>
        IEnumerable<INotificationController> GetVisible();

        /// <summary>
        /// Gets dismissed notifications.
        /// </summary>
        /// <returns></returns>
        IEnumerable<INotificationController> GetDismissed();

        /// <summary>
        /// Acknowledge all notifications.
        /// </summary>
        void AcknowledgeAll();

        /// <summary>
        /// Acknowledge specified notification.
        /// </summary>
        /// <param name="notificationId">Notification identifier.</param>
        /// <returns></returns>
        bool TryAcknowledge(int notificationId);

        /// <summary>
        /// Dismiss all notifications.
        /// </summary>
        void DismissAll();

        /// <summary>
        /// Suspends timeout timer for all notifications.
        /// </summary>
        void SuspendTimeOutAll();

        /// <summary>
        /// Resume timeout timer for all notifications.
        /// </summary>
        void ResumeTimeOutAll();

        /// <summary>
        /// Tries to reset time out for specified notification.
        /// </summary>
        /// <param name="notificationId">Notification id.</param>
        /// <returns>True if notification found and timeout reset, otherwise false.</returns>
        bool TryResetTimeout(int notificationId);

        /// <summary>
        /// Tries to suspend time out for specified notification.
        /// </summary>
        /// <param name="notificationId">Notification id.</param>
        /// <returns>True if notification found and timeout suspended, otherwise false.</returns>
        bool TrySuspendTimeout(int notificationId);

        /// <summary>
        /// Request desired size from notification host.
        /// </summary>
        /// <param name="size">Size.</param>
        /// <returns>True if size can be provided, otherwise false.</returns>
        bool RequestNotificationHostSize(Size size);
    }
}
