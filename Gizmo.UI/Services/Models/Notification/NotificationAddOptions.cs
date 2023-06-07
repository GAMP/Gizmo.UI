namespace Gizmo.UI.Services
{
    /// <summary>
    /// Notification add options.
    /// </summary>
    public sealed class NotificationAddOptions
    {
        /// <summary>
        /// Default acknowledge options (time-out or dismiss).
        /// </summary>
        public static readonly NotificationAddOptions DefaultAcknowlege = new NotificationAddOptions() { NotificationAckOptions = NotificationAckOptions.Dismiss | NotificationAckOptions.TimeOut };

        /// <summary>
        /// Gets notification timeout.
        /// </summary>
        /// <remarks>
        /// <b>The values are in seconds.</b><br></br>
        /// Set to null to use default value.<br></br>
        /// Set to -1 to use infinite timeout.<br></br>
        /// </remarks>
        public int? Timeout { get; internal set; } = null;

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
