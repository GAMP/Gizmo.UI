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
            IOptions<ClientCurrencyOptions> options)
        {
            Logger = logger;
            _localizer = localizer;
            _cultureOptions = options.Value;

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

        public IEnumerable<CultureInfo> SupportedCultures => _supportedCultures ??= GetSupportedCultures();

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

        public virtual IEnumerable<CultureInfo> GetSupportedCultures()
        {
            var supportedLanguages = new string[] { "en-US", "el-GR", "ru-RUl" };
            
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            
            var supportedCultures = cultures
                .Where(culture => supportedLanguages.Contains(culture.Name))
                .Where(culture =>
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
                })
                .DistinctBy(x => x.TwoLetterISOLanguageName)
                .Select(culture => new CultureInfo(culture.Name))
                .ToList();

            //replace invariant culture with default english
            if (supportedCultures.Contains(CultureInfo.InvariantCulture))
            {
                supportedCultures.Remove(CultureInfo.InvariantCulture);
                supportedCultures.Insert(0, CultureInfo.GetCultureInfo("en-us"));
            }

            SetCurrencyOptions(supportedCultures);

            return supportedCultures;
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
        protected void SetCurrencyOptions(IEnumerable<CultureInfo> cultures)
        {
            if (!string.IsNullOrWhiteSpace(_cultureOptions.CurrencySymbol))
            {
                foreach (var culture in cultures)
                {
                    culture.NumberFormat.CurrencySymbol = _cultureOptions.CurrencySymbol;
                }
            }
        }
        #endregion
        
        #endregion
    }
}
