using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Gizmo.UI.Services;
using Gizmo.UI.View.Services;
using Gizmo.UI.View.States;

namespace Gizmo.UI
{
    /// <summary>
    /// Service collection extensions.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        #region FUNCTIONS
        
        /// <summary>
        /// Registers view states in the di container.
        /// </summary>
        /// <param name="services">Services instance.</param>
        /// <returns>Service collection.</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="services"/> equals to null.</exception>
        public static IServiceCollection AddViewStates(this IServiceCollection services)
        {
            return AddViewStates(services, Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// Registers view states in the di container.
        /// </summary>
        /// <param name="services">Services instance.</param>
        /// <param name="assembly">Source assembly.</param>
        /// <returns>Service collection.</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="assembly"/> or <paramref name="services"/> equals to null.</exception>
        public static IServiceCollection AddViewStates(this IServiceCollection services, Assembly assembly)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            var viewStates = assembly
                .GetTypes()
                .Where(type => type.IsAbstract == false && type.GetInterfaces().Contains(typeof(IViewState)))
                .Select(type => new { Type = type, Attribute = type.GetCustomAttribute<RegisterAttribute>() })
                .Where(result => result.Attribute != null)
                .ToList();

            foreach (var viewState in viewStates)
            {
                if (viewState?.Attribute?.Scope == RegisterScope.Scoped)
                {
                    services.AddScoped(viewState.Type);
                }
                else if (viewState?.Attribute?.Scope == RegisterScope.Singelton)
                {
                    services.AddSingleton(viewState.Type);
                }
                else if (viewState?.Attribute?.Scope == RegisterScope.Transient)
                {
                    services.AddTransient(viewState.Type);
                }
            }

            return services;
        }

        /// <summary>
        /// Registers view services in the di container.
        /// </summary>
        /// <param name="services">Services instance.</param>
        /// <param name="assembly">Source assembly.</param>
        /// <returns>Service collection.</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="assembly"/> or <paramref name="services"/> equals to null.</exception>
        public static IServiceCollection AddViewServices(this IServiceCollection services, Assembly assembly)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            var viewServices = assembly
                .GetTypes()
                .Where(type => type.IsAbstract == false && type.GetInterfaces().Contains(typeof(IViewService)))
                .Select(type => new { Type = type, Attribute = type.GetCustomAttribute<RegisterAttribute>() })
                .Where(result => result.Attribute != null)
                .ToList();

            foreach (var viewService in viewServices)
            {
                if (viewService?.Attribute?.Scope == RegisterScope.Scoped)
                {
                    services.AddScoped(viewService.Type);
                    services.AddScoped(sp => (IViewService)sp.GetRequiredService(viewService.Type));
                }
                else if (viewService?.Attribute?.Scope == RegisterScope.Singelton)
                {
                    services.AddSingleton(viewService.Type);
                    services.AddSingleton(sp => (IViewService)sp.GetRequiredService(viewService.Type));
                }
                else if (viewService?.Attribute?.Scope == RegisterScope.Transient)
                {
                    services.AddTransient(viewService.Type);
                    services.AddTransient(sp => (IViewService)sp.GetRequiredService(viewService.Type));
                }
            }

            return services;
        }

        /// <summary>
        /// Registers view services in the di container.
        /// </summary>
        /// <param name="services">Services instance.</param>
        /// <returns>Service collection.</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="services"/> equals to null.</exception>
        public static IServiceCollection AddViewServices(this IServiceCollection services)
        {
            return AddViewServices(services, Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// Registers shared UI services.
        /// </summary>
        /// <param name="services">Services instance.</param>
        /// <returns>Service collection.</returns>
        public static IServiceCollection AddUIServices(this IServiceCollection services)
        {
            services.AddSingleton<NavigationService>();
            return services;
        }

        #endregion
    }
}
