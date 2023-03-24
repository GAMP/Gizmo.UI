using Gizmo.UI.Services;
using Gizmo.UI.View.States;

using Microsoft.Extensions.Logging;

namespace Gizmo.UI.View.Services;

/// <inheritdoc/>
public sealed class ViewStateDebounceService : DebounceAsyncServiceBase<IViewState>
{
    public ViewStateDebounceService(ILogger<ViewStateDebounceService> logger) : base(logger)
    {
    }

    public override object GetKey(IViewState item) => item.GetHashCode();

    public override Task OnDebounce(IViewState item, CancellationToken cToken = default)
    {
        item.RaiseChanged();
        Console.WriteLine($"Debounce item {item.GetHashCode()} was changed.");
        return Task.CompletedTask;
    }
}
