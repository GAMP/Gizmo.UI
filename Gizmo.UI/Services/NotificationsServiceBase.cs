using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// Notification service base.
    /// </summary>
    public abstract class NotificationsServiceBase : INotificationsService
    {
        #region CONSTRCUTOR
        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="logger">Logger.</param>
        public NotificationsServiceBase( INotificationsHost notificationsHost, IServiceProvider serviceProvider, ILogger logger)
        {
            _notificationsHost = notificationsHost;
            _serviceProvider = serviceProvider;
            _logger = logger;

            //WARNING injecting GlobalCancellationService will fail due to the way we register it
            _globalCancellationService = _serviceProvider.GetRequiredService<GlobalCancellationService>();
        }
        #endregion

        #region FIELDS
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly GlobalCancellationService _globalCancellationService; 
        private readonly ConcurrentDictionary<int,NotificationState> _notificationStates = new();
        private readonly INotificationsHost _notificationsHost;
        #endregion

        public event EventHandler<NotificationsChangedArgs>? NotificationsChanged;

        private class NotificationState
        {
            public NotificationState(INotificationController notificationController)
            {
                Controller = notificationController;
                CreationTime = DateTime.UtcNow;
                Ack = NotificationAck.None;

            }

            public DateTime CreationTime
            {
                get;init;
            }

            public NotificationAck Ack
            {
                get; init;
            }

            public INotificationController Controller { get;}
        }

        public enum NotificationAck
        {
            None,
            Aknowledged,
        }
    }

    public class NotificationsChangedArgs
    {
        public NotificationsChangedArgs() { }

        public int NotificationId { get; set; }
    }
}
