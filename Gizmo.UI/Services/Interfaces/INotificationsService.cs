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
    }
}
