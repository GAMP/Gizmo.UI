using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Gizmo.UI.Services;
using Gizmo.UI.View.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gizmo.UI.View.Services
{
    /// <summary>
    /// View state lookup service base.
    /// </summary>
    /// <typeparam name="TKey">Lookup key.</typeparam>
    /// <typeparam name="TViewState">View state type.</typeparam>
    public abstract class ViewStateLookupServiceBase<TKey, TViewState> : ViewServiceBase
        where TKey : notnull
        where TViewState : IViewState
    {
        #region CONSTRUCTOR
        protected ViewStateLookupServiceBase(ILogger logger, IServiceProvider serviceProvider) : base(logger, serviceProvider) =>
            _debounceService = serviceProvider.GetRequiredService<DebounceActionService>();
        #endregion

        #region PRIVATE FIELDS
        private bool _dataInitialized;
        private readonly SemaphoreSlim _cacheAccessLock = new(1);
        private readonly SemaphoreSlim _initializeLock = new(1);
        private readonly ConcurrentDictionary<TKey, TViewState> _cache = new();
        private readonly DebounceActionService _debounceService;
        #endregion

        /// <summary>
        /// Initialization lock.
        /// </summary>
        /// <remarks>
        /// This will be required in implementations in order to be able to wait for the initialization to complete.
        /// </remarks>
        protected SemaphoreSlim InitializationLock => _initializeLock;

        #region PUBLIC EVENTS
        /// <summary>
        /// Occurs when view state is changed.
        /// </summary>
        public event EventHandler<LookupServiceChangeArgs>? Changed;
        #endregion

        #region PUBLIC FUNCTIONS

        /// <summary>
        /// Gets all view states.
        /// Initialize view states if it is not initialized.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>View states.</returns>
        public async ValueTask<IEnumerable<TViewState>> GetStatesAsync(CancellationToken cancellationToken = default)
        {
            //this will trigger data initialization if required
            await EnsureDataInitialized(cancellationToken);

            //return any generated view states
            return _cache.Values;
        }

        /// <summary>
        /// Gets view state specified by <paramref name="key"/>.
        /// Initialize view states if it is not initialized.
        /// Create view state if it is not found.
        /// </summary>
        /// <param name="key">View state key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="withUpdate">True if view state should be updated, otherwise false.</param>
        /// <returns>View state.</returns>
        public async ValueTask<TViewState> GetStateAsync(TKey key, bool withUpdate = false, CancellationToken cancellationToken = default)
        {
            //this will trigger data initialization if required
            await EnsureDataInitialized(cancellationToken);

            var hasValue = _cache.TryGetValue(key, out var viewState);

            if (!withUpdate && hasValue)
                return viewState!;

            try
            {
                await _cacheAccessLock.WaitAsync(cancellationToken);

                if (withUpdate && hasValue)
                {
                    var updatedViewState = await UpdateViewStateAsync(key, viewState!, cancellationToken);

                    if(_cache.TryUpdate(key, updatedViewState, viewState!))
                    {
                        //the view state was replaced, that mean that we have updated existing one and some UI might be holding into it
                        //in such cases we need notify UI of changes made
                        _debounceService.Debounce(updatedViewState.RaiseChanged);
                    }

                    return updatedViewState;
                }

                viewState = await CreateViewStateAsync(key, cancellationToken);

                _cache.TryAdd(key, viewState);

                _debounceService.Debounce(viewState.RaiseChanged);

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

        #endregion

        #region PRIVATE FUNCTIONS
        private async ValueTask EnsureDataInitialized(CancellationToken cancellationToken)
        {
            //make initial check without lock or await
            if (_dataInitialized)
                return;

            await _initializeLock.WaitAsync(cancellationToken);

            try
            {
                //re check initialization with lock
                if (_dataInitialized)
                    return;

                //clear current cache
                _cache.Clear();

                var data = await DataInitializeAsync(cancellationToken);

                foreach (var item in data)
                    _cache.TryAdd(item.Key, item.Value);

                //initialize data
                _dataInitialized = true;

                //view states/data was initialized
                RaiseChanged(LookupServiceChangeType.Initialized);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Data initialization failed.");
                _dataInitialized = false;
            }
            finally
            {
                _initializeLock.Release();
            }
        }
        #endregion

        #region PROTECTED FUNCTIONS
    
        /// <summary>
        /// Tries to obtain view state from the cache.
        /// </summary>
        /// <param name="key">View state key.</param>
        /// <param name="state">View state.</param>
        /// <returns>True if found in cache, otherwise false.</returns>
        protected bool TryGetState(TKey key, [NotNullWhen(true)] out TViewState? state) =>
            _cache.TryGetValue(key, out state);

        /// <summary>
        /// Gets or adds view state.
        /// </summary>
        /// <param name="key">View state key.</param>
        /// <param name="stateFactory">View state creation factory.</param>
        /// <returns>View state.</returns>
        protected TViewState GetOrAdd(TKey key, Func<TKey,TViewState> stateFactory)
        {
           return _cache.GetOrAdd(key, stateFactory);
        }

        /// <summary>
        /// Gets or adds default view state.
        /// </summary>
        /// <param name="key">View state key.</param>
        /// <returns>View state.</returns>
        protected TViewState GetOrAddDefault(TKey key)
        {
            return GetOrAdd(key, itemKey => CreateDefaultViewState(itemKey));
        }

        /// <summary>
        /// Gets currently cached view states.
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<TViewState> GetCachedStates()
        {
            return _cache.Values;
        }
     
        /// <summary>
        /// Debounces view state change.
        /// </summary>
        /// <param name="viewState">View state.</param>
        /// <exception cref="ArgumentNullException">thrown in case <paramref name="viewState"/>is equal to null.</exception>
        protected void DebounceViewStateChange(IViewState viewState) => _debounceService.Debounce(viewState.RaiseChanged);
      
        /// <summary>
        /// Handles the changes of the incoming data.
        /// </summary>
        /// <param name="key">Lookup key.</param>
        /// <param name="modificationType">Type of the changes.</param>
        /// <param name="cancellationToken">Cancelation token.</param>
        /// <returns> Task.</returns>
        protected async Task HandleChangesAsync(TKey key, LookupServiceChangeType modificationType, CancellationToken cancellationToken = default)
        {
            try
            {
                switch (modificationType)
                {
                    case LookupServiceChangeType.Modified:
                    case LookupServiceChangeType.Added:
                        _ = await GetStateAsync(key, withUpdate: true, cancellationToken);
                        break;
                    case LookupServiceChangeType.Removed:
                        _cache.TryRemove(key, out _);
                        break;
                }

                RaiseChanged(modificationType);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to handle change.");
            }
        }
       
        /// <summary>
        /// Raises change event.
        /// If the modification type is not defined, this will set the modified data type as Initialized.
        /// </summary>
        /// <param name="modificationType">Type of changes.</param>
        protected void RaiseChanged(LookupServiceChangeType modificationType) =>
            Changed?.Invoke(this, new() { Type = modificationType });

        /// <summary>
        /// Resets initialization.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// This function should be called once current initialization state no longer considered valid.
        /// </remarks>
        protected async ValueTask ResetInitialization(CancellationToken cancellationToken = default)
        {
            await _initializeLock.WaitAsync(cancellationToken);

            try
            {
                //clear current cache
                _cache.Clear();

                _dataInitialized = false;

                RaiseChanged(LookupServiceChangeType.None);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Data reset initialization failed.");
            }
            finally
            {
                _initializeLock.Release();
            }
        }

        /// <summary>
        /// Logs getting state error.
        /// </summary>
        /// <param name="exception">Error exception.</param>
        protected void LogCreateStateError(Exception exception)
        {
            Logger.LogError(exception, "Error getting state.");
        }

        /// <summary>
        /// Logs updating state error.
        /// </summary>
        /// <param name="exception">Error exception.</param>
        protected void LogUpdateStateError(Exception exception)
        {
            Logger.LogError(exception, "Error updating state.");
        }

        #endregion

        #region ABSTRACT FUNCTIONS

        /// <summary>
        /// Initializes data.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Results dictionary.</returns>
        /// <remarks>
        /// The method is responsible of initializing initial data.<br></br>
        /// Example would be calling an service over api and getting required data and creating appropriate initial view states.<br></br>
        /// <b>The function is thread safe.</b>
        /// </remarks>
        protected abstract Task<IDictionary<TKey, TViewState>> DataInitializeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Responsible of creating the view state.
        /// </summary>
        /// <param name="key">View state lookup key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Created view state.</returns>
        /// <remarks>
        /// This function will only be called if we cant obtain the view state with <paramref name="key"/> specified from cache.<br></br>
        /// It is responsible of obtaining view state for single item.<br></br>
        /// <b>This function should not attempt to modify cache, its only purpose to create view state.</b>
        /// </remarks>
        protected abstract ValueTask<TViewState> CreateViewStateAsync(TKey key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Responsible of updating the view state.
        /// </summary>
        /// <param name="key">View state lookup key.</param>
        /// <param name="viewState">Existing view state instance.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Updated view state.</returns>
        /// <remarks>
        /// It is responsible of updating view state for single item.<br></br>
        /// <b>This function should not attempt to modify the cache, its only purpose is to update the view state.</b>
        /// </remarks>
        protected abstract ValueTask<TViewState> UpdateViewStateAsync(TKey key, TViewState viewState, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates default view state.
        /// </summary>
        /// <param name="key">Lookup key.</param>
        /// <returns>Created default view state.</returns>
        /// <remarks>
        /// This function will be called in case we cant obtain associated data object for specified <paramref name="key"/>.<br></br>
        /// This will be used in cases of error in order to present default/errored view state for the view.<br></br>
        /// <b>By default we will try to obtain uninitialized view state from DI container.</b>
        /// </remarks>
        /// <exception cref="InvalidOperationException">thrown if <typeparamref name="TViewState"/> is not registered in IOC container.</exception>
        protected abstract TViewState CreateDefaultViewState(TKey key);

        #endregion
    }
}
