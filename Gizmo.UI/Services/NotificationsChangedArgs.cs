namespace Gizmo.UI.Services
{
    public class NotificationsChangedArgs : EventArgs
    {
        /// <summary>
        /// Creates new instance.
        /// </summary>
        public NotificationsChangedArgs() { }

        /// <summary>
        /// Gets notification id.
        /// </summary>
        public int NotificationId { get;init; }
    }
}
