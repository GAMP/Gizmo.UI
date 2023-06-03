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
            if(displayOptions!=null)
                parameters.TryAdd("DisplayOptions", displayOptions);
        }
        #endregion

        #region FIELDS
        private readonly IDictionary<string, object> _parameters;
        private readonly TDisplayOptions _displayOptions;
        private readonly int _identifier;
        private readonly Type _componentType;
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
        /// Gets cancel callback.
        /// </summary>
        private EventCallback CancelCallback { get; set; }

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
            return ErrorResultAsync(IComponentController.TimeoutException);
        }

        public Task SuspendTimeoutAsync(bool suspend)
        {
            return SuspendTimeoutCallback.InvokeAsync(suspend);
        }

        #endregion

        public void CreateCallbacks(Action<TResult> result,
            Action<Exception> error,
            Action cancel,
            Action<bool> suspend,
            IDictionary<string,object> parameters)
        {
            //create and add cancel event callback
            EventCallback cancelEventCallback = EventCallback.Factory.Create(this, cancel);
            CancelCallback = cancelEventCallback;
            parameters.TryAdd("CancelCallback", cancelEventCallback);

            //create and add result event callback
            EventCallback<TResult> resultEventCallabck = EventCallback.Factory.Create(this, result);
            ResultCallback = resultEventCallabck;
            parameters.TryAdd("ResultCallback", resultEventCallabck);

            //create and add error event callback
            EventCallback<Exception> errorEventCallabck = EventCallback.Factory.Create(this, error);
            ErrorCallback = errorEventCallabck;
            parameters.TryAdd("ErrorCallback", errorEventCallabck);

            //create and add suspend timeout event callback
            EventCallback<bool> suspendTimeoutEventCallback = EventCallback.Factory.Create(this, suspend);
            SuspendTimeoutCallback = suspendTimeoutEventCallback;
            parameters.TryAdd("SuspendTimeoutCallback", suspendTimeoutEventCallback);
        }

       
    }
}
