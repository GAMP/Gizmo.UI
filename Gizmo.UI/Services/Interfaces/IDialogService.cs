using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace Gizmo.UI.Services
{
    public interface IDialogService
    {
        /// <summary>
        /// Raised when dialog queue changes (dialog added or removed).
        /// </summary>
        event EventHandler<EventArgs>? DialogChanged;

        /// <summary>
        /// Shows dialog with custom result.
        /// </summary>
        /// <typeparam name="TComponent">Component type.</typeparam>
        /// <typeparam name="TResult">Custom result type.</typeparam>
        /// <param name="parameters">Dialog parameters.</param>
        /// <param name="displayOptions">Dialog display options.</param>
        /// <param name="addOptions">Dialog addition options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Show dialog result, this result allows us to await for dialog completion or cancellation.</returns>
        Task<ShowDialogResult<TResult>> ShowDialogAsync<TComponent, TResult>(IDictionary<string, object> parameters, DialogDisplayOptions? displayOptions = null, DialogAddOptions? addOptions = null, CancellationToken cancellationToken = default)
            where TComponent : ComponentBase
            where TResult : class, new();

        ///<summary>Shows dialog with empty result.</summary>
        /// <inheritdoc cref="ShowDialogAsync{TComponent, TResult}(IDictionary{string, object}, DialogDisplayOptions?, DialogAddOptions?, CancellationToken)"/>
        Task<ShowDialogResult<EmptyDialogResult>> ShowDialogAsync<TComponent>(IDictionary<string, object> parameters, DialogDisplayOptions? displayOptions = null, DialogAddOptions? addOptions = null, CancellationToken cancellationToken = default) where TComponent : ComponentBase, new();

        /// <summary>
        /// Tries to obtain next dialog to be shown from the queue.
        /// </summary>
        /// <param name="componentDialog">Found dialog.</param>
        /// <returns>True if any dialog found in queue, false if no other dialog remains to be shown.</returns>
        bool TryGetNext([MaybeNullWhen(false)] out IDialogController? componentDialog);
    }
}
