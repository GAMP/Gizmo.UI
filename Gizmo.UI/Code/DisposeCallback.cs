namespace Gizmo.UI
{
    /// <summary>
    /// Class used to invoke callback on dispose.
    /// </summary>
    public sealed class DisposeCallback : IDisposable
    {
        public DisposeCallback(Action callback)
        {
            _callback = callback;
        }

        private readonly Action _callback;

        public void Dispose()
        {
            _callback.Invoke();
        }
    }
}
