using Gizmo.UI.View.States;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Gizmo.UI.View.Services
{
    /// <summary>
    /// Base view state service with authorization support.
    /// </summary>
    /// <typeparam name="TViewState">View state.</typeparam>
    /// <remarks>
    /// The service will only require user to be authenticated, any other policies should be applied with <see cref="AuthorizeAttribute"/> on derived classes.
    /// </remarks>
    [Authorize()]
    public abstract class AuthorizeViewStateServiceBase<TViewState> : ViewStateServiceBase<TViewState> where TViewState : IViewState
    {
        public AuthorizeViewStateServiceBase(TViewState viewState,
            ILogger logger,
            IServiceProvider serviceProvider) : base(viewState, logger, serviceProvider)
        {
        }
    }
}
