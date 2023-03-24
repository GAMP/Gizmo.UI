using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Net.Shared.Queues.WorkQueue;
/// <summary>
///  Asynchronously debounces in the concurrent queue.
/// </summary>
/// <remarks>Disposable.</remarks>
public abstract class DebounceService<T> : IDisposable
{
    #region CONSTRUCTOR
    protected DebounceService(ILogger logger) => Logger = logger;
    #endregion

    #region PRIVATE FIELDS
    private record DebounceItem(T Item, CancellationToken CancelationToken);
    private readonly ConcurrentDictionary<object, DebounceItem> _items = new();
    Timer? _timer;
    #endregion

    #region PROPERTIES
    protected ILogger Logger { get; }
    public int DebounceBufferTime { get; set; } = 1000; // 1 sec by default
    #endregion

    #region PUBLIC FUNCTIONS
    public void Debounce(T item, CancellationToken cToken = default)
    {
        _timer ??= new Timer(async _ =>
            {
                await Task.Run(async () =>
                {
                    await ProcessItems();

                    _timer?.Dispose();
                    _timer = null;
                });
            }, null, DebounceBufferTime, Timeout.Infinite);

        var key = GetKey(item);

        _items.AddOrUpdate(key, new DebounceItem(item, cToken), (k, v) => new DebounceItem(item, cToken));
    }

    public void Dispose() => _timer?.Dispose();
    #endregion

    #region PRIVATE FUNCTIONS
    private async Task ProcessItems()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items.ElementAt(i);

            if (item.Value.CancelationToken.IsCancellationRequested)
            {
                _items.TryRemove(item.Key, out _);
                continue;
            }

            try
            {
                await OnDebounce(item.Value.Item, item.Value.CancelationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while processing queue item.");
            }
            finally
            {
                _items.TryRemove(item.Key, out _);
            }
        }
    }
    #endregion

    #region ABSTRACT FUNCTIONS
    public abstract Task OnDebounce(T item, CancellationToken cToken = default);
    public abstract object GetKey(T item);
    #endregion
}
