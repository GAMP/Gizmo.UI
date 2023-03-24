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
    #endregion

    #region PROPERTIES
    protected ILogger Logger { get; }
    public int DebounceBufferTime { get; set; } = 10000; // 1 sec by default
    #endregion

    #region PUBLIC FUNCTIONS
    public void Debounce(T item, CancellationToken cToken = default)
    {
        _timer ??= new Timer(async _ =>
            {
                await Task.Run(async () =>
                {
                    Console.WriteLine("Debounce...");
                    await ProcessItems();

                    _timer?.Dispose();
                    _timer = null;
                    Console.WriteLine("Debounce complete.");
                });
            }, null, DebounceBufferTime, Timeout.Infinite);

        var key = GetKey(item);
        var value = new DebounceItem(item, cToken);

        _items.AddOrUpdate(key, value, (_, __) => value);

        Console.WriteLine($"Debounce item {key} was added or updated");
    }

    public void Dispose() => _timer?.Dispose();
    #endregion

    #region PRIVATE FUNCTIONS
    private async Task ProcessItems()
    {
        foreach (var element in _items)
        {
            Console.WriteLine($"Debounce processing item {element.Key}");

            if (_items.TryRemove(element.Key, out var value))
            {
                if (value.CancelationToken.IsCancellationRequested)
                    continue;

                try
                {
                    await OnDebounce(value.Item, value.CancelationToken);

                    Console.WriteLine($"Debounce processed item {element.Key}");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error while debounce processing item {element.Key}.");
                }
            }
        }
    }
    #endregion

    #region ABSTRACT FUNCTIONS
    public abstract Task OnDebounce(T item, CancellationToken cToken = default);
    public abstract object GetKey(T item);
    #endregion
}
