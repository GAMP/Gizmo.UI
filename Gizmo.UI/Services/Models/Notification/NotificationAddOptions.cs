namespace Gizmo.UI.Services
{
    /// <summary>
    /// Notification add options.
    /// </summary>
    public sealed class NotificationAddOptions
    {
        /// <summary>
        /// Gets notification timeout.
        /// </summary>
        /// <remarks>
        /// <b>The values are in seconds.</b><br></br>
        /// Set to null to use default value.<br></br>
        /// Set to -1 to use infinite timeout.<br></br>
        /// </remarks>
        public int? Timeout { get; init; }

        /// <summary>
        /// Gets notification priority.
        /// </summary>
        public NotificationPriority Priority { get; init; }

        /// <summary>
        /// Notification acknowledge options.
        /// </summary>
        public NotificationAckOptions NotificationAckOptions
        {
            get; init;
        } = NotificationAckOptions.Dismiss;
    }
}
