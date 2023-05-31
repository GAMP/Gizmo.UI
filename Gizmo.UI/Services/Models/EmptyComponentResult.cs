namespace Gizmo.UI.Services
{
    /// <summary>
    /// Custom component empty result.
    /// </summary>
    /// <remarks>
    /// This result is provided by components that dont return any result.
    /// </remarks>
    public sealed class EmptyComponentResult
    {
        /// <summary>
        /// Default result.
        /// </summary>
        public static readonly EmptyComponentResult Default = new();
    }
}
