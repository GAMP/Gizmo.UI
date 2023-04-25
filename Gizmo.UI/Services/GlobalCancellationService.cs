namespace Gizmo.UI.Services
{
    /// <summary>
    /// Provides shared cancellation functionality.
    /// </summary>
    /// <remarks>
    /// This service is required for cases where we need to trigger cancellation from single global source.
    /// An example is when some of dialogs are shown and user logs out, all dialogs needs to be cancelled and closed.
    /// </remarks>
    public sealed class GlobalCancellationService : IDisposable
    {
        private CancellationTokenSource? _cts;
        private readonly object _lock = new(); 

        /// <summary>
        /// Gets linked cancellation token source.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Linked cancellation token source.</returns>
        public CancellationTokenSource GetLinkedTokenSource(CancellationToken cancellationToken)
        {
           return CancellationTokenSource.CreateLinkedTokenSource(GetSource().Token, cancellationToken);
        }

        /// <summary>
        /// Gets linked cancellation token.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Linked cancellation token.</returns>
        public CancellationToken GetLinkedCancellationToken(CancellationToken cancellationToken)
        {
            return GetLinkedTokenSource(cancellationToken).Token;
        }

        private CancellationTokenSource GetSource()
        {
            lock (_lock)
            {
                _cts ??= new CancellationTokenSource();
                return _cts;
            }
        }

        /// <summary>
        /// Cancells any linked operations.
        /// </summary>
        public void Cancel()
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _cts = null;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _cts?.Dispose();
                _cts = null;
            }
        }
    }
}
