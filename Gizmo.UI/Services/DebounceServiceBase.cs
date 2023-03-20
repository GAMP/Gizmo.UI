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

    private readonly Subject<T> _subject = new();
    private int _debounceBufferTime = 1000; // 1 sec by default
    private IDisposable? _subscription;

    #endregion

    #region PROPERTIES

    protected ILogger Logger { get; }

    protected int DebounceBufferTime
    {
        get { return _debounceBufferTime; }
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(DebounceBufferTime));

            //update current value
            _debounceBufferTime = value;

            //resubscribe
            DebounceSubscribe();
        }
    }

    #endregion

    #region PUBLIC FUNCTIONS

    public void Debounce(T item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        _subject.OnNext(item);
    }

    public void Dispose()
    {
        _subject?.Dispose();
        _subscription?.Dispose();
    }

    #endregion

    #region PRIVATE FUNCTIONS

    private void DebounceSubscribe()
    {
        //dispose any existing subscriptions
        _subscription?.Dispose();

        //resubscribe
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

    protected abstract void OnDebounce(T item);

    #endregion
}
