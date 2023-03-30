using System.Globalization;
using System.Reflection;
using System.Resources;

using Gizmo.Client.UI;

using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// Localization service base.
    /// </summary>
    public abstract class LocalizationServiceBase : ILocalizationService
    {
        #region CONSTRUCTOR
        protected LocalizationServiceBase(
            ILogger logger,
            IStringLocalizer localizer,
            IOptions<ClientUIOptions> options)
        {
            Logger = logger;
            _localizer = localizer;
            _cultureOptions = options.Value.CurrencyOptions;

            var prop = localizer.GetType().GetField("_localizer", BindingFlags.NonPublic | BindingFlags.Instance);
            _resourceManagerStringLocalizer = (ResourceManagerStringLocalizer)prop.GetValue(localizer);
            prop = _resourceManagerStringLocalizer?.GetType().GetField("_resourceManager", BindingFlags.NonPublic | BindingFlags.Instance);
            _resourceManager = (ResourceManager)prop.GetValue(_resourceManagerStringLocalizer);
        }
        #endregion

        #region FIELDS

        #region PRIVATE

        private readonly object[] _defaultArgs = Array.Empty<object>();
        private readonly ClientCurrencyOptions _cultureOptions;
        private readonly ResourceManager _resourceManager;
        private readonly ResourceManagerStringLocalizer _resourceManagerStringLocalizer;

        #endregion

        #endregion

        #region PROPERTIES

        private IEnumerable<CultureInfo>? _supportedCultures;

        public IEnumerable<CultureInfo> SupportedCultures
        {
            get
            {
                if (_supportedCultures is null)
                {
                    Task.Run(() => GetSupportedCulturesAsync())
                        .ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                                _supportedCultures = new[]
                                {
                                    new CultureInfo("en_us"),
                                };
                            else
                            _supportedCultures = task.Result.Result;
                        });
                }
                
                return _supportedCultures!;
            }
        }

        #region PRIVATE

        /// <summary>
        /// Gets localizer instance.
        /// </summary>
        private IStringLocalizer _localizer;

        /// <summary>
        /// Gets logger instance.
        /// </summary>
        protected ILogger Logger { get; }

        #endregion

        #endregion

        #region FUNCTIONS

        #region PUBLIC

        #region ABSTRACT
        public abstract Task SetCurrentCultureAsync(CultureInfo culture);
        public abstract CultureInfo GetCulture(string twoLetterISOLanguageName);
        #endregion

        public virtual ValueTask<IEnumerable<CultureInfo>> GetSupportedCulturesAsync()
        {
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            var supportedCultures = cultures.Where(culture =>
            {
                try
                {
                    var resourceSet = _resourceManager?.GetResourceSet(culture, true, false);
                    return resourceSet != null;
                }
                catch (CultureNotFoundException ex)
                {
                    Logger.LogError(ex, "Could not obtain resource set for {culture}.", culture);
                    return false;
                }
            }).DistinctBy(x => x.TwoLetterISOLanguageName).ToList();

            //replace invariant culture with default english
            if (supportedCultures.Contains(CultureInfo.InvariantCulture))
            {
                supportedCultures.Remove(CultureInfo.InvariantCulture);
                supportedCultures.Insert(0, CultureInfo.GetCultureInfo("en-us"));
            }

            SetCurrencyOptions();

            return new ValueTask<IEnumerable<CultureInfo>>(supportedCultures);
        }

        /// <inheritdoc/>
        public string GetString(string key)
        {
            return GetString(key, _defaultArgs);
        }

        /// <inheritdoc/>
        public virtual string GetString(string key, params object[] arguments)
        {
            return _localizer.GetString(key, arguments);
        }

        /// <inheritdoc/>
        public string GetStringUpper(string key)
        {
            return GetStringUpper(key, _defaultArgs);
        }

        /// <inheritdoc/>
        public string GetStringUpper(string key, params object[] arguments)
        {
            return GetString(key, arguments).ToUpper();
        }

        /// <inheritdoc/>
        public string GetStringLower(string key)
        {
            return GetStringLower(key, _defaultArgs);
        }

        /// <inheritdoc/>
        public string GetStringLower(string key, params object[] arguments)
        {
            return GetString(key, arguments).ToLower();
        }

        #endregion

        #region PROTECTED
        protected void SetCurrencyOptions()
        {
            if (!string.IsNullOrWhiteSpace(_cultureOptions.CurrencySymbol) && _supportedCultures is not null)
            {
                foreach (var culture in _supportedCultures)
                {
                    culture.NumberFormat.CurrencySymbol = _cultureOptions.CurrencySymbol;
                }
            }
        }
        #endregion
        #endregion
    }
}
