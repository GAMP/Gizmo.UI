namespace Gizmo.UI.Services
{
    /// <summary>
    /// Add component result code.
    /// </summary>
    /// <remarks>
    /// This code provide addition status code.
    /// </remarks>
    public enum AddComponentResultCode
    {
        /// <summary>
        /// Component was succesfuly added.
        /// </summary>
        Opened,
        /// <summary>
        /// Component was closed with result.
        /// </summary>
        Ok,
        /// <summary>
        /// Component was closed without result.
        /// </summary>
        Canceled,
        /// <summary>
        /// Component was not added.
        /// </summary>
        Failed,
        /// <summary>
        /// Component timed out.
        /// </summary>
        TimeOut,
    }
}
