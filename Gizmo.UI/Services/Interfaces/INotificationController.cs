namespace Gizmo.UI.Services
{
    /// <summary>
    /// Notification controller interface.
    /// </summary>
    public interface INotificationController : IComponentController
    {
        /// <summary>
        /// Gets notification display options.
        /// </summary>
        public NotificationDisplayOptions DisplayOptions { get; }
    }
}
