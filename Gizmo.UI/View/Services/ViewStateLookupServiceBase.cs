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
    /// <typeparam name="TLookUpkey">Lookup key.</typeparam>
    /// <typeparam name="TViewState">View state type.</typeparam>
    public abstract class ViewStateLookupServiceBase<TLookUpkey, TViewState> : ViewServiceBase
        where TLookUpkey : notnull
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
        private readonly ConcurrentDictionary<TLookUpkey, TViewState> _cache = new();
        private readonly DebounceActionService _debounceService;
        #endregion

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
        /// Initialize view states if it is not initialized.
        /// Create view state if it is not found.
        /// </summary>
        /// <param name="key">View state key.</param>
        /// <param name="cToken">Cancellation token.</param>
        /// <param name="withUpdate">True if view state should be updated, otherwise false.</param>
        /// <returns>View state.</returns>
        public async ValueTask<TViewState> GetStateAsync(TLookUpkey key, bool withUpdate = false, CancellationToken cToken = default)
        {
            //this will trigger data initalization if required
            await EnsureDataInitialized(cToken);

            var hasValue = _cache.TryGetValue(key, out var viewState);

            if (!withUpdate && hasValue)
                return viewState!;

            try
            {
                await _cacheAccessLock.WaitAsync(cToken);

                if (withUpdate && hasValue)
                {
                    var updatedViewState = await UpdateViewStateAsync(viewState!, cToken);

                    _cache.TryUpdate(key, updatedViewState, viewState!);

                    return updatedViewState;
                }

                viewState = await CreateViewStateAsync(key, cToken);

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

                //clear current cache
                _cache.Clear();

                var data = await DataInitializeAsync(cToken);

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
        /// <param name="lookUpkey">View state key.</param>
        /// <param name="state">View state.</param>
        /// <returns>True if found in cache, otherwise false.</returns>
        protected bool TryGetState(TLookUpkey lookUpkey, [NotNullWhen(true)] out TViewState? state) =>
            _cache.TryGetValue(lookUpkey, out state);
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
        /// <param name="cToken">Cancelation token.</param>
        /// <returns> Task.</returns>
        protected async Task HandleChangesAsync(TLookUpkey key, LookupServiceChangeType modificationType, CancellationToken cToken = default)
        {
            try
            {
                switch (modificationType)
                {
                    case LookupServiceChangeType.Modified:
                    case LookupServiceChangeType.Added:
                        _ = await GetStateAsync(key, withUpdate: true, cToken);
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
        #endregion

        #region ABSTRACT FUNCTIONS
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
        protected abstract Task<IDictionary<TLookUpkey, TViewState>> DataInitializeAsync(CancellationToken cToken);
        /// <summary>
        /// Responsible of creating the view state.
        /// </summary>
        /// <param name="lookUpkey">View state lookup key.</param>
        /// <param name="cToken">Cancellation token.</param>
        /// <returns>Created view state.</returns>
        /// <remarks>
        /// This function will only be called if we cant obtain the view state with <paramref name="lookUpkey"/> specified from cache.<br></br>
        /// It is responsible of obtaining view state for signle item.<br></br>
        /// <b>This function should not attempt to modify cache, its only purpose to create view state.</b>
        /// </remarks>
        protected abstract ValueTask<TViewState> CreateViewStateAsync(TLookUpkey lookUpkey, CancellationToken cToken = default);
        /// <summary>
        /// Responsible of updating the view state.
        /// </summary>
        /// <param name="cToken">Cancellation token.</param>
        /// <returns>Updated view state.</returns>
        /// <remarks>
        /// It is responsible of updating view state for signle item.<br></br>
        /// <b>This function should modify the cache, its only purpose is to update the view state.</b>
        /// </remarks>
        protected abstract ValueTask<TViewState> UpdateViewStateAsync(TViewState viewState, CancellationToken cToken = default);
        /// <summary>
        /// Creates default view state.
        /// </summary>
        /// <param name="lookUpkey">Lookup key.</param>
        /// <returns>Created default view state.</returns>
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