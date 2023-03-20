using Gizmo.UI.Services;
using Gizmo.UI.View.States;

using Microsoft.Extensions.Logging;

namespace Gizmo.UI.View.Services;

public sealed class ViewStateDebounceService : DebounceServiceBase<IViewState>
{
    public ViewStateDebounceService(ILogger<ViewStateDebounceService> logger) : base(logger)
    {
    }

    protected override void OnDebounce(IViewState item) => item.RaiseChanged();
}
