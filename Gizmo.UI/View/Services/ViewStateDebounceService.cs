using Gizmo.UI.Services;
using Gizmo.UI.View.States;

using Microsoft.Extensions.Logging;

namespace Gizmo.UI.View.Services;

/// <inheritdoc/>
public sealed class ViewStateDebounceService : DebounceServiceBase<IViewState>
{
    public ViewStateDebounceService(ILogger<ViewStateDebounceService> logger) : base(logger)
    {
    }

    protected override void OnDebounce(IViewState item) => item.RaiseChanged();

    //for asyn debounce

    // protected override object GetKey(IViewState item) => item.GetHashCode();

    // protected override Task OnDebounce(IViewState item, CancellationToken cToken = default)
    // {
    //     item.RaiseChanged();
    //     return Task.CompletedTask;
    // }
}
