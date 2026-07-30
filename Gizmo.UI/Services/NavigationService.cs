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
        private string? _lastLocation;
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
        public void AssociateNavigationManager(NavigationManager navigationManager)
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

            //a new manager association means a new document (initial attach or web view recreation after a crash),
            //the last location belongs to the previous document so it must not suppress the event below,
            //view services rely on it to reconcile current state with the fresh document location
            _lastLocation = null;

            OnNavigationManagerLocationChanged(this, new LocationChangedEventArgs(navigationManager.Uri, false));
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

        /// <summary>
        /// Opens page with "window.open" java script function.
        /// </summary>
        /// <param name="uri">Uri.</param>
        /// <param name="target">Target.</param>
        public async Task OpenPageAsync(string  uri, string target= "_blank")
        {
            uri = CreateUri(uri);

           if(_jsRuntime.JSRuntime!=null)
                await _jsRuntime.JSRuntime!.InvokeVoidAsync("window.open", uri, target);
        }

        public string CreateUri(string uri)
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

            return uri;
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
            if (string.Equals(_lastLocation, e.Location, StringComparison.Ordinal))
                return;

            _lastLocation = e.Location;

            LocationChanged?.Invoke(this, e);
        }

        #endregion
    }
}
