using System.Reactive.Linq;
using System.Reactive.Subjects;

using Microsoft.Extensions.Logging;

namespace Gizmo.UI.Services;

/// <summary>
///  Asynchronously debounces in the concurrent queue.
/// </summary>
/// <typeparam name="T">Debounce item type.</typeparam>
/// <remarks>Disposable.</remarks>
public abstract class DebounceAsyncServiceBase2<T> : IDisposable
{
    #region CONSTRUCTOR
    protected DebounceAsyncServiceBase2(ILogger logger)
    {
        Logger = logger;
        DebounceSubscribe();
    }

    #endregion

    #region FIELDS
    private readonly Subject<(T item, CancellationToken cToken)> _subject = new();
    private IDisposable? _subscription;
    private int _debounceBufferTime = 1000; // 1 sec by default
    #endregion

    #region PROPERTIES

    protected ILogger Logger { get; }

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
    /// <param name="item">Item to debounce.</param>
    /// <param name="cToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">thrown in case <paramref name="item"/>is equal to null.</exception>
    public void Debounce(T item, CancellationToken cToken = default)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        _subject.OnNext((item, cToken));
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
            .Where(items => items.Count > 0)
            .Distinct()
            .Subscribe(items =>
            {
                foreach (var (item, cToken) in items)
                {
                    _ = Task.Run(() => OnDebounce(item, cToken))
                        .ContinueWith(task =>
                            {
                                if (task.IsFaulted)
                                    Logger.LogError(task.Exception, "Error in view state change debounce handler.");
                            }, cToken)
                        .ConfigureAwait(false);
                }
            });
    }
    #endregion

    #region ABSTRACT FUNCTIONS
    /// <summary>
    /// Called when debounce is triggered.
    /// </summary>
    /// <param name="item">Item to debounce.</param>
    /// <param name="cToken">Cancellation token.</param>
    protected abstract Task OnDebounce(T item, CancellationToken cToken);
    #endregion
}
