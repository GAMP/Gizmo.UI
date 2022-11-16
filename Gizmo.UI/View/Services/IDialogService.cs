#nullable enable

using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace Gizmo.UI.Services
{
    public interface IDialogService
    {
        IEnumerable<IDynamicComponentDialog> DialogQueue { get; }

        event EventHandler<EventArgs>? DialogChanged;

        Task<ShowDialogResult<TResult>> ShowDialogAsync<TComponent, TResult>(IDictionary<string, object> parameters, DialogDisplayOptions? displayOptions = null, DialogAddOptions? addOptions = null, CancellationToken cancellationToken = default)
            where TComponent : ComponentBase
            where TResult : class;
        Task<ShowDialogResult<EmptyDialogResult>> ShowDialogAsync<TComponent>(IDictionary<string, object> parameters, DialogDisplayOptions? displayOptions = null, DialogAddOptions? addOptions = null, CancellationToken cancellationToken = default) where TComponent : ComponentBase;
        bool TryPeek([MaybeNullWhen(false)] out IDynamicComponentDialog componentDialog);
        bool TryRemove(int dialogId);
    }
}