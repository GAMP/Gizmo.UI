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
        void TryDismissAll();
    }
}
