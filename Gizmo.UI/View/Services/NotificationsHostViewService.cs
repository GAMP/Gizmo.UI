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
            IServiceProvider serviceProvider) : base(viewState, logger, serviceProvider)
        {
            _notificationsService = notificationsService;
        }

        private readonly INotificationsService _notificationsService;

        protected override Task OnInitializing(CancellationToken ct)
        {
            _notificationsService.NotificationsChanged += OnNotificationsChanged;
            return base.OnInitializing(ct);
        }

        protected override void OnDisposing(bool isDisposing)
        {
            _notificationsService.NotificationsChanged -= OnNotificationsChanged;
            base.OnDisposing(isDisposing);
        }

        private void OnNotificationsChanged(object? sender, NotificationsChangedArgs e)
        {
            ViewState.Visible = _notificationsService.GetVisible();
            ViewState.Dismissed = _notificationsService.GetDismissed();

            RaiseViewStateChanged();
        }
    }
}
