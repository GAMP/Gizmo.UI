using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Gizmo.UI.Services;
/// <summary>
///  Asynchronously debounces in the concurrent queue.
/// </summary>
/// <typeparam name="T">Debounce item type.</typeparam>
/// <remarks>Disposable.</remarks>
public abstract class DebounceAsyncServiceBase<T> : IDisposable
{
    #region CONSTRUCTOR
    protected DebounceAsyncServiceBase(ILogger logger) => Logger = logger;
    #endregion

    #region PRIVATE FIELDS
    Timer? _timer;
    private int _debounceBufferTime = 1000; // 1 sec by default
    private record DebounceItem(T Item, CancellationToken CancelationToken);
    private readonly ConcurrentDictionary<object, DebounceItem> _items = new();
    #endregion

    #region PROPERTIES
    protected ILogger Logger { get; }
    public int DebounceBufferTime
    {
        get { return _debounceBufferTime; }
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(DebounceBufferTime));

            _debounceBufferTime = value;

            //reschedule timer
            _timer ??= new Timer(GetTimerCallback(), null, _debounceBufferTime, Timeout.Infinite);
        }
    }
    #endregion

    #region PUBLIC FUNCTIONS
    public void Debounce(T item, CancellationToken cToken = default)
    {
        _timer ??= new Timer(GetTimerCallback(), null, _debounceBufferTime, Timeout.Infinite);

        var key = GetKey(item);
        var value = new DebounceItem(item, cToken);

        _items.AddOrUpdate(key, value, (_, __) => value);
    }

    public void Dispose() => _timer?.Dispose();
    #endregion

    #region PRIVATE FUNCTIONS
    private TimerCallback GetTimerCallback() => async _ => await Task.Run(async () =>
        {
            await ProcessItems();

            _timer?.Dispose();
            _timer = null;
        });
    private async Task ProcessItems()
    {
        List<Task> tasks = new(_items.Count);

        foreach (var element in _items)
        {
            if (_items.TryRemove(element.Key, out _))
            {
                if (element.Value.CancelationToken.IsCancellationRequested)
                {
                    Logger.LogWarning("Debounce processing item {0} was cancelled.", element.Key);
                    continue;
                }

                tasks.Add(Task.Run(() => OnDebounce(element.Value.Item, element.Value.CancelationToken)));
            }
        }

        await Task.WhenAll(tasks).ContinueWith(task =>
        {
            if (task.IsFaulted)
                Logger.LogError(task.Exception, "Debounce processing tasks of the items were failed.");
        });
    }
    #endregion

    #region ABSTRACT FUNCTIONS
    protected abstract Task OnDebounce(T item, CancellationToken cToken);
    protected abstract object GetKey(T item);
    #endregion
}
