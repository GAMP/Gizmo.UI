namespace Gizmo.UI.Services
{
    [Flags()]
    public enum NotificationAckOptions
    {
        /// <summary>
        /// Default.
        /// </summary>
        Default=0,
        /// <summary>
        /// Notification will be acknowledged on dismiss.
        /// </summary>
        Dismiss=1,
        /// <summary>
        /// Notification will be acknowledged on time out.
        /// </summary>
        TimeOut = 2,
    }
}
