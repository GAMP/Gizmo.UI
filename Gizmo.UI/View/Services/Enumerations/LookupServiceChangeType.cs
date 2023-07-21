namespace Gizmo.UI.View.Services
{
    /// <summary>
    /// Lookup view state change type.
    /// </summary>
    public enum LookupServiceChangeType
    {
        None = 0,
        /// <summary>
        /// View states initialized.
        /// </summary>
        Initialized = 1,
        /// <summary>
        /// View state added.
        /// </summary>
        Added = 2,
        /// <summary>
        /// View state removed.
        /// </summary>
        Removed = 3,
        /// <summary>
        /// View state modified.
        /// </summary>
        Modified = 4,
    }
}

