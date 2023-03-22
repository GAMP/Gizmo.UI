
namespace Gizmo.UI.View.Services
{
    /// <summary>
    /// Look up service change event args.
    /// </summary>
    public sealed class LookupServiceChangeArgs : EventArgs
    {
        /// <summary>
        /// Change type.
        /// </summary>
        public LookupServiceChangeType Type { get; init; }
    }
}
