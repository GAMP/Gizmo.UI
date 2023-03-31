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
            _resourceManager = GetResourceManager();
        }
        #endregion

        #region PRIVATE FIELDS

        private readonly object[] _defaultArgs = Array.Empty<object>();

        private readonly IStringLocalizer _localizer;
        private readonly ResourceManager _resourceManager;

        private readonly ClientCurrencyOptions _cultureOptions;

        #endregion

        #region PROPERTIES
        /// <summary>
        /// Gets logger instance.
        /// </summary>
        protected ILogger Logger { get; }

        #endregion

        #region ABSTRACT FUNCTIONS
        /// <inheritdoc/>
        public abstract Task SetCurrentCultureAsync(CultureInfo culture);
        #endregion

        #region VIRTUAL FUNCTIONS

        /// <inheritdoc/>
        public virtual ValueTask<IEnumerable<CultureInfo>> GetSupportedCulturesAsync(CancellationToken cToken = default)
        {
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            var supportedCultures = cultures
                .Where(culture => !string.IsNullOrEmpty(culture.Name) && culture.Name.Length > 2) // Ignore invariant cultures
                .Where(culture =>
                {
                    try
                    {
                        var resourceSet = _resourceManager.GetResourceSet(culture, true, false);
                        return resourceSet != null;
                    }
                    catch (CultureNotFoundException ex)
                    {
                        Logger.LogError(ex, "Could not obtain resource set for {culture}.", culture);
                        return false;
                    }
                })
                .DistinctBy(x => x.LCID)
                .Select(culture => new CultureInfo(culture.LCID))
                .ToList();

            if (!supportedCultures.Any())
                supportedCultures.Add(new("en-US"));

            SetCurrencyOptions(supportedCultures);

            return new(supportedCultures);
        }

        #endregion

        #region SHARED FUNCTIONS

        /// <summary>
        /// Sets currency options  from the configuration for the <paramref name="cultures"/>.
        /// </summary>
        /// <param name="cultures">
        /// Cultures to set currency options for.
        /// </param>
        protected virtual void SetCurrencyOptions(IEnumerable<CultureInfo> cultures)
        {
            if (!string.IsNullOrWhiteSpace(_cultureOptions.CurrencySymbol))
                foreach (var culture in cultures)
                    culture.NumberFormat.CurrencySymbol = _cultureOptions.CurrencySymbol;

            if (_cultureOptions.CurrencyDecimalDigits.HasValue)
                foreach (var culture in cultures)
                    culture.NumberFormat.CurrencyDecimalDigits = _cultureOptions.CurrencyDecimalDigits.Value;

            if (!string.IsNullOrWhiteSpace(_cultureOptions.CurrencyDecimalSeparator))
                foreach (var culture in cultures)
                    culture.NumberFormat.CurrencyDecimalSeparator = _cultureOptions.CurrencyDecimalSeparator;

            if (!string.IsNullOrWhiteSpace(_cultureOptions.CurrencyGroupSeparator))
                foreach (var culture in cultures)
                    culture.NumberFormat.CurrencyGroupSeparator = _cultureOptions.CurrencyGroupSeparator;

            if (_cultureOptions.CurrencyGroupSizes != null)
                foreach (var culture in cultures)
                    culture.NumberFormat.CurrencyGroupSizes = _cultureOptions.CurrencyGroupSizes;

            if (_cultureOptions.CurrencyNegativePattern.HasValue)
                foreach (var culture in cultures)
                    culture.NumberFormat.CurrencyNegativePattern = _cultureOptions.CurrencyNegativePattern.Value;

            if (_cultureOptions.CurrencyPositivePattern.HasValue)
                foreach (var culture in cultures)
                    culture.NumberFormat.CurrencyPositivePattern = _cultureOptions.CurrencyPositivePattern.Value;
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

        #region PRIVATE FUNCTIONS

        private ResourceManager GetResourceManager()
        {
            var field = _localizer.GetType().GetField("_localizer", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("Resource manager is invalid");

            if (field.GetValue(_localizer) is not ResourceManagerStringLocalizer resourceManagerStringLocalizer)
                throw new InvalidOperationException("Resource manager is invalid");

            field = resourceManagerStringLocalizer.GetType().GetField("_resourceManager", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("Resource manager is invalid");

            return field.GetValue(resourceManagerStringLocalizer) is not ResourceManager resourceManager
                ? throw new InvalidOperationException("Resource manager is invalid")
                : resourceManager;
        }

        #endregion
    }
}
