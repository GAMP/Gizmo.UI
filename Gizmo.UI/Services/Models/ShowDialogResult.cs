namespace Gizmo.UI.Services
{
    /// <summary>
    /// Dialog show result.
    /// </summary>
    public sealed class ShowDialogResult<TResult> where TResult : class, new()
    {
        #region CONSTRUCTOR
        public ShowDialogResult(TaskCompletionSource<TResult>? tcs)
        {
            _tcs = tcs;
        }
        #endregion

        #region FIELDS
        private static readonly Task<TResult> CompletedTask = Task.FromResult<TResult>(new());
        private readonly TaskCompletionSource<TResult>? _tcs;
        #endregion

        #region PROPERTIES

        /// <summary>
        /// Gets dialog additon result.
        /// </summary>
        public DialogAddResult Result { get; init; }

        /// <summary>
        /// Gets created dialog controller.
        /// </summary>
        /// <remarks>
        /// The value will be null in case <see cref="Result"/> value is equal to <see cref="DialogAddResult.Failed"/>.
        /// </remarks>
        public IDialogController? Controller
        {
            get; init;
        } = null;

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// Waits for dialog result.
        /// </summary>
        /// <returns>Dialog result.</returns>
        /// <exception cref="OperationCanceledException">thrown if dialog was canclled by the user or <paramref name="cancellationToken"/>.</exception>
        public async Task<TResult> WaitForDialogResultAsync(CancellationToken cancellationToken = default) => _tcs == null ? await CompletedTask : await _tcs.Task.WaitAsync(cancellationToken);

        #endregion
    }
}
