namespace Gizmo.UI.Services
{
    public class NotificationsChangedArgs : EventArgs
    {
        public NotificationsChangedArgs() { }

        /// <summary>
        /// Gets notification id.
        /// </summary>
        public int NotificationId { get;init; }
    }
}
