using System.Reactive.Linq;
using System.Reactive.Subjects;

using Microsoft.Extensions.Logging;

namespace Gizmo.UI.Services;

/// <summary>
///  Asynchronously debounces the actions with params in the concurrent queue.
/// </summary>
/// <typeparam name="T">Debounce item type.</typeparam>
/// <remarks>Disposable.</remarks>
public sealed class DebounceActionAsyncWithParamService : IDisposable
{
    #region CONSTRUCTOR
    public DebounceActionAsyncWithParamService(ILogger<DebounceActionAsyncWithParamService> logger)
    {
        _logger = logger;
        DebounceSubscribe();
    }
    #endregion

    #region FIELDS
    private ILogger _logger;
    private readonly Subject<(Func<object[], CancellationToken, Task> Action, CancellationToken CToken, object[] Params)> _subject = new();
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
    /// /// <param name="cToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">thrown in case <paramref name="action"/>is equal to null.</exception>
    public void Debounce(Func<object[], CancellationToken, Task> action, CancellationToken cToken = default, params object[] funcParams)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        _subject.OnNext((action, cToken, funcParams));
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
                    _ = Task.Run(() => item.Action(item.Params, item.CToken))
                            .ContinueWith(task =>
                            {
                                if (task.IsFaulted)
                                    _logger.LogError(task.Exception, "Error in view state change debounce handler.");
                            })
                            .ConfigureAwait(false);
                }
            });
    }
    #endregion
}
