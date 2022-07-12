﻿using System.ComponentModel;

namespace Gizmo.UI.View.States
{
    /// <summary>
    /// Validating view state base class.
    /// </summary>
    public abstract class ValidatingViewStateBase : ViewStateBase, IValidatingViewState
    {
        #region FIELDS
        private bool? _isValid;
        private bool _isValidating;
        #endregion

        #region PROPERTIES
        
        [PropertyChangeIgnore()]
        public bool? IsValid { get { return _isValid; } set { SetProperty(ref _isValid, value); } }
        
        [PropertyChangeIgnore()]
        public bool IsValidating { get { return _isValidating; } set { SetProperty(ref _isValidating, value); } }

        #endregion
    }
}
