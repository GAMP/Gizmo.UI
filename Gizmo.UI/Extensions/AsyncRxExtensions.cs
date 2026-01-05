namespace System.Reactive.Linq
{
    public static class AsyncRxExtensions
    {
        public static IObservable<T> AsyncWhereSequential<T>(this IObservable<T> source, Func<T, CancellationToken, Task<bool>> predicateAsync)
        {
            return source
                .Select(x =>
                    Observable.FromAsync(ct => predicateAsync(x, ct))
                              .Where(pass => pass)     // keep only passed
                              .Select(_ => x))         // project back the original item
                .Concat();                             // process sequentially
        }

        public static IObservable<T> AsyncWhereConcurrent<T>(this IObservable<T> source, Func<T, CancellationToken, Task<bool>> predicateAsync, int maxConcurrency = 4)
        {
            return source
                .Select(x =>
                    Observable.FromAsync(ct => predicateAsync(x, ct))
                              .Where(pass => pass)
                              .Select(_ => x))
                .Merge(maxConcurrency); // parallel up to N
        }
    }
}
