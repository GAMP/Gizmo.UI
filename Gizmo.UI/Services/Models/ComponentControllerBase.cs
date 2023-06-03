using Microsoft.AspNetCore.Components;

namespace Gizmo.UI.Services
{
    public abstract class ComponentControllerBase<TComponentType, TResult, TDisplayOptions> : IComponentController
       where TComponentType : ComponentBase where TResult : class
    {
        #region CONSTRUCTOR
        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="componentType">Component type.</param>
        public ComponentControllerBase(TDisplayOptions displayOptions,
            IDictionary<string, object> parameters)
        {
            ComponentType = typeof(TComponentType);
            _parameters = parameters;
            _displayOptions = displayOptions;
        }
        #endregion

        #region FIELDS
        private readonly IDictionary<string, object> _parameters;
        private readonly TDisplayOptions _displayOptions;
        #endregion

        #region PROPERTIES

        /// <summary>
        /// Component display options implementation.
        /// </summary>
        /// <remarks>
        /// This allows us to provide an optional display parameters to the dynamic component.
        /// </remarks>
        public TDisplayOptions DisplayOptions { get { return _displayOptions; } }

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
        /// Gets cancel callback.
        /// </summary>
        internal EventCallback CancelCallback { get; init; }

        /// <summary>
        /// Gets result callback.
        /// </summary>
        /// <remarks><typeparamref name="TResult"/> will be equal to <see cref="EmptyComponentResult"/> when dialog does not produce any result.</remarks>
        internal EventCallback<TResult> ResultCallback { get; init; }

        /// <summary>
        /// Gets error callback.
        /// </summary>
        internal EventCallback<Exception> ErrorCallback { get; init; }

        #endregion

        #region FUNCTIONS

        public Task CancelAsync()
        {
            return CancelCallback.InvokeAsync();
        }

        public Task ResultAsync(object result)
        {
            return ResultCallback.InvokeAsync((TResult)result);
        }

        public Task EmptyResultAsync()
        {
            return ResultCallback.InvokeAsync((TResult)(object)EmptyComponentResult.Default);
        }

        public Task ErrorResultAsync(Exception error)
        {
            return ErrorCallback.InvokeAsync(error);
        }

        public Task TimeOutResultAsync()
        {
            return ErrorResultAsync(new TimeoutException());
        }

        #endregion
    }
}
