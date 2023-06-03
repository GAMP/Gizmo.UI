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
        public AddComponentResultBase(AddComponentResultCode addResult, TaskCompletionSource<TResult>? tcs)
        {
            Result = addResult;
            _task = tcs?.Task ?? Task.FromResult<TResult>(new());
        }

        /// <summary>
        /// Completion task.
        /// </summary>
        private readonly Task<TResult> _task;

        /// <summary>
        /// Gets additon result.
        /// </summary>
        public AddComponentResultCode Result { get; private set; }

        /// <summary>
        /// Gets created component controller.
        /// </summary>
        /// <remarks>
        /// The value will be null in case <see cref="Result"/> value is equal to <see cref="AddComponentResultCode.Failed"/>.
        /// </remarks>
        public TController? Controller { get; init; }

        /// <summary>
        /// Waits for dialog result and set Result property.
        /// </summary>
        /// <returns>Task.</returns>
        /// <exception cref="OperationCanceledException"></exception>
        public async Task<TResult?> WaitForDialogResultAsync(CancellationToken cancellationToken = default)
        {
            return await _task.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    if(task.Exception?.GetBaseException() is TimeoutException)
                    {
                        Result = AddComponentResultCode.TimeOut;
                    }
                    else
                    {
                        Result = AddComponentResultCode.Failed;
                    }
                    
                    return null;
                }
                else if (task.IsCompletedSuccessfully)
                {
                    Result = AddComponentResultCode.Ok;
                    return task.Result;
                }
                else
                {
                    Result = AddComponentResultCode.Canceled;
                    return null;
                }
            }, cancellationToken);
        }
    }
}
