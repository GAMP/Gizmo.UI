using System.Globalization;
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
            IStringLocalizer stringLocalizer,
            IOptions<ClientUIOptions> options)
        {
            Logger = logger;
            Localizer = stringLocalizer;
            _cultureOptions = options.Value.CultureOutputOptions;
        }
        #endregion

        #region FIELDS

        #region PRIVATE

        private readonly object[] _defaultArgs = Array.Empty<object>();
        private readonly CultureOutputOptions _cultureOptions;

        #endregion

        #region PUBLIC

        /// <inheritdoc/>
        public abstract IEnumerable<CultureInfo> SupportedCultures { get; }

        #endregion

        #endregion

        #region PROPERTIES

        #region PRIVATE

        /// <summary>
        /// Gets localizer instance.
        /// </summary>
        private IStringLocalizer Localizer { get; }

        /// <summary>
        /// Gets logger instance.
        /// </summary>
        protected ILogger Logger { get; }

        #endregion

        #endregion

        #region FUNCTIONS

        #region PUBLIC

        public abstract Task SetCurrentCultureAsync(CultureInfo culture);
        public abstract CultureInfo GetCulture(string twoLetterISOLanguageName);

        /// <inheritdoc/>
        public string GetString(string key)
        {
            return GetString(key, _defaultArgs);
        }

        /// <inheritdoc/>
        public virtual string GetString(string key, params object[] arguments)
        {
            return Localizer.GetString(key, arguments);
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

        #region PRIVATE
        protected void OverrideCultureCurrencyConfiguration(IEnumerable<CultureInfo> cultures)
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
