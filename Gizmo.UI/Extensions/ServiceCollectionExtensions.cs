using System.Reflection;
using Gizmo.UI.Services;
using Gizmo.UI.View.Services;
using Gizmo.UI.View.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
                .Where(type => !type.IsAbstract && type.GetInterfaces().Contains(typeof(IViewState)))
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
                .Where(type => !type.IsAbstract && type.GetInterfaces().Contains(typeof(IViewService)))
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
            //add js runtime service, this will be required when we need access to js runtime injection outside of blazor components
            services.TryAddSingleton<JSRuntimeService>();

            //add global cancellation service
            services.TryAddSingleton<GlobalCancellationService>();

            //add navigation service, this will be required when we need access navigation manager outside of blazor components
            services.TryAddSingleton<NavigationService>();

            services.AddTransient<DebounceActionService>();
            services.AddTransient<DebounceActionAsyncService>();
            services.AddTransient<DebounceActionAsyncWithParamService>();
            return services;
        }

        /// <summary>
        /// Adds Dialog service implementation.
        /// </summary>
        /// <typeparam name="TService">Dialog service type.</typeparam>
        /// <param name="services">Service collection.</param>
        /// <returns>Service collection.</returns>
        /// <exception cref="ArgumentException">thrown in case some of ui composition conditions are not met.</exception>
        public static IServiceCollection AddDialogService<TService>(this IServiceCollection services) where TService : IDialogService
        {
            //add dialog service by type
            services.TryAddSingleton(typeof(TService), (sp) =>
            {
                //get current compsition options
                //they will contain the configuration
                var compositionOptions = sp.GetRequiredService<IOptionsMonitor<UICompositionOptions>>();

                //get app assembly
                var appAssemblyName = compositionOptions.CurrentValue.AppAssembly;

                //check if app assembly is configured
                if (appAssemblyName == null)
                    throw new ArgumentException("App assembly not configured.");

                //load our app assembly
                var requestingAssembly = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), assembly => string.Compare(Path.GetFileName(assembly.GetName().CodeBase), appAssemblyName, true) == 0);

                //check if app assembly is loaded
                if (requestingAssembly == null)
                    throw new ArgumentException("App assembly is not loaded.");

                //get all dialog services implementation
                var dialogServices = requestingAssembly
                    .GetTypes()
                    .Where(type => !type.IsAbstract && type.GetInterfaces().Contains(typeof(IDialogService)))
                    .ToList();

                //check if any dialog services exist in app assembly
                if (dialogServices.Count == 0)
                    throw new ArgumentException($"No dialog services registered in app assembly {appAssemblyName}.");

                //get dialog service type, we could check if multiple types found ?
                var dialogServiceType = dialogServices[0];

                //create instance of dialog service
                return ActivatorUtilities.CreateInstance(sp, dialogServiceType);
            });

            //add dialog service by interface
            services.TryAddSingleton(sp => (IDialogService)sp.GetRequiredService(typeof(TService)));

            return services;
        }

        /// <summary>
        /// Adds Dialog service implementation.
        /// </summary>
        /// <typeparam name="TService">Dialog service type.</typeparam>
        /// <param name="services">Service collection.</param>
        /// <returns>Service collection.</returns>
        /// <exception cref="ArgumentException">thrown in case some of ui composition conditions are not met.</exception>
        public static IServiceCollection AddNotificationsService<TService>(this IServiceCollection services) where TService : INotificationsService
        {
            //add dialog service by type
            services.TryAddSingleton(typeof(TService), (sp) =>
            {
                //get current compsition options
                //they will contain the configuration
                var compositionOptions = sp.GetRequiredService<IOptionsMonitor<UICompositionOptions>>();

                //get app assembly
                var appAssemblyName = compositionOptions.CurrentValue.AppAssembly;

                //check if app assembly is configured
                if (appAssemblyName == null)
                    throw new ArgumentException("App assembly not configured.");

                //load our app assembly
                var requestingAssembly = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), assembly => string.Compare(Path.GetFileName(assembly.GetName().CodeBase), appAssemblyName, true) == 0);

                //check if app assembly is loaded
                if (requestingAssembly == null)
                    throw new ArgumentException("App assembly is not loaded.");

                //get all dialog services implementation
                var notificationsService = requestingAssembly
                    .GetTypes()
                    .Where(type => !type.IsAbstract && type.GetInterfaces().Contains(typeof(INotificationsService)))
                    .ToList();

                //check if any dialog services exist in app assembly
                if (notificationsService.Count == 0)
                    throw new ArgumentException($"No notifications services registered in app assembly {appAssemblyName}.");

                //get dialog service type, we could check if multiple types found ?
                var dialogServiceType = notificationsService[0];

                //create instance of notifications service
                return ActivatorUtilities.CreateInstance(sp, dialogServiceType);
            });

            //add notifications service by interface
            services.TryAddSingleton(sp => (INotificationsService)sp.GetRequiredService(typeof(TService)));

            return services;
        }

        #endregion
    }
}
