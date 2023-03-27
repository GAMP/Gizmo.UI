namespace Gizmo.UI.View.States
{
    /// <summary>
    /// View state base class.
    /// </summary>
    public abstract class ViewStateBase : IViewState
    {
        #region EVENTS
        public event EventHandler? OnChange;
        #endregion

        #region FIELDS
        private bool? _isInitialized;
        private bool _isInitializing;
        private bool _isDirty;
        #endregion

        #region PROPERTIES

        public bool? IsInitialized
        {
            get { return _isInitialized; }
            set { _isInitialized = value; }
        }

        public bool IsInitializing
        {
            get { return _isInitializing; }
            set { _isInitializing = value; }
        }

        public bool IsDirty
        {
            get { return _isDirty; }
            set { _isDirty = value; }
        }

        #endregion

        #region FUNCTIONS

        public void RaiseChanged()
        {
            OnChange?.Invoke(this, EventArgs.Empty);
        }

        public virtual void SetDefaults()
        {
        }

        #endregion       
    }
}
