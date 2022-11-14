using Microsoft.AspNetCore.Components;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// Dynamic component dialog base.
    /// </summary>
    /// <typeparam name="TComponentType">Component type.</typeparam>
    /// <remarks>
    /// This dialog is used to provide the ability to show dialogs with any Razor component as content.
    /// </remarks>
    public sealed class DynamicComponentDialog<TComponentType,TResult> : IDynamicComponentDialog where TComponentType : ComponentBase
    {
        #region CONSTRUCTOR
        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="componentType">Component type.</param>
        public DynamicComponentDialog(DialogDisplayOptions displayOptions,
            IDictionary<string, object> parameters)
        {
            ComponentType = typeof(TComponentType);
            _parameters = parameters;
            _displayOptions= displayOptions;
        }
        #endregion

        #region FIELDS
        private readonly IDictionary<string, object> _parameters;
        private readonly DialogDisplayOptions _displayOptions;
        #endregion

        #region PROPERTIES

        public DialogDisplayOptions DisplayOptions { get { return _displayOptions; } }

        public Type ComponentType
        {
            get;
        }

        public IDictionary<string, object> Parameters { get { return _parameters; } }

        /// <summary>
        /// Gets dialog cancel callback.
        /// </summary>
        public EventCallback CancelCallback { get; init; }

        /// <summary>
        /// Gets optional dialog result callback.
        /// </summary>
        public EventCallback<TResult>? ResultCallback { get; init; } = null;       

        #endregion

        #region FUNCTIONS
        
        public Task CancelAsync()
        {
            return CancelCallback.InvokeAsync();
        }

        public Task ProvideResultAsync(object result)
        {
            if (ResultCallback == null)
                return Task.CompletedTask;

            return ResultCallback.Value.InvokeAsync((TResult)result);
        } 

        #endregion
    }
}
