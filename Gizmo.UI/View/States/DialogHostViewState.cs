using Gizmo.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gizmo.UI.View.States
{
    /// <summary>
    /// Dialog host view state.
    /// </summary>
    /// <remarks>
    /// Used to provide view state for Dialog host components.
    /// </remarks>
    [Register()]
    public sealed class DialogHostViewState : ViewStateBase
    {
        #region FIELDS
        private IDialogController? _current;
        #endregion

        #region PROPERTIES
        
        /// <summary>
        /// Gets current dialog.
        /// </summary>
        public IDialogController? Current
        {
            get { return _current; }
            internal set { _current = value; }
        } 

        /// <summary>
        /// Gets if there is a dialog currently shown.
        /// </summary>
        public bool HasDialog
        {
            get { return _current != null; }
        }

        #endregion
    }
}
