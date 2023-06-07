using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// UI Composition service base implementation.
    /// </summary>
    public abstract class UICompositionServiceBase : IUICompositionService , IDisposable
    {
        #region CONSTRUCTOR
        public UICompositionServiceBase(IOptionsMonitor<UICompositionOptions> optionsMonitor, ILogger logger, IServiceProvider serviceProvider)
        {
            _optionsMonitor = optionsMonitor;
            _changeMonitor = _optionsMonitor.OnChange(OnCompositionSettingsChanged);

            _logger = logger;
            _serviceProvider = serviceProvider;
        }
        #endregion

        #region EVENTS
        public event EventHandler<EventArgs>? Initialized;
        #endregion

        #region FIELDS
        private readonly IDisposable _changeMonitor;
        private readonly IOptionsMonitor<UICompositionOptions> _optionsMonitor;
        protected HashSet<Assembly> _addtionalAssemblies = new();
        protected Assembly? _appAssembly = default;
        protected Type? _rootComponentType = default;
        protected Type? _notificationsComponentType = default;
        protected List<UIPageModuleMetadata> _pageModules = new();
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private bool _isInitialized = false;
        #endregion

        #region PROPERTIES

        /// <inheritdoc/>
        public virtual IEnumerable<Assembly> AdditionalAssemblies
        {
            get { return _addtionalAssemblies; }
        }

        /// <inheritdoc/>
        public virtual Assembly? AppAssembly
        {
            get { return _appAssembly; }
        }

        /// <inheritdoc/>
        public IEnumerable<UIPageModuleMetadata> PageModules
        {
            get { return _pageModules; }
        }

        /// <inheritdoc/>
        public Type? RootComponentType => _rootComponentType;

        /// <inheritdoc/>
        public Type? NotificationsComponentType => _notificationsComponentType;

        /// <inheritdoc/>
        public bool IsInitialized
        {
            get { return _isInitialized; }
            private set { _isInitialized = value; }
        }

        #region PROTECTED

        /// <summary>
        /// Gets logger instance.
        /// </summary>
        protected ILogger Logger
        {
            get { return _logger; }
        }

        /// <summary>
        /// Gets service provider.
        /// </summary>
        protected IServiceProvider ServiceProvider
        {
            get { return _serviceProvider; }
        }

        #endregion

        #endregion

        #region EVENT HANDLERS

        protected virtual void OnCompositionSettingsChanged(UICompositionOptions uICompositionSettings, string setting)
        {
        }

        #endregion

        #region FUNCTIONS

        /// <inheritdoc/>
        public virtual async Task InitializeAsync(CancellationToken ct)
        {  
            //reset initialization value
            IsInitialized = false;

            //here we should attempt to load any dependent assemblies

            //first we need to get the additional assemblies from configuration
            //second step is to try to load them into current app domain

            //get client app settings
            var appConfiguration = _optionsMonitor.CurrentValue;

            //validate configuration

            //app assembly must be set
            if(string.IsNullOrWhiteSpace(appConfiguration.AppAssembly))
            {
                Logger.LogWarning("App assembly is not set, aborting initialization.");
                return;
            }

            //root component type must be set
            if (string.IsNullOrWhiteSpace(appConfiguration.RootComponentType))
            {
                Logger.LogWarning("Root component type is not set, aborting initialization.");
                return;
            }

            //root component type must be set
            if (string.IsNullOrWhiteSpace(appConfiguration.NotificationsComponentType))
            {
                Logger.LogWarning("Notifications component type is not set, aborting initialization.");
                return;
            }

            //get external assemblies from the configuration
            string[] externalAssemblies = appConfiguration.AdditionalAssemblies.ToArray();

            //try to load external librares configured in our settings file
            foreach (var externalAssembly in externalAssemblies)
            {
                try
                {
                    //load external assembly
                    var assembly = await LoadAssemblyAsync(externalAssembly, ct);

                    //add additional assembly
                    _addtionalAssemblies.Add(assembly);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Could not load external assembly, assembly name : {AssemblyName}", externalAssembly);
                }
            }

            try
            {
                //load app assembly
                _appAssembly = await LoadAssemblyAsync(appConfiguration.AppAssembly, ct);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Could not load app assembly, assembly name : {AssemblyName}", appConfiguration.AppAssembly);
                return;
            }

            try
            {

                //get root component type
                _rootComponentType = Type.GetType(appConfiguration.RootComponentType);

                //get notifications component type
                _notificationsComponentType = Type.GetType(appConfiguration.NotificationsComponentType);

                //create list of all assembiles
                var targetAssemblies = AdditionalAssemblies
                    .ToArray()
                    .Append(_appAssembly)
                    .ToArray();

                //populate page modules
                _pageModules = targetAssemblies
                    .SelectMany(assembly => assembly.GetTypes().Where(type => type.GetCustomAttribute<PageUIModuleAttribute>() != null))
                    .Select(type => new UIPageModuleMetadata()
                    {
                        Title = type.GetCustomAttribute<PageUIModuleAttribute>()?.Title,
                        TitleLocalizationKey = type.GetCustomAttribute<PageUIModuleAttribute>()?.TitleLocalizationKey,
                        Description = type.GetCustomAttribute<PageUIModuleAttribute>()?.Description,
                        DescriptionLocalizationKey = type.GetCustomAttribute<PageUIModuleAttribute>()?.DescriptionLocalizationKey,
                        DefaultRoute = type.GetCustomAttribute<DefaultRouteAttribute>()?.Template,
                        DefaultRouteMatch = type.GetCustomAttribute<DefaultRouteAttribute>()?.DefaultRouteMatch ?? NavlinkMatch.All,
                        Routes = GetRoutes(type),
                        DisplayOrder = type.GetCustomAttribute<ModuleDisplayOrderAttribute>()?.DisplayOrder ?? 0,
                        Guid = type.GetCustomAttribute<ModuleGuidAttribute>()?.Guid,
                        Type = type
                    })
                    .OrderBy(metaData => metaData.DisplayOrder)
                    .ToList();

                foreach (var pageModule in _pageModules)
                {
                    Logger.LogInformation("Found page module {ModuleType}", pageModule.Type.Name);
                    Logger.LogInformation("Default route {DefaultRoute}", pageModule.DefaultRoute);
                    foreach (var route in pageModule.Routes)
                    {
                        Logger.LogInformation("Found route template {Route}", route);
                    }
                }

                Initialized?.Invoke(this, EventArgs.Empty);

                //update initialization value
                IsInitialized = true;
            }
            catch (Exception ex) 
            {
                Logger.LogError(ex, "Initialization failed.");
            }
        }

        /// <summary>
        /// Loads the specified library into current appdomain and service.
        /// </summary>
        /// <param name="assemblyName">Assembly name.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Associated task.</returns>
        protected abstract Task<Assembly> LoadAssemblyAsync(string assemblyName, CancellationToken ct = default);

        /// <summary>
        /// Gets routes from the specified type.
        /// </summary>
        /// <param name="type">Component type.</param>
        /// <returns>List of routes exposed by component.</returns>
        protected virtual IEnumerable<string> GetRoutes(Type type)
        {
            return type.GetCustomAttributes<RouteAttribute>().Select(attribute => attribute.Template).ToArray();
        }

        #endregion

        #region IDisposable
        public void Dispose()
        {
            _changeMonitor?.Dispose();
        } 
        #endregion
    }
}
