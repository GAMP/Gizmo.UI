using Gizmo.UI.Services;
using Gizmo.UI.View.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gizmo.UI.View.Services
{
    /// <summary>
    /// Notifications host view service.
    /// </summary>
    [Register()]
    public sealed class NotificationsHostViewService : ViewStateServiceBase<NotificationsHostViewState>
    {
        public NotificationsHostViewService(NotificationsHostViewState viewState, 
            INotificationsService notificationsService,
            ILogger<NotificationsHostViewService> logger,
            IServiceProvider serviceProvider) : base(viewState,logger,serviceProvider)
        {
            _notificationsService = notificationsService;
        }

        private readonly INotificationsService _notificationsService;
    }
}
