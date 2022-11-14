namespace Gizmo.UI.Services
{
    /// <summary>
    /// Dialog show result.
    /// </summary>
    public sealed class ShowDialogResult<TResult> : ShowDialogResultBase where TResult :class
    {
        public ShowDialogResult(TaskCompletionSource<TResult>? tcs)
        {
            _tcs = tcs;
        }

        #region FIELDS
        private static readonly Task<TResult> _completedTask = Task.FromResult<TResult>(default);
        private readonly TaskCompletionSource<TResult>? _tcs; 
        #endregion

        /// <summary>
        /// Waits for dialog result.
        /// </summary>
        /// <returns></returns>
        public async Task<TResult> WaitForDialogResultAsync(CancellationToken cancellationToken = default) => _tcs == null? await _completedTask : await _tcs.Task.WaitAsync(cancellationToken);
    }
}
