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
    protected DebounceServiceBase(ILogger logger)
    {
        Logger = logger;
        DebounceSubscribe();
    }

    #endregion

    #region FIELDS
    private readonly Subject<T> _subject = new();
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
    /// <exception cref="ArgumentNullException">thrown in case <paramref name="item"/>is equal to null.</exception>
    public void Debounce(T item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        _subject.OnNext(item);
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
    }
    #endregion

    #region ABSTRACT FUNCTIONS
    /// <summary>
    /// Called when debounce is triggered.
    /// </summary>
    /// <param name="item">Item to debounce.</param>
    protected abstract void OnDebounce(T item);
    #endregion
}
