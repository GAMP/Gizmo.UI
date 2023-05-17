namespace Gizmo.UI.Services
{
    /// <summary>
    /// Dialog addition result.
    /// </summary>
    public enum DialogResult
    {
        /// <summary>
        /// Dialog was succesfuly added.
        /// </summary>
        Opened,
        /// <summary>
        /// Dialog was closed with result.
        ///  </summary>
        Ok,
        /// <summary>
        ///  Dialog was closed without result.
        ///  </summary>
        Canceled,
        /// <summary>
        /// Dialog was not added.
        /// </summary>
        Failed
    }
}
