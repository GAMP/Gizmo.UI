using System.Collections.Concurrent;

namespace Net.Shared.Queues.WorkQueue;
/// <summary>
///  Asynchronously debounces in the concurrent queue.
/// </summary>
/// <remarks>Disposable.</remarks>
public abstract class DebounceService<T> : IDisposable
{
    #region CONSTRUCTOR
    public DebounceService()
    {
        _queueItems = new();
        Task.Run(ProcessQueueItems);
    }
    public DebounceService(int itemsCount)
    {
        _queueItems = new(itemsCount);
        Task.Run(ProcessQueueItems);
    }
    #endregion

    #region PRIVATE FIELDS
    private record WorkQueueItem(T Item, CancellationToken CancelationToken, TaskCompletionSource TaskCompletionSource);
    private readonly BlockingCollection<WorkQueueItem> _queueItems;
    #endregion

    #region PROPERTIES
    public int DebounceBufferTime { get; set; } = 1000; // 1 sec by default
    #endregion

    #region PUBLIC FUNCTIONS
    public Task Debounce(T item, CancellationToken cToken = default)
    {
        TaskCompletionSource tcs = new();

        return _queueItems.TryAdd(new(item, cToken, tcs))
            ? tcs.Task
            : Task.CompletedTask;
    }

    public void Dispose() => _queueItems.Dispose();
    #endregion

    #region PRIVATE FUNCTIONS
    private async Task ProcessQueueItems()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(DebounceBufferTime));

        foreach (var queueItem in _queueItems.GetConsumingEnumerable())
        {
            try
            {
                do
                {

                } while (await timer.WaitForNextTickAsync(queueItem.CancelationToken));

                await OnDebounce(queueItem.Item, queueItem.CancelationToken);
                queueItem.TaskCompletionSource.SetResult();
            }
            catch (Exception exeption)
            {
                queueItem.TaskCompletionSource.SetException(exeption);
            }
        }
    }
    #endregion

    #region ABSTRACT FUNCTIONS
    public abstract Task OnDebounce(T item, CancellationToken cToken = default);
    #endregion
}
