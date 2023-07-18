using Gizmo.UI.View.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gizmo.UI
{
    /// <summary>
    /// Helper class for log category mapping.
    /// </summary>
    public sealed class UIServiceProviderExtensions
    {
    }

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
            //create logger
            var logger = serviceProvider.GetRequiredService<ILogger<UIServiceProviderExtensions>>();

            //get view services
            var viewServices = serviceProvider.GetServices<IViewService>();

            //initialize view services
            foreach (var service in viewServices)
            {
                logger.LogTrace("Initializing view service {s}.", service);
                await service.InitializeAsync(ct);
                logger.LogTrace("Initialization of view service {s} completed.", service);
            }
        } 

        #endregion
    }
}
