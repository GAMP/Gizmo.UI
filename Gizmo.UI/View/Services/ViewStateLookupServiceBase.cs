using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Gizmo.UI.View.States;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;

namespace Gizmo.UI.View.Services;

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
        _debounceService = serviceProvider.GetRequiredService<ViewStateDebounceService>();
    #endregion

    #region PRIVATE FIELDS
    private int _dataInitializedInt;
    private bool dataInitialized
    {
        get { return Interlocked.CompareExchange(ref _dataInitializedInt, 0, 0) == 1; }
        set { Interlocked.Exchange(ref _dataInitializedInt, value ? 1 : 0); }
    }
    private readonly ViewStateDebounceService _debounceService;
    private readonly ConcurrentDictionary<TLookUpkey, TViewState> _cache = new();
    #endregion

    #region PUBLIC EVENTS
    public event EventHandler<LookupServiceChangeArgs>? Changed;
    #endregion

    #region PUBLIC FUNCTIONS
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

        if (_cache.TryGetValue(key, out var value))
            return value;

        try
        {
            var viewState = await CreateViewStateAsync(key, cToken);

            _cache.TryAdd(key, viewState);

            return viewState;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed creating view state.");
            return CreateDefaultViewState(key);
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
    /// Add the state to the cache.
    /// </summary>
    /// <param name="lookUpkey">Lookup key.</param>
    /// <param name="state">View state.</param>
    protected void AddOrUpdateViewState(TLookUpkey lookUpkey, TViewState state) =>
        _cache.AddOrUpdate(lookUpkey, state, (_, __) => state);

    /// <summary>
    /// Debounces view state change.
    /// </summary>
    /// <param name="viewState">View state.</param>
    /// <exception cref="ArgumentNullException">thrown in case <paramref name="viewState"/>is equal to null.</exception>
    protected void DebounceViewStateChange(IViewState viewState) =>
        _debounceService.Debounce(viewState);

    /// <summary>
    /// Handles changes of incoming data.
    /// </summary>
    /// <param name="key">Lookup key.</param>
    /// <param name="modificationType">Type of changes.</param>
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
                    var newState = await CreateViewStateAsync(key, cToken);
                    AddOrUpdateViewState(key, newState);
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

    #region PRIVATE FUNCTIONS
    private async ValueTask EnsureDataInitialized(CancellationToken cToken)
    {
        if (dataInitialized)
            return;

        try
        {
            //clear current cache
            _cache.Clear();

            //initialize data
            dataInitialized = await DataInitializeAsync(cToken);

            //view states/data was initialized
            RaiseChanged(LookupServiceChangeType.Initialized);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Data initialization failed.");
            dataInitialized = false;
        }
    }
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
