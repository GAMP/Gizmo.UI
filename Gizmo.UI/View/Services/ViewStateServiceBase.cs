using Microsoft.Extensions.Logging;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.ComponentModel;
using Gizmo.UI.View.States;
using Gizmo.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gizmo.UI.View.Services
{
    /// <summary>
    /// Base view state service.
    /// </summary>
    /// <typeparam name="TViewState">View state.</typeparam>
    public abstract class ViewStateServiceBase<TViewState> : ViewServiceBase where TViewState : IViewState
    {
        #region CONSTRUCTOR
        public ViewStateServiceBase(TViewState viewState, ILogger logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _viewState = viewState;
            _navigationService = serviceProvider.GetRequiredService<NavigationService>();
        }
        #endregion

        #region FIELDS
        private readonly TViewState _viewState;
        private readonly Subject<IViewState> _stateChnageDebounceSubject = new();
        private readonly Subject<Tuple<object, PropertyChangedEventArgs>> _propertyChangedDebounceSubject = new();
        private IDisposable? _propertyChangedDebounceSubscription;
        private IDisposable? _stateChangeDebounceSubscription;
        private int _stateChangedDebounceBufferTime = 1000;  //buffer state changes for 1 second by default
        private int _propertyChangedBufferTime = 1000; //buffer state changes for 1 second by default
        private readonly NavigationService _navigationService;
        #endregion

        #region PROPERTIES

        /// <summary>
        /// Gets view state.
        /// </summary>
        public TViewState ViewState
        {
            get { return _viewState; }
        }

        /// <summary>
        /// Gets or sets defaul view state changed buffer time in milliseconds.
        /// </summary>
        protected int StateChangedDebounceBufferTime
        {
            get { return _stateChangedDebounceBufferTime; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(StateChangedDebounceBufferTime));

                //update current value
                _stateChangedDebounceBufferTime = value;

                //resubscribe
                StateChangedDebounceSubscribe();
            }
        }

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

        /// <summary>
        /// Gets navigation service.
        /// </summary>
        protected NavigationService NavigationService
        {
            get { return _navigationService; }
        }

        #endregion

        #region PRIVATE FUNCTIONS

        private void StateChangedDebounceSubscribe()
        {
            //dispose any existing subscriptions
            _stateChangeDebounceSubscription?.Dispose();

            //resubscribe
            _stateChangeDebounceSubscription = _stateChnageDebounceSubject
                .Buffer(TimeSpan.FromMilliseconds(StateChangedDebounceBufferTime))
                .Where(buffer => buffer.Count > 0)
                .Distinct()
                .Subscribe(viewStates =>
                {
                    foreach (IViewState changedViewState in viewStates)
                    {
                        try
                        {
                            changedViewState.RaiseChanged();
                        }
                        catch (Exception ex)
                        {
                            //the handlers are outside of our code so we should handle the exception and log it
                            Logger.LogError(ex, "Error in view state change debounce handler.");
                        }
                    }
                });
        }

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
             .Select(e => e.Select(p=> new
             {
                 //sender will be the groupping key
                 Sender=p.Key,
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
                         OnViewStatePropertyChangedDebounced(changedState.Sender, changedState.Args.ToList());
                     }
                     catch(Exception ex)
                     {
                         Logger.LogError(ex, "Error in property changed debounce handler (multiple properties).");
                     }

                     foreach(var change in changedState.Args)
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

        #endregion

        #region PROTECTED FUNCTIONS

        /// <summary>
        /// Debounces view state change.
        /// </summary>
        protected void DebounceViewStateChange()
        {
            DebounceViewStateChange(ViewState);
        }

        /// <summary>
        /// Debounces view state change.
        /// </summary>
        /// <param name="viewState">View state.</param>
        /// <exception cref="ArgumentNullException">thrown in case<paramref name="viewState"/>being equal to null.</exception>
        protected void DebounceViewStateChange(IViewState viewState)
        {
            if (viewState == null)
                throw new ArgumentNullException(nameof(viewState));

            //push the state to the subject
            _stateChnageDebounceSubject.OnNext(viewState);
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
        protected virtual void OnViewStatePropertyChangedDebounced(object sender, IEnumerable<PropertyChangedEventArgs> propertyChangedArgs)
        {
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

        protected virtual void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
        }

        #endregion

        #region OVERRIDES      

        protected override Task OnInitializing(CancellationToken ct)
        {
            StateChangedDebounceSubscribe();
            PropertyChangedDebounceSubscribe();

            Attach(ViewState);

            NavigationService.LocationChanged += OnLocationChanged;

            return base.OnInitializing(ct);
        }

        protected override void OnDisposing(bool isDisposing)
        {
            _stateChangeDebounceSubscription?.Dispose();
            _stateChnageDebounceSubject?.Dispose();

            _propertyChangedDebounceSubject?.Dispose();
            _propertyChangedDebounceSubscription?.Dispose();

            Detach(ViewState);

            NavigationService.LocationChanged -= OnLocationChanged;

            base.OnDisposing(isDisposing);
        }

        #endregion
    }
}
