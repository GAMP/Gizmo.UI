using Gizmo.UI.View.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gizmo.UI
{
    /// <summary>
    /// Service provider extensions.
    /// </summary>
    public static class ServiceProviderExtensions
    {
        #region FUNCTIONS
        
        /// <summary>
        /// Initializes view services.
        /// </summary>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Associated task.</returns>
        public static async Task InitializeViewsServices(this IServiceProvider serviceProvider, CancellationToken ct = default)
        {
            //get view services
            var viewServices = serviceProvider.GetServices<IViewService>();

            //initialize view services
            foreach (var service in viewServices)
            {
                await service.InitializeAsync(ct);
            }
        } 

        #endregion
    }
}
