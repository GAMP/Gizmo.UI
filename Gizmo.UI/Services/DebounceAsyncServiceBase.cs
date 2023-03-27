using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Gizmo.UI.Services;
/// <summary>
///  Asynchronously debounces in the concurrent queue.
/// </summary>
/// <remarks>Disposable.</remarks>
public abstract class DebounceAsyncServiceBase<T> : IDisposable
{

    #region CONSTRUCTOR
    protected DebounceAsyncServiceBase(ILogger logger) => Logger = logger;
    #endregion

    #region PRIVATE FIELDS
    private record DebounceItem(T Item, CancellationToken CancelationToken);
    private readonly ConcurrentDictionary<object, DebounceItem> _items = new();
    Timer? _timer;
    private readonly Random _random = new();
    #endregion

    #region PROPERTIES
    protected ILogger Logger { get; }
    public int DebounceBufferTime { get; set; } = 5000; // 1 sec by default
    #endregion

    #region PUBLIC FUNCTIONS
    public void Debounce(T item, CancellationToken cToken = default)
    {
        _timer ??= new Timer(GetTimerCallback(), null, DebounceBufferTime, Timeout.Infinite);

        var key = GetKey(item);
        var value = new DebounceItem(item, cToken);

        _items.AddOrUpdate(key, value, (_, __) => value);

        Console.WriteLine($"Debounce item {key} was added or updated to the dictionary.");
    }

    public void Dispose() => _timer?.Dispose();
    #endregion

    #region PRIVATE FUNCTIONS
    private TimerCallback GetTimerCallback() => async _ =>
    {
        await Task.Run(async () =>
        {
            Console.WriteLine("Debounce processing timer callback is starting.");

            await ProcessItems();

            _timer?.Dispose();
            _timer = null;

            Console.WriteLine("Debounce processing timer callback was completed.");
        });
    };
    private async Task ProcessItems()
    {
        List<Task> tasks = new(_items.Count);

        foreach (var element in _items)
        {
            if (_items.TryRemove(element.Key, out _))
            {
                if (_random.Next(0, 100) > 50)
                    element.Value.CancelationToken.ThrowIfCancellationRequested();

                if (element.Value.CancelationToken.IsCancellationRequested)
                {
                    Logger.LogWarning("Debounce processing item {0} was cancelled.", element.Key);
                    continue;
                }

                tasks.Add(Task.Run(() => OnDebounce(element.Value.Item, element.Value.CancelationToken)));

                Console.WriteLine($"Debounce processing item {element.Key} was added to the tasks.");
            }
            else
            {
                Console.WriteLine($"Debounce process of the item {element.Key} was not removed.");
            }
        }

        Console.WriteLine("Debounce processing tasks of the items were starting.");

        await Task.WhenAll(tasks).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Logger.LogError(task.Exception, "Debounce processing tasks of the items were failed.");
            }
            else
            {
                Console.WriteLine("Debounce processing tasks of the items were completed.");
            }
        });
    }
    #endregion

    #region ABSTRACT FUNCTIONS
    public abstract Task OnDebounce(T item, CancellationToken cToken = default);
    public abstract object GetKey(T item);
    #endregion
}
