using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reflection;
using Gizmo.UI.Services;
using Gizmo.UI.View.States;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gizmo.UI.View.Services
{
    /// <summary>
    /// Base view state service.
    /// </summary>
    /// <typeparam name="TViewState">View state.</typeparam>
    public abstract class ViewStateServiceBase<TViewState> : ViewServiceBase where TViewState : IViewState
    {
        #region CONSTRUCTOR
        protected ViewStateServiceBase(
            TViewState viewState,
            ILogger logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            ViewState = viewState;
            NavigationService = serviceProvider.GetRequiredService<NavigationService>();
            _debounceService = serviceProvider.GetRequiredService<ViewStateDebounceService>();

            _associatedRoutes = GetType().GetCustomAttributes<RouteAttribute>().ToList() ?? Enumerable.Empty<RouteAttribute>().ToList();
            _navigatedRoutes = new(5, _associatedRoutes.Count);
            _stackRoutes = new();
        }
        #endregion

        #region FIELDS

        private readonly List<RouteAttribute> _associatedRoutes; //set of associated routes
        private readonly ConcurrentDictionary<string, bool> _navigatedRoutes; //keep visited routes and local paths of URL
        private readonly ConcurrentStack<string> _stackRoutes; //keep visited routes

        private readonly ViewStateDebounceService _debounceService;
        #endregion

        #region PROPERTIES

        /// <summary>
        /// Gets view state.
        /// </summary>
        public TViewState ViewState { get; }

        /// <summary>
        /// Gets navigation service.
        /// </summary>
        protected NavigationService NavigationService { get; }

        #endregion

        #region PRIVATE FUNCTIONS

        private CancellationTokenSource? _navigatedInCancellationSource;
        private CancellationTokenSource? _navigatedOutCancellationSource;

        private async void OnLocationChangedInternal(object? sender, LocationChangedEventArgs args)
        {
            var (isFirstNavigation, isNavigatedIn) = GeLocationChangedInternalState(args.Location);

            if (isNavigatedIn)
            {
                _stackRoutes.Push(args.Location);

                //cancel any current navigated out handlers
                _navigatedOutCancellationSource?.Cancel();

                _navigatedInCancellationSource = new();

                try
                {
                    await OnNavigatedIn(new(isFirstNavigation, args.IsNavigationIntercepted), _navigatedInCancellationSource.Token);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error while handling navigated in event.");
                }
            }
            else
            {
                // if we have no previous location - return
                if (!_stackRoutes.TryPop(out var _))
                    return;

                //cancel any currently running navigated in handlers
                _navigatedInCancellationSource?.Cancel();

                _navigatedOutCancellationSource = new();

                try
                {
                    await OnNavigatedOut(new(false, args.IsNavigationIntercepted), _navigatedOutCancellationSource.Token);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error while handling navigated out event.");
                }
            }

            try
            {
                await OnLocationChanged(sender, args);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while handling location changed event.");
            }
        }

        /// <summary>
        /// Get information about a state of the incoming location.
        /// </summary>
        /// <param name="location">Location from the LocationChangedEventArgs.</param>
        /// <returns>
        /// 1 boolean - If it is the first navigation by this location.
        /// 2 boolean - If this location is this RouteAttribute.Template.
        /// </returns>
        private (bool, bool) GeLocationChangedInternalState(string location)
        {
            if (_navigatedRoutes.TryGetValue(location, out var isNavigatedIn))
                return (false, isNavigatedIn);

            var uri = new Uri(location, UriKind.Absolute);
            
            isNavigatedIn = _associatedRoutes.Any(route => route.Template == uri.LocalPath);

            var isFirstNavigation = _navigatedRoutes.TryAdd(location, isNavigatedIn);

            return (isFirstNavigation, isNavigatedIn);
        }

        #endregion

        #region PROTECTED FUNCTIONS

        /// <summary>
        /// Raises view state change event on attached view state.
        /// </summary>
        protected void RaiseViewStateChanged()
        {
            ViewState.RaiseChanged();
        }

        /// <summary>
        /// Debounces view state change for current view state.
        /// </summary>
        /// <param name="viewState">View state.</param>
        /// <exception cref="ArgumentNullException">thrown in case <paramref name="viewState"/> is equal to null.</exception>
        protected void DebounceViewStateChanged()
        {
            _debounceService.Debounce(ViewState);
        }

        /// <summary>
        /// Gets view state.
        /// </summary>
        /// <typeparam name="T">View state type.</typeparam>
        /// <param name="init">Initialization function.</param>
        /// <returns>
        /// View state instance of <typeparamref name="T"/> type.
        /// </returns>
        /// <exception cref="InvalidOperationException">thrown in case required view state cant be obtained from DI contaner.</exception>
        protected T GetRequiredViewState<T>(Action<T>? init = default) where T : IViewState
        {
            //get required view state
            var state = ServiceProvider.GetRequiredService<T>();

            //if initalization function set invoke it
            init?.Invoke(state);

            return state;
        }

        #endregion

        #region PROTECTED VIRTUAL

        /// <summary>
        /// Called after current application location changed.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Location change parameters.</param>
        protected virtual Task OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called once application navigates into one of view service associated routes.
        /// </summary>
        protected virtual Task OnNavigatedIn(NavigationParameters navigationParameters, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called once application navigates to route that does not match any view service associated routes.
        /// </summary>
        protected virtual Task OnNavigatedOut(NavigationParameters navigationParameters, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        #endregion

        #region OVERRIDES      

        protected override Task OnInitializing(CancellationToken ct)
        {
            NavigationService.LocationChanged += OnLocationChangedInternal;

            return base.OnInitializing(ct);
        }

        protected override void OnDisposing(bool isDisposing)
        {
            NavigationService.LocationChanged -= OnLocationChangedInternal;

            base.OnDisposing(isDisposing);
        }

        #endregion
    }

    public record NavigationParameters(bool IsInitial, bool IsByLink);
}
