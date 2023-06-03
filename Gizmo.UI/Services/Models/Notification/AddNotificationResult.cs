namespace Gizmo.UI.Services
{
    /// <summary>
    /// Notifciation addition result.
    /// </summary>
    public sealed class AddNotificationResult<TResult> : AddComponentResultBase<TResult, INotificationController> where TResult : class, new()
    {
        public AddNotificationResult(AddComponentResultCode addResult, INotificationController? controller, TaskCompletionSource<TResult>? tcs) : base(addResult,controller, tcs) { }
    }
}
