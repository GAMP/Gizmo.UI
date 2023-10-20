namespace Gizmo.UI.Services
{
    /// <summary>
    /// Dialog addition result.
    /// </summary>
    public sealed class AddDialogResult<TResult> : AddComponentResultBase<TResult, IDialogController> where TResult : class, new()
    {
        public AddDialogResult(AddComponentResultCode addResult, IDialogController? controller, TaskCompletionSource<TResult>? tcs) 
            : base(addResult, controller, tcs) { }
    }
}
