using Gizmo.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gizmo.UI.View.States
{
    /// <summary>
    /// Notifications host view state.
    /// </summary>
    [Register()]
    public sealed class NotificationsHostViewState : ViewStateBase
    {
        /// <summary>
        /// Gets visible notifications.
        /// </summary>
        public IEnumerable<INotificationController> Visible { get; internal set; } = Enumerable.Empty<INotificationController>();

        /// <summary>
        /// Gets dismissed notifications.
        /// </summary>
        public IEnumerable<INotificationController> Dismissed { get; internal set; } = Enumerable.Empty<INotificationController>();
    }
}
