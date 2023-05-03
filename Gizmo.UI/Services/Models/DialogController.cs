﻿using Microsoft.AspNetCore.Components;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// Dialog controller.
    /// </summary>
    /// <typeparam name="TComponentType">Component type.</typeparam>
    /// <remarks>
    /// This dialog controller is used to provide the ability to show dialogs with any Razor component as content.
    /// </remarks>
    public sealed class DialogController<TComponentType, TResult> : IDialogController where TComponentType : ComponentBase where TResult : class
    {
        #region CONSTRUCTOR
        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="componentType">Component type.</param>
        public DialogController(DialogDisplayOptions displayOptions,
            IDictionary<string, object> parameters)
        {
            ComponentType = typeof(TComponentType);
            _parameters = parameters;
            _displayOptions = displayOptions;
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

        public int Identifier
        {
            get; init;
        }

        /// <summary>
        /// Gets dialog cancel callback.
        /// </summary>
        internal EventCallback CancelCallback { get; init; }

        /// <summary>
        /// Gets dialog result callback.
        /// </summary>
        /// <remarks><typeparamref name="TResult"/> will be equal to <see cref="EmptyDialogResult"/> when dialog does not produce any result.</remarks>
        internal EventCallback<TResult> ResultCallback { get; init; }

        #endregion

        #region FUNCTIONS

        public Task CancelAsync()
        {
            return CancelCallback.InvokeAsync();
        }

        public Task ProvideResultAsync(object result)
        {
            return ResultCallback.InvokeAsync((TResult)result);
        }

        public Task ProvideEmptyResult()
        {
            return ResultCallback.InvokeAsync((TResult)(object)EmptyDialogResult.Default);
        }

        #endregion
    }
}