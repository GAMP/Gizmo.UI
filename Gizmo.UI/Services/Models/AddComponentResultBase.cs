namespace Gizmo.UI.Services
{
    /// <summary>
    /// Component additon result base implementation.
    /// </summary>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <typeparam name="TController">Component controller type.</typeparam>
    public abstract class AddComponentResultBase<TResult, TController> where TResult : class, new() where TController : IComponentController
    {
        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="addResult">Addition result.</param>
        /// <param name="tcs">Completion source.</param>
        public AddComponentResultBase(AddComponentResultCode addResult, TController? controller, TaskCompletionSource<TResult>? tcs)
        {
            _result = addResult;
            _controller = controller;
            _task = tcs?.Task ?? Task.FromResult<TResult>(new());
        }

        private readonly Task<TResult> _task;
        private readonly TController? _controller;
        private AddComponentResultCode _result;

        /// <summary>
        /// Gets additon result.
        /// </summary>
        public AddComponentResultCode Result 
        { 
            get 
            {
                return _result; 
            }
        }

        /// <summary>
        /// Gets created component controller.
        /// </summary>
        /// <remarks>
        /// The value will be null in case <see cref="Result"/> value is equal to <see cref="AddComponentResultCode.Failed"/>.
        /// </remarks>
        public TController? Controller
        {
            get 
            { 
                return _controller;
            }
        }

        /// <summary>
        /// Waits for component result and set <see cref="Result"/> property.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task.</returns>
        /// <remarks>
        /// <b>Specifying <paramref name="cancellationToken"/> and cancelling does not cause dialog to be cancelled.<br></br></b><br></br>
        /// <see cref="OperationCanceledException"/> might be thrown if <paramref name="cancellationToken"/> is specified and cancelled durring wait.<br></br>
        /// If no <paramref name="cancellationToken"/> specified the cancellation will be communicated with result code.
        /// </remarks>
        /// <exception cref="OperationCanceledException"></exception>
        public async Task<TResult?> WaitForResultAsync(CancellationToken cancellationToken = default)
        {
            return await _task.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    //get base exception
                    var baseExcption = task.Exception?.GetBaseException();

                    if(baseExcption == IComponentController.DismissedException)
                    {
                        _result = AddComponentResultCode.Dismissed;
                    }
                    else if(baseExcption == IComponentController.TimeoutException)
                    {
                        _result = AddComponentResultCode.TimeOut;
                    }
                    else
                    {
                        _result = AddComponentResultCode.Failed;
                    }
                    
                    return null;
                }
                else if (task.IsCompletedSuccessfully)
                {
                    _result = AddComponentResultCode.Ok;
                    return task.Result;
                }
                else
                {
                    _result = AddComponentResultCode.Canceled;
                    return null;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Gets exception.
        /// </summary>
        public Exception? Exception
        {
            get { return _task?.Exception?.GetBaseException(); }
        }       
    }
}
