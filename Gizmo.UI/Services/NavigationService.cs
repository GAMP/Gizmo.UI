using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// Navigation service.
    /// </summary>
    public sealed class NavigationService
    {
        #region CONSTRUCTOR
        public NavigationService(JSRuntimeService jsRuntime, ILogger<NavigationService> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }
        #endregion

        #region FIELDS
        private readonly ILogger<NavigationService> _logger;
        private readonly JSRuntimeService _jsRuntime;
        private NavigationManager? _navigationManager;
        private readonly TaskCompletionSource _associateTask = new();
        private readonly TimeSpan _associateWaitTime = TimeSpan.FromSeconds(10);
        #endregion

        #region EVENTS

        /// <summary>
        /// An event that fires when the navigation location has changed.
        /// </summary>
        public event EventHandler<LocationChangedEventArgs>? LocationChanged;

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// Associates navigation manager with this service.
        /// </summary>
        /// <param name="navigationManager">Navigation manager.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AssociateNavigtionManager(NavigationManager navigationManager)
        {
            if (navigationManager == null)
                throw new ArgumentNullException(nameof(navigationManager));

            if (_navigationManager != null)
            {
                _navigationManager.LocationChanged -= OnNavigationManagerLocationChanged;
            }

            _navigationManager = navigationManager;
            _navigationManager.LocationChanged += OnNavigationManagerLocationChanged;

            _associateTask.TrySetResult();

            LocationChanged?.Invoke(this, new LocationChangedEventArgs(navigationManager.Uri, false));
        }

        public void NavigateTo(string uri, NavigationOptions options = default)
        {            
            _associateTask.Task.Wait(_associateWaitTime);

            //https://github.com/dotnet/aspnetcore/issues/25204           
            if (!IsBaseUriRoot)
            {
                if (uri == "/")
                {
                    uri = _navigationManager!.BaseUri;
                }
                else
                {
                    if (uri.StartsWith("/"))
                        uri = uri[1..];
                }
            }

            _logger.LogTrace("Requested navigation to {url}", uri);

            _navigationManager?.NavigateTo(uri, options);
        }

        public string GetUri()
        {
            return _navigationManager?.Uri ?? string.Empty;
        }

        public string GetBaseUri()
        {
            return _navigationManager?.BaseUri ?? string.Empty;
        }

        public bool IsBaseUriRoot
        {
            get
            {
                var baseUri = new Uri(_navigationManager!.BaseUri);
                return baseUri.LocalPath == "/";
            }
        }

        public async Task GoBackAsync()
        {
            if (_jsRuntime.JSRuntime != null)
            {
                await _jsRuntime.JSRuntime.InvokeVoidAsync("window.history.back");
            }
        }

        #endregion

        #region EVENT HANDLERS

        private void OnNavigationManagerLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            LocationChanged?.Invoke(this, e);
        }

        #endregion
    }
}
