using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Gizmo.UI.Services
{
    [Register()]
    public sealed class DialogService
    {
        #region CONSTRCUTOR
        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="logger">Logger.</param>
        public DialogService(IServiceProvider serviceProvider, ILogger<DialogService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        #endregion

        #region FIELDS
        public event EventHandler<EventArgs>? DialogChanged;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly ConcurrentQueue<IDynamicComponentDialog> _dialogQueue = new();
        private readonly ConcurrentDictionary<int, IDynamicComponentDialog> _dialogLookup = new();
        private int _dialogIdentifierCounter = 0;
        #endregion

        #region PROPERTIES

        /// <summary>
        /// Get enumerable dialog queue.
        /// </summary>
        public IEnumerable<IDynamicComponentDialog> DialogQueue
        {
            get { return _dialogQueue; }
        }

        #endregion

        #region FUNCTIONS

        public Task<ShowDialogResult<TResult>> ShowDialogAsync<TComponent, TResult>(IDictionary<string, object> parameters,
            DialogDisplayOptions? displayOptions = null,
            DialogAddOptions? addOptions = null,
            CancellationToken cancellationToken = default) where TComponent : ComponentBase where TResult : class
        {
            //create default display options if none provided
            displayOptions ??= new();
            //create default add options if none provided
            addOptions ??= new();

            // 1) check parameters
            // 2) confirm that based on parameters dialog can be added

            //check if dialog can be added
            DialogAddResult dialogAddResult = DialogAddResult.Success;

            //if not return the result with null task completion source (Task.CompletedTask), this will make any await calls to complete instantly
            if (dialogAddResult != DialogAddResult.Success)
                return Task.FromResult(new ShowDialogResult<TResult>(default) { Result = dialogAddResult });

            //create new dialog identifier, right now we use int, this could be a string or any other key value.
            //this will give a dialog an unique id that we can capture in anonymous functions
            var dialogIdentifier = Interlocked.Add(ref _dialogIdentifierCounter, 1);

            //create completion source
            var completionSource = new TaskCompletionSource<TResult>();

            //cancel callback handler
            var cancelCallback = () =>
            {
                _logger.LogTrace("Cancelling dialog ({dialogId}).", dialogIdentifier);
                completionSource.TrySetCanceled();
                TryRemove(dialogIdentifier);
            };

            //result callback handler
            var resultCallback = (TResult result) =>
            {
                _logger.LogTrace("Setting dialog ({dialogId}) result, result {result}.", dialogIdentifier, result);
                completionSource.TrySetResult(result);
                TryRemove(dialogIdentifier);
            };

            //user provider token cancellation handler
            cancellationToken.Register(() =>
            {
                cancelCallback();
            });

            //create cancel event callback
            EventCallback cancelEventCallback = EventCallback.Factory.Create(this, cancelCallback);
            //add cancel callback
            parameters.Add("CancelCallback", cancelEventCallback);

            //create result event callback
            EventCallback<TResult>? resultEventCallabck = default;
            //only add result callback parameter if component provides non empty result
            if (typeof(TResult) != typeof(EmptyDialogResult))
            {
                resultEventCallabck = EventCallback.Factory.Create(this, resultCallback);
                parameters.Add("ResultCallback", resultEventCallabck);
            }

            //create dialog and pass the parameters
            var dialog = _dialogLookup.GetOrAdd(dialogIdentifier, (id) => new DynamicComponentDialog<TComponent, TResult>(displayOptions, parameters)
            {
                CancelCallback = cancelEventCallback,
                ResultCallback = resultEventCallabck,
            });

            //add dialog to the queue
            _dialogQueue.Enqueue(dialog);

            //notify of change
            DialogChanged?.Invoke(this, EventArgs.Empty);

            //return dialog result
            var result = new ShowDialogResult<TResult>(completionSource)
            {
                Result = dialogAddResult
            };

            return Task.FromResult(result);
        }

        public Task<ShowDialogResult<EmptyDialogResult>> ShowDialogAsync<TComponent>(IDictionary<string, object> parameters,
            DialogDisplayOptions? displayOptions = null,
            DialogAddOptions? addOptions = null,
            CancellationToken cancellationToken = default) where TComponent : ComponentBase
        {
            return ShowDialogAsync<TComponent, EmptyDialogResult>(parameters, displayOptions, addOptions, cancellationToken);
        }

        public bool TryRemove(int dialogId)
        {
            //try to obtain dialog from lookup
            if (!_dialogLookup.TryRemove(dialogId, out var dialog))
                return false;

            //try to remove dialog from queue
            return TryRemove(dialog);
        }

        public bool TryPeek([MaybeNullWhen(false)] out IDynamicComponentDialog componentDialog)
        {
            return _dialogQueue.TryPeek(out componentDialog);
        }

        private bool TryRemove(IDynamicComponentDialog componentDialog)
        {
            if (componentDialog == null)
                throw new ArgumentException(nameof(componentDialog));

            if (_dialogQueue.TryDequeue(out _))
            {
                DialogChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }

        #endregion        
    }
}
