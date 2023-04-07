using System.Reactive.Linq;
using System.Reactive.Subjects;

using Microsoft.Extensions.Logging;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// Synchronously debounces the actions in the concurrent queue.
    /// </summary>
    public sealed class DebounceActionService : IDisposable
    {
        #region CONSTRUCTOR
        public DebounceActionService(ILogger<DebounceActionService> logger)
        {
            _logger = logger;
            DebounceSubscribe();
        }
        #endregion

        #region FIELDS
        private readonly ILogger _logger;
        private readonly Subject<Action> _subject = new();
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
        /// <param name="action">
        /// Item to debounce.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// thrown in case <paramref name="action"/>is equal to null.
        /// </exception>
        public void Debounce(Action action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            _subject.OnNext(action);
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
                .Where(x => x.Count > 0)
                .Subscribe(items =>
                {
                    foreach (var item in items.DistinctBy(x => x.GetHashCode()))
                    {
                        try
                        {
                            item.Invoke();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "DebounceActionService: Error while executing the action.");
                        }
                    }
                });
        }
        #endregion
    }
}