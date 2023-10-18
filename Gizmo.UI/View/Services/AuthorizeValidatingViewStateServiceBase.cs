using Gizmo.UI.View.States;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Gizmo.UI.View.Services
{
    /// <summary>
    /// View state service supporting validating view state and authorization.
    /// </summary>
    /// <typeparam name="TViewState">Validating view state.</typeparam>
    /// <remarks>
    /// The service will only require user to be authenticated, any other policies should be applied with <see cref="AuthorizeAttribute"/> on derived classes.
    /// </remarks>
    [Authorize()]
    public abstract class AuthorizeValidatingViewStateServiceBase<TViewState> : ValidatingViewStateServiceBase<TViewState> where TViewState : IValidatingViewState
    {
        public AuthorizeValidatingViewStateServiceBase(TViewState viewState,
            ILogger logger,
            IServiceProvider serviceProvider) : base(viewState, logger, serviceProvider)
        {
        }
    }
}
