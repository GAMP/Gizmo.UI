using System.Collections.Concurrent;
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
    public abstract class ViewStateLookupServiceBase<TLookUpkey,TViewState> : ViewServiceBase where TViewState : IViewState  where TLookUpkey : notnull
    {
        #region CONSTRUCTOR
        public ViewStateLookupServiceBase(ILogger logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        } 
        #endregion

        #region FIELDS
        private readonly SemaphoreSlim _accessLock = new(1);
        private readonly SemaphoreSlim _initializeLock = new(1);
        protected readonly ConcurrentDictionary<TLookUpkey, TViewState> _cache = new();
        private bool _dataInitialized = false;
        private event EventHandler<EventArgs>? Changed;
        #endregion

        /// <summary>
        /// Gets all view states.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>View states.</returns>
        public async ValueTask<IEnumerable<TViewState>> GetAsync(CancellationToken cancellationToken)
        {
            //this will trigger data initalization if required
            await EnsureDataInitialized(cancellationToken);

            //return any generated view states
            return _cache.Values.AsEnumerable();
        }

        /// <summary>
        /// Gets view state specified by <paramref name="key"/>.
        /// </summary>
        /// <param name="key">View state key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>View state.</returns>
        public async ValueTask<TViewState> GetAsync(TLookUpkey key, CancellationToken cancellationToken = default)
        {
            await EnsureDataInitialized(cancellationToken);
            await _accessLock.WaitAsync(cancellationToken);
            try
            {
                if(_cache.TryGetValue(key, out TViewState? value)) return value;

                var viewState = await CreateViewStateAsync(key, cancellationToken);

                _cache.TryAdd(key, viewState);

                return viewState;
            }
            catch(Exception ex) 
            {
                Logger.LogError(ex,"Failed creating view state.");
                return CreateDefaultViewStateAsync(key); 
            }
            finally
            {
               _accessLock.Release();
            }
            
        }

        private async Task EnsureDataInitialized(CancellationToken cancellationToken)
        {
            await _initializeLock.WaitAsync(cancellationToken);
            try
            {
                if(_dataInitialized) return;

                //clar current cache
                _cache.Clear();

                //initialize data
               _dataInitialized = await DataInitializeAsync(cancellationToken);

                //view states/data was changed
                RaiseChanged();              
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

        /// <summary>
        /// Initializes data.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if initialization was successful, false if retry needed.</returns>
        /// <remarks>
        /// The method is responsible of initializing initial data.<br></br>
        /// Example would be calling an service over api and getting required data and creating appropriate initial view states.<br></br>
        /// <b>The function is thread safe.</b>
        /// </remarks>
        protected virtual Task<bool> DataInitializeAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Responsible of creating the view state.
        /// </summary>
        /// <param name="lookUpkey">View state lookup key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>View state.</returns>
        /// <remarks>
        /// This function will only be called if we cant obtain the view state with <paramref name="lookUpkey"/> specified from cache.<br></br>
        /// It is responsible of obtaining view state for signle item.
        /// </remarks>
        protected abstract ValueTask<TViewState> CreateViewStateAsync(TLookUpkey lookUpkey, CancellationToken cancellationToken = default);

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
        /// <exception cref="ArgumentNullException">thrown if <typeparamref name="TViewState"/> is not registered in IOC container.</exception>
        protected virtual TViewState CreateDefaultViewStateAsync(TLookUpkey lookUpkey) => ServiceProvider.GetService<TViewState>() ?? throw new ArgumentNullException();

        /// <summary>
        /// Raises change event.
        /// </summary>
        protected void RaiseChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

    }
}
