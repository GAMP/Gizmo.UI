using System.Reactive.Linq;
using System.Reactive.Subjects;

using Microsoft.Extensions.Logging;

namespace Gizmo.UI.Services;

/// <summary>
/// Gneric debouncig service base.
/// </summary>
/// <typeparam name="T">Debounce item type.</typeparam>
public abstract class DebounceServiceBase<T> : IDisposable
{
    #region CONSTRUCTOR
    protected DebounceServiceBase(ILogger logger) => Logger = logger;

    #endregion

    #region FIELDS

    private bool _isInitialized;
    private Timer? _timer;
    private Subject<T>? _subject;
    private IDisposable? _subscription;

    private int _debounceBufferTime = 1000; // 1 sec by default
    private int _debounceLifeTime = 60 * 1000 * 10; // 10 min by default debounce lifetime

    #endregion

    #region PROPERTIES

    protected ILogger Logger { get; }

    /// <summary>
    /// Debounce buffertime.
    /// </summary>
    /// <value></value>
    public int DebounceBufferTime
    {
        get { return _debounceBufferTime; }
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(DebounceBufferTime));

            //update current value
            if (value >= DebounceLifeTime)
                _debounceBufferTime = DebounceLifeTime - 100;
            else
                _debounceBufferTime = value;

            //resubscribe
            Dispose();
            DebounceSubscribe();
        }
    }
    /// <summary>
    /// Debounce lifetime.
    /// </summary>
    /// <value></value>
    public int DebounceLifeTime
    {
        get { return _debounceLifeTime; }
        set
        {
            if (value <= _debounceBufferTime)
                _debounceLifeTime = _debounceBufferTime + 100;
            _debounceLifeTime = value;
        }
    }

    #endregion

    #region PUBLIC FUNCTIONS

    public void Debounce(T item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        if (!_isInitialized)
            DebounceSubscribe();

        _subject!.OnNext(item);
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _subject?.Dispose();
        _subscription?.Dispose();

        _timer = null;
        _subject = null;
        _subscription = null;
    }

    #endregion

    #region PRIVATE FUNCTIONS

    private void DebounceSubscribe()
    {
        _subject = new();

        // The debounce action
        _subscription = _subject!
            .Buffer(TimeSpan.FromMilliseconds(_debounceBufferTime))
            .Where(items => items.Count > 0)
            .Distinct()
            .Subscribe(items =>
            {
                for (int i = 0; i < items.Count; i++)
                {
                    try
                    {
                        OnDebounce(items[i]);
                    }
                    catch (Exception exception)
                    {
                        //the handlers are outside of our code so we should handle the exception and log it
                        Logger.LogError(exception, "Error in view state change debounce handler.");
                    }
                }
            });

        // The debounce lifetime
        _timer = new Timer((object? _) =>
        {
            Dispose();
            _isInitialized = false;
        }, null, TimeSpan.FromMinutes(_debounceLifeTime), TimeSpan.FromMilliseconds(-1));

        _isInitialized = true;
    }
    #endregion

    #region ABSTRACT FUNCTIONS

    protected abstract void OnDebounce(T item);

    #endregion
}
