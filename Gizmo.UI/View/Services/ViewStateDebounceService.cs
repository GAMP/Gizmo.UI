using Gizmo.UI.Services;
using Gizmo.UI.View.States;

using Microsoft.Extensions.Logging;

namespace Gizmo.UI.View.Services;

/// <inheritdoc/>
public sealed class ViewStateDebounceService : DebounceAsyncServiceBase<IViewState>
{
    private readonly Random _random = new();
    public ViewStateDebounceService(ILogger<ViewStateDebounceService> logger) : base(logger)
    {
    }

    public override object GetKey(IViewState item) => item.GetHashCode();

    public override async Task OnDebounce(IViewState item, CancellationToken cToken = default)
    {
        await Task.Delay(_random.Next(1000, 5000), cToken);
        var isException = _random.Next(0, 100) > 50;

        if (isException)
            throw new NotImplementedException($"This is a test exception for the {item.GetHashCode()}.");

        item.RaiseChanged();
        Console.WriteLine($"Debounce item {item.GetHashCode()} method was called.");
    }
}
