namespace Gizmo.UI.View.States
{
    public sealed class SelectValueViewState<TValueType> : ViewStateBase
    {
        /// <summary>
        /// Display value.
        /// </summary>
        public string DisplayValue { get; set; } = string.Empty;

        /// <summary>
        /// Selected value.
        /// </summary>
        public TValueType Value { get; set; } = default!;
    }
}
