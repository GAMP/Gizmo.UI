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
        public ComponentControllerBase(int identifier, TDisplayOptions displayOptions,
            IDictionary<string, object> parameters)
        {
            _componentType = typeof(TComponentType);

            _identifier = identifier;
            _parameters = parameters;
            _displayOptions = displayOptions;

            //add display options
            if (displayOptions != null)
                parameters.TryAdd("DisplayOptions", displayOptions);
        }
        #endregion

        #region FIELDS
        private readonly IDictionary<string, object> _parameters;
        private readonly TDisplayOptions _displayOptions;
        private readonly int _identifier;
        private readonly Type _componentType;

        private Action<Exception>? _error = default;
        private Action<TResult>? _result = default;
        private Action<bool>? _suspendTimeout = default;

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
            get { return _componentType; }
        }

        public IDictionary<string, object> Parameters { get { return _parameters; } }

        public int Identifier
        {
            get { return _identifier; }
        }

        /// <summary>
        /// Gets dismiss callback.
        /// </summary>
        private EventCallback DismissCallback { get; set; }

        /// <summary>
        /// Gets result callback.
        /// </summary>
        /// <remarks><typeparamref name="TResult"/> will be equal to <see cref="EmptyComponentResult"/> when dialog does not produce any result.</remarks>
        private EventCallback<TResult> ResultCallback { get; set; }

        /// <summary>
        /// Gets error callback.
        /// </summary>
        private EventCallback<Exception> ErrorCallback { get; set; }

        /// <summary>
        /// Gets suspend timeout callback.
        /// </summary>
        private EventCallback<bool> SuspendTimeoutCallback { get; set; }

        #endregion

        #region FUNCTIONS

        public Task DismissAsync()
        {
            return DismissCallback.InvokeAsync();
        }

        public void Dismiss()
        {
            _error?.Invoke(IComponentController.DismissedException);
        }

        public Task ResultAsync(object result)
        {
            return ResultCallback.InvokeAsync((TResult)result);
        }

        public void Result(object result)
        {
            _result?.Invoke((TResult)result);
        }

        public Task EmptyResultAsync()
        {
            return ResultCallback.InvokeAsync((TResult)(object)EmptyComponentResult.Default);
        }

        public void EmptyResult()
        {
            _result?.Invoke((TResult)(object)EmptyComponentResult.Default);
        }

        public Task ErrorResultAsync(Exception error)
        {
            return ErrorCallback.InvokeAsync(error);
        }

        public void ErrorResult(Exception error)
        {
            _error?.Invoke(error);
        }

        public Task TimeOutResultAsync()
        {
            return ErrorResultAsync(IComponentController.TimeoutException);
        }

        public void TimeOutResult()
        {
            _error?.Invoke(IComponentController.TimeoutException);
        }

        public Task SuspendTimeoutAsync(bool suspend)
        {
            return SuspendTimeoutCallback.InvokeAsync(suspend);
        }

        public void SuspendTimeout(bool suspend)
        {
            _suspendTimeout?.Invoke(suspend);
        }

        #endregion

        public void CreateCallbacks(Action<TResult> result,
            Action<Exception> error,
            Action<bool> suspendTimeout,
            IDictionary<string, object> parameters)
        {
            _error = error;
            _result = result;
            _suspendTimeout = suspendTimeout;

            //create and add dismiss event callback
            DismissCallback = EventCallback.Factory.Create(this, () => { error(IComponentController.DismissedException); });
            parameters.TryAdd("DismissCallback", DismissCallback);

            //create and add result event callback
            ResultCallback = EventCallback.Factory.Create(this, result);
            parameters.TryAdd("ResultCallback", ResultCallback);

            //create and add error event callback
            ErrorCallback = EventCallback.Factory.Create(this, error);
            parameters.TryAdd("ErrorCallback", ErrorCallback);

            //create and add suspend timeout event callback
            SuspendTimeoutCallback = EventCallback.Factory.Create(this, suspendTimeout);
            parameters.TryAdd("SuspendTimeoutCallback", SuspendTimeoutCallback);
        }
    }
}
