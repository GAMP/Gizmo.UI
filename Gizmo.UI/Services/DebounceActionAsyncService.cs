using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Gizmo.UI.Services
{
    /// <summary>
    ///  Asynchronously debounces the action without params in the concurrent queue.
    /// </summary>
    /// <remarks>Disposable.</remarks>
    public sealed class DebounceActionAsyncService : IDisposable
    {
        #region CONSTRUCTOR
        public DebounceActionAsyncService(ILogger<DebounceActionAsyncService> logger)
        {
            _logger = logger;
            DebounceSubscribe();
        }
        #endregion

        #region FIELDS
        private readonly ILogger _logger;
        private readonly Subject<(Func<CancellationToken, Task> Action, CancellationToken CToken)> _subject = new();
        private IDisposable? _subscription;
        private int _debounceBufferTime = 1000; // 1 sec by default
        #endregion

        #region PROPERTIES

        /// <summary>
        /// Debounce buffertime.
        /// </summary>
        public int DebounceBufferTime
        {
            get { return _debounceBufferTime; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(DebounceBufferTime));

                _debounceBufferTime = value;

                //resubscribe
                _subscription?.Dispose();
                DebounceSubscribe();
            }
        }

        #endregion

        #region PUBLIC FUNCTIONS

        /// <summary>
        /// Debounces the data.
        /// </summary>
        /// <param name="action">Item to debounce.</param>
        /// <param name="cToken">Cancellation token.</param>
        /// <exception cref="ArgumentNullException">thrown in case <paramref name="action"/>is equal to null.</exception>
        public void Debounce(Func<CancellationToken, Task> action, CancellationToken cToken = default)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            _subject.OnNext((action, cToken));
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            _subject?.Dispose();
            _subscription?.Dispose();
        }

        #endregion

        #region PRIVATE FUNCTIONS
        private void DebounceSubscribe()
        {
            // The debounce action
            _subscription = _subject
                .Buffer(TimeSpan.FromMilliseconds(_debounceBufferTime))
                .Distinct(task => task.GetHashCode())
                .Where(batch => batch.Count > 0)               
                .SelectMany(batch => ProcessBatchAsync(batch).ToObservable())
                .Subscribe();
        }
        #endregion

        private async Task ProcessBatchAsync(IList<(Func<CancellationToken,Task> Action, CancellationToken CancellationToken)> batch)
        {
            foreach (var task in batch)
            {
                try
                {
                    await task.Action(task.CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // we don't need to log cancellation errors
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DebounceActionAsyncService: Debounce action faulted.");
                }
            }
        }
    }
}
