using System.Diagnostics.CodeAnalysis;
using Gizmo.UI.View.States;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;

namespace Gizmo.UI.View.Services
{


    /// <summary>
    /// View state lookup service base.
    /// </summary>
    /// <typeparam name="TLookUpkey">Lookup key.</typeparam>
    /// <typeparam name="TViewState">View state type.</typeparam>
    public abstract class ViewStateLookupServiceBase<TLookUpkey, TViewState> : ViewServiceBase
    where TLookUpkey : notnull
    where TViewState : IViewState
    {
        #region CONSTRUCTOR
        protected ViewStateLookupServiceBase(ILogger logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _debounceService = serviceProvider.GetRequiredService<ViewStateDebounceService>();
        }
        #endregion

        #region FIELDS
        private readonly ViewStateDebounceService _debounceService;
        private readonly SemaphoreSlim _cacheAccessLock = new(1);
        private readonly SemaphoreSlim _initializeLock = new(1);
        private readonly Dictionary<TLookUpkey, TViewState> _cache = new();
        private bool _dataInitialized = false;
        /// <summary>
        /// Raised when managed view state collection changes, for example view state is added or removed.<br></br>
        /// <b>This event is not raised when individual view state changes.</b>
        /// </summary>
        public event EventHandler<LookupViewStateChangeArgs>? Changed;
        #endregion

        #region METHODS

        /// <summary>
        /// Gets all view states.
        /// </summary>
        /// <param name="cToken">Cancellation token.</param>
        /// <returns>View states.</returns>
        public async ValueTask<IEnumerable<TViewState>> GetStatesAsync(CancellationToken cToken = default)
        {
            //this will trigger data initalization if required
            await EnsureDataInitialized(cToken);

            //return any generated view states
            return _cache.Values;
        }

        /// <summary>
        /// Gets view state specified by <paramref name="key"/>.
        /// </summary>
        /// <param name="key">View state key.</param>
        /// <param name="cToken">Cancellation token.</param>
        /// <returns>View state.</returns>
        public async ValueTask<TViewState> GetStateAsync(TLookUpkey key, CancellationToken cToken = default)
        {
            //this will trigger data initalization if required
            await EnsureDataInitialized(cToken);

            await _cacheAccessLock.WaitAsync(cToken);

            try
            {
                if (_cache.TryGetValue(key, out var value))
                    return value;

                var viewState = await CreateViewStateAsync(key, cToken);

                _cache.Add(key, viewState);

                return viewState;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed creating view state.");
                return CreateDefaultViewState(key);
            }
            finally
            {
                _cacheAccessLock.Release();
            }
        }

        /// <summary>
        /// Tries to obtain view state from the cache.
        /// </summary>
        /// <param name="lookUpkey">View state key.</param>
        /// <param name="state">View state.</param>
        /// <returns>True if found in cache, otherwise false.</returns>
        protected bool TryGetState(TLookUpkey lookUpkey, [NotNullWhen(true)] out TViewState? state)
        {
            return _cache.TryGetValue(lookUpkey, out state);
        }

        /// <summary>
        /// Add the state to the cache.
        /// </summary>
        /// <param name="lookUpkey">Lookup key.</param>
        /// <param name="state">View state.</param>
        protected void AddViewState(TLookUpkey lookUpkey, TViewState state) => _cache.Add(lookUpkey, state);

        /// <summary>
        /// Raises change event.
        /// </summary>
        protected void RaiseChanged(LookupViewStateChangeType type)
        {
            Changed?.Invoke(this, new LookupViewStateChangeArgs() { Type = type });
        }

        protected void DebounceViewStateChange(IViewState viewState)
        {
            _debounceService.Debounce(viewState);
        }

        private async ValueTask EnsureDataInitialized(CancellationToken cToken)
        {
            //make inital check without lock or await
            if (_dataInitialized)
                return;

            await _initializeLock.WaitAsync(cToken);

            try
            {
                //re cehck initialization with lock
                if (_dataInitialized)
                    return;

                //clar current cache
                _cache.Clear();

                //initialize data
                _dataInitialized = await DataInitializeAsync(cToken);

                //view states/data was initialized
                RaiseChanged(LookupViewStateChangeType.Initialized);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Data initialization failed.");
                _dataInitialized = false;
            }
            finally
            {
                _initializeLock.Release();
            }
        }

        #endregion

        #region ABSTRACT METHODS

        /// <summary>
        /// Initializes data.
        /// </summary>
        /// <param name="cToken">Cancellation token.</param>
        /// <returns>True if initialization was successful, false if retry needed.</returns>
        /// <remarks>
        /// The method is responsible of initializing initial data.<br></br>
        /// Example would be calling an service over api and getting required data and creating appropriate initial view states.<br></br>
        /// <b>The function is thread safe.</b>
        /// </remarks>
        protected abstract Task<bool> DataInitializeAsync(CancellationToken cToken);

        /// <summary>
        /// Responsible of creating the view state.
        /// </summary>
        /// <param name="lookUpkey">View state lookup key.</param>
        /// <param name="cToken">Cancellation token.</param>
        /// <returns>View state.</returns>
        /// <remarks>
        /// This function will only be called if we cant obtain the view state with <paramref name="lookUpkey"/> specified from cache.<br></br>
        /// It is responsible of obtaining view state for signle item.<br></br>
        /// <b>This function should not attempt to modify cache, its only purpose to create view state.</b>
        /// </remarks>
        protected abstract ValueTask<TViewState> CreateViewStateAsync(TLookUpkey lookUpkey, CancellationToken cToken = default);

        /// <summary>
        /// Creates default view state.
        /// </summary>
        /// <param name="lookUpkey">Lookup key.</param>
        /// <returns>View state.</returns>
        /// <remarks>
        /// This function will be called in case we cant obtain associated data object for specified <paramref name="lookUpkey"/>.<br></br>
        /// This will be used in cases of error in order to present default/errored view state for the view.<br></br>
        /// <b>By default we will try to obtain uninitialized view state from DI container.</b>
        /// </remarks>
        /// <exception cref="InvalidOperationException">thrown if <typeparamref name="TViewState"/> is not registered in IOC container.</exception>
        protected abstract TViewState CreateDefaultViewState(TLookUpkey lookUpkey);

        #endregion
    }
}
