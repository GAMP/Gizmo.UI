using System.Drawing;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// Notifications service.
    /// </summary>
    public interface INotificationsService
    {
        public event EventHandler<NotificationsChangedArgs>? NotificationsChanged;
        IEnumerable<INotificationController> GetVisible();
        IEnumerable<INotificationController> GetDismissed();

        /// <summary>
        /// Acknowledge all notifications.
        /// </summary>
        void AcknowledgeAll();

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
        /// Request desired size from notification host.
        /// </summary>
        /// <param name="size">Size.</param>
        /// <returns>True if size can be provided, otherwise false.</returns>
        bool RequestNotificationHostSize(Size size);
    }
}
