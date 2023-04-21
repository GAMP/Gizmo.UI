namespace Gizmo.UI.Services
{
    /// <summary>
    /// Dialog empty result.
    /// </summary>
    /// <remarks>
    /// This result is provided by components that dont return any result.
    /// </remarks>
    public sealed class EmptyDialogResult
    {
        /// <summary>
        /// Default result.
        /// </summary>
        public static readonly EmptyDialogResult Default = new();
    }
}
