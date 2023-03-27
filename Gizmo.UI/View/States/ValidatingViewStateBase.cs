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

        public bool? IsValid
        {
            get { return _isValid; }
            set { _isValid = value; }
        }

        public bool IsValidating
        {
            get
            { return _isValidating; }
            set
            {
                _isValidating = value;
            }
        }

        #endregion
    }
}
