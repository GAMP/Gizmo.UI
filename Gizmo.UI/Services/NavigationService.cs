using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// Navigation service.
    /// </summary>
    public sealed class NavigationService
    {
        #region FIELDS

        private NavigationManager? _navigationManager;

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

            if (_navigationManager == null)
            {
                _navigationManager = navigationManager;
                _navigationManager.LocationChanged += OnNavigationManagerLocationChanged;

                LocationChanged?.Invoke(this, new LocationChangedEventArgs(navigationManager.Uri, false));
            }
        }

        public void NavigateTo(string uri, NavigationOptions options = default)
        {
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

        #endregion

        #region EVENT HANDLERS

        private void OnNavigationManagerLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            LocationChanged?.Invoke(this, e);
        }

        #endregion
    }
}
