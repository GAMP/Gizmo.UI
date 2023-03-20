using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        private readonly Subject<IViewState> _stateChnageDebounceSubject = new();
        private readonly Subject<Tuple<object, PropertyChangedEventArgs>> _propertyChangedDebounceSubject = new();
        private IDisposable? _propertyChangedDebounceSubscription;
        private int _propertyChangedBufferTime = 1000; //buffer state changes for 1 second by default

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

        /// <summary>
        /// Gets or sets default view property changed buffer time in milliseconds.
        /// </summary>
        protected int PropertyChangedDebounceBufferTime
        {
            get { return _propertyChangedBufferTime; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(PropertyChangedDebounceBufferTime));

                //update current value
                _propertyChangedBufferTime = value;

                //resubscribe
                PropertyChangedDebounceSubscribe();
            }
        }

        #endregion

        #region PRIVATE FUNCTIONS

        private void PropertyChangedDebounceSubscribe()
        {
            //dispose any existing subscriptions
            _propertyChangedDebounceSubscription?.Dispose();

            //resubscribe
            _propertyChangedDebounceSubscription = _propertyChangedDebounceSubject
             //buffer for desired time
             .Buffer(TimeSpan.FromMilliseconds(PropertyChangedDebounceBufferTime))
             //only call when there are items in the buffer
             .Where(buffer => buffer.Count > 0)
             //group changes by their source
             .Select(e => e.GroupBy(p => p.Item1))
             //select changes grupped by sender (view state)
             .Select(e => e.Select(p => new
             {
                 //sender will be the groupping key
                 Sender = p.Key,
                 //group property changes by property name and only select last from each
                 Args = p.Select(pc => pc.Item2)
                 .GroupBy(pr => pr.PropertyName)
                 .Select(prc => prc.Last())
             }))
             .Subscribe((changes) =>
             {
                 foreach (var changedState in changes)
                 {
                     try
                     {
                         OnViewStatePropertyChangedDebouncedAsync(changedState.Sender, changedState.Args.ToList());
                     }
                     catch (Exception ex)
                     {
                         Logger.LogError(ex, "Error in property changed debounce handler (multiple properties).");
                     }

                     foreach (var change in changedState.Args)
                     {
                         try
                         {
                             OnViewStatePropertyChangedDebounced(changedState.Sender, change);
                         }
                         catch (Exception ex)
                         {
                             Logger.LogError(ex, "Error in property changed debounce handler (single property).");
                         }
                     }
                 }
             });
        }

        private CancellationTokenSource? _navigatedInCancellationSource;
        private CancellationTokenSource? _navigatedOutCancellationSource;

        private async void OnLocationChangedInternal(object? _, LocationChangedEventArgs args)
        {
            var (isFirstNavigation, isNavigatedIn) = GeLocationChangedInternalState(args.Location);

            if (isNavigatedIn)
            {
                _stackRoutes.Push(args.Location);

                //cancel any current navigated out handlers
                _navigatedOutCancellationSource?.Cancel();

                _navigatedInCancellationSource = new();

                await OnNavigatedIn(new(isFirstNavigation, args.IsNavigationIntercepted), _navigatedInCancellationSource.Token);
            }
            else
            {
                // if we have no previous location - return
                if (!_stackRoutes.TryPop(out var _))
                    return;

                //cancel any currently running navigated in handlers
                _navigatedInCancellationSource?.Cancel();

                _navigatedOutCancellationSource = new();

                await OnNavigatedOut(new(false, args.IsNavigationIntercepted), _navigatedOutCancellationSource.Token);
            }

            OnLocationChanged(_, args);
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

            if (!Uri.TryCreate(location, UriKind.Absolute, out var uri))
                throw new ArgumentException("Route is not valid", location);

            isNavigatedIn = _associatedRoutes.Any(route => route.Template == uri.LocalPath);

            while (!_navigatedRoutes.TryAdd(location, isNavigatedIn))
                ;

            return (true, isNavigatedIn);
        }

        #endregion

        #region PROTECTED FUNCTIONS

        /// <summary>
        /// Debounces view state change.
        /// </summary>
        protected void DebounceViewStateChange()
        {
            _debounceService.Debounce(ViewState);
        }

        protected void DebounceViewStateChange(IViewState viewState)
        {
            _debounceService.Debounce(viewState);
        }

        /// <summary>
        /// Raises view state change event on attached view state.
        /// </summary>
        protected void ViewStateChanged()
        {
            ViewState.RaiseChanged();
        }

        /// <summary>
        /// Attaches property changed event.
        /// </summary>
        /// <param name="notifyPropertyChanged">Instance.</param>
        protected void Attach(INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged -= OnViewStatePropertyChangedInternal;
            notifyPropertyChanged.PropertyChanged += OnViewStatePropertyChangedInternal;
        }

        /// <summary>
        /// Dettaches property changed event.
        /// </summary>
        /// <param name="notifyPropertyChanged">Instance.</param>
        protected void Detach(INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged -= OnViewStatePropertyChangedInternal;
        }

        /// <summary>
        /// Gets view state.
        /// </summary>
        /// <typeparam name="T">View state type.</typeparam>
        /// <param name="init">Initialization function.</param>
        /// <returns>
        /// View state instance of <typeparamref name="T"/> type.
        /// </returns>
        /// <remarks>
        /// The new instance <see cref="INotifyPropertyChanged"/> event will also be attached autmatically.<br/>
        /// Property changed notifications will also be locked as long as the <paramref name="init"/> routine runs.
        /// </remarks>
        protected T GetViewState<T>(Action<T>? init = default) where T : IViewState
        {
            //get required view state
            var state = ServiceProvider.GetRequiredService<T>();

            //if initalization function set invokeit
            if (init != null)
            {
                //always lock property changes during in code modification of properties
                using (state.PropertyChangedLock())
                {
                    init(state);
                }
            }

            //attach property changes
            Attach(state);

            return state;
        }

        #endregion

        #region PRIVATE EVENT HANDLERS

        private void OnViewStatePropertyChangedInternal(object? sender, PropertyChangedEventArgs e)
        {
            //this should not happen and it is a requirement to have sender object
            //adding a check to avoid nullability warnings
            if (sender == null)
                return;

            Logger.LogTrace("View state ({viewState}) property ({propertyName}) changed.", sender.GetType().FullName, e.PropertyName);

            //call property changed
            OnViewStatePropertyChanged(sender, e);

            //buffer chnage
            _propertyChangedDebounceSubject.OnNext(Tuple.Create(sender, e));
        }

        #endregion

        #region PROTECTED VIRTUAL

        /// <summary>
        /// Called after view state property changed based on buffer interval.
        /// </summary>
        /// <param name="sender">Source view state object.</param>
        /// <param name="propertyChangedArgs">Property changed arguments.</param>
        /// <remarks>
        /// The arguments will contain a list of unique property change arguments.
        /// </remarks>
        protected virtual Task OnViewStatePropertyChangedDebouncedAsync(object sender, IEnumerable<PropertyChangedEventArgs> propertyChangedArgs)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called after view state property changed based on buffer interval.
        /// </summary>
        /// <param name="sender">Source view state object.</param>
        /// <param name="e">Property changed arguments.</param>
        protected virtual void OnViewStatePropertyChangedDebounced(object sender, PropertyChangedEventArgs e)
        {
        }

        /// <summary>
        /// Called instantly on view state property changed.
        /// </summary>
        /// <param name="sender">Source view state object.</param>
        /// <param name="e">Property changed arguments.</param>
        /// <remarks>
        /// This method is called as soon as the view state property changes.
        /// </remarks>
        protected virtual void OnViewStatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        /// <summary>
        /// Called after current application location changed.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Location change parameters.</param>
        protected virtual void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
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
            PropertyChangedDebounceSubscribe();

            Attach(ViewState);

            NavigationService.LocationChanged += OnLocationChangedInternal;

            return base.OnInitializing(ct);
        }

        protected override void OnDisposing(bool isDisposing)
        {
            _stateChnageDebounceSubject?.Dispose();

            _propertyChangedDebounceSubject?.Dispose();
            _propertyChangedDebounceSubscription?.Dispose();

            Detach(ViewState);

            NavigationService.LocationChanged -= OnLocationChangedInternal;

            base.OnDisposing(isDisposing);
        }

        #endregion
    }

    public record NavigationParameters(bool IsInitial, bool IsByLink);
}
