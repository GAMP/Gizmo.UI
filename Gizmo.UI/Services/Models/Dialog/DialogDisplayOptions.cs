namespace Gizmo.UI.Services
{
    /// <summary>
    /// Dialog display options.
    /// </summary>
    public sealed class DialogDisplayOptions
    {
        #region CONSTRUCTOR
        /// <summary>
        /// Creates new instance.
        /// </summary>
        public DialogDisplayOptions()
        {
        }
        #endregion

        #region FIELDS
        private readonly bool _closable = true;
        private readonly bool _closeOnClick = true; 
        #endregion

        #region PROPERTIES

        /// <summary>
        /// Indicates that dialog is closable.
        /// </summary>
        /// <remarks><b>True</b> by default.</remarks>
        public bool Closable
        {
            get
            {
                return _closable;
            }
            init
            {
                _closable = value;
            }
        }

        /// <summary>
        /// Indicates if dialog can be closed if an click occurs outsdie of dialog visual area.
        /// </summary>
        /// <remarks>This value will only have effect if <see cref="Closable"/> property is set to true.<br></br>
        /// <b>True</b> by default.
        /// </remarks>
        public bool CloseOnClick 
        {
            get { return _closeOnClick; }
            init { _closeOnClick = value; } 
        } 

        #endregion
    }
}
