using Microsoft.AspNetCore.Components;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// Dialog controller.
    /// </summary>
    /// <typeparam name="TComponentType">Component type.</typeparam>
    /// <remarks>
    /// This dialog controller is used to provide the ability to show dialogs with any Razor component as content.
    /// </remarks>
    public sealed class DialogController<TComponentType, TResult> : 
        ComponentControllerBase<TComponentType,TResult,DialogDisplayOptions> ,
        IDialogController where TComponentType : ComponentBase where TResult : class
    {
        #region CONSTRUCTOR
        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="componentType">Component type.</param>
        public DialogController(int identifier, DialogDisplayOptions displayOptions,
            IDictionary<string, object> parameters) : base(identifier, displayOptions, parameters)
        {
        }
        #endregion
    }
}
