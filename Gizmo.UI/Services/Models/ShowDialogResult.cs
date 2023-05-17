namespace Gizmo.UI.Services
{
    /// <summary>
    /// Dialog show result.
    /// </summary>
    public sealed class ShowDialogResult<TResult> where TResult : class, new()
    {
        #region CONSTRUCTOR
        public ShowDialogResult(DialogResult addResult, TaskCompletionSource<TResult>? tcs)
        {
            Result = addResult;
            _task = tcs?.Task ?? Task.FromResult<TResult>(new());
        }
        #endregion

        #region FIELDS
        private readonly Task<TResult> _task;
        #endregion

        #region PROPERTIES

        /// <summary>
        /// Gets dialog additon result.
        /// </summary>
        public DialogResult Result { get; private set; }

        /// <summary>
        /// Gets created dialog controller.
        /// </summary>
        /// <remarks>
        /// The value will be null in case <see cref="Result"/> value is equal to <see cref="DialogResult.Failed"/>.
        /// </remarks>
        public IDialogController? Controller { get; init; }

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// Waits for dialog result and set Result property.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task<TResult?> WaitForDialogResultAsync(CancellationToken cancellationToken = default)
        {
            return await _task.ContinueWith(task =>
            {
                if(task.IsFaulted)
                {
                    Result = DialogResult.Failed;
                    return null;
                }   
                else if(task.IsCompletedSuccessfully)
                {
                    Result = DialogResult.Ok;
                    return task.Result;
                }
                else
                {
                    Result = DialogResult.Canceled;
                    return null;
                }
            }, cancellationToken);
        }

        #endregion
    }
}
