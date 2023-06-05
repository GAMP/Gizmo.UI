using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// Dialog service base implementation.
    /// </summary>
    public abstract class DialogServiceBase : IDialogService
    {
        #region CONSTRCUTOR
        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="logger">Logger.</param>
        public DialogServiceBase(IOptionsMonitor<DialogOptions> options,
            IServiceProvider serviceProvider, 
            ILogger logger)
        {
            _options = options;
            _serviceProvider = serviceProvider;
            _logger = logger;

            //WARNING injecting GlobalCancellationService will fail due to the way we register it
            _globalCancellationService = _serviceProvider.GetRequiredService<GlobalCancellationService>();
        }
        #endregion

        #region FIELDS
        public event EventHandler<EventArgs>? DialogChanged;
        private readonly IOptionsMonitor<DialogOptions> _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly ConcurrentQueue<IDialogController> _dialogQueue = new();
        private readonly ConcurrentDictionary<int, IDialogController> _dialogLookup = new();
        private readonly GlobalCancellationService _globalCancellationService;
        private int _dialogIdentifierCounter = 0;
        #endregion

        #region PROPERTIES

        /// <summary>
        /// Get enumerable dialog queue.
        /// </summary>
        public IEnumerable<IDialogController> DialogQueue
        {
            get { return _dialogQueue; }
        }

        #endregion

        #region FUNCTIONS

        public virtual Task<AddDialogResult<TResult>> ShowDialogAsync<TComponent, TResult>(IDictionary<string, object> parameters,
            DialogDisplayOptions? displayOptions = null,
            DialogAddOptions? addOptions = null,
            CancellationToken cancellationToken = default) where TComponent : ComponentBase where TResult : class, new()
        {
            //create linked token, this will allow us to cancel any open dialog
            cancellationToken = _globalCancellationService.GetLinkedCancellationToken(cancellationToken);

            //create default display options if none provided
            displayOptions ??= new();
            //create default add options if none provided
            addOptions ??= new();

            // 1) check parameters
            // 2) confirm that based on parameters dialog can be added

            //check if dialog can be added
            AddComponentResultCode dialogResult = AddComponentResultCode.Opened;

            //if not return the result with null task completion source (Task.CompletedTask), this will make any await calls to complete instantly
            if (dialogResult != AddComponentResultCode.Opened)
                return Task.FromResult(new AddDialogResult<TResult>(dialogResult, default, default));

            //create new dialog identifier, right now we use int, this could be a string or any other key value.
            //this will give a dialog an unique id that we can capture in anonymous functions
            var dialogIdentifier = Interlocked.Add(ref _dialogIdentifierCounter, 1);

            //create completion source
            var completionSource = new TaskCompletionSource<TResult>();

            //result callback handler
            var resultCallback = (TResult result) =>
            {
                _logger.LogTrace("Setting dialog ({dialogId}) result, result {result}.", dialogIdentifier, result);
                completionSource.TrySetResult(result);
                TryRemove(dialogIdentifier);
            };

            //error callback
            var errorCallback = (Exception error) =>
            {               
                _logger.LogTrace("Cancelling dialog ({dialogId}).", dialogIdentifier);
                completionSource.TrySetException(error);
                TryRemove(dialogIdentifier);
            };

            //suspend timeout callback
            var suspendTimeoutCallback = (bool suspend) =>
            {
                //suspend timeout here
            };

            //user provider token cancellation handler
            cancellationToken.Register(() =>
            {
                completionSource.TrySetCanceled();
            });

            //create dialog controller and pass the parameters
            var dialogController = _dialogLookup.GetOrAdd(dialogIdentifier, (id) =>
            {
                var controller = new DialogController<TComponent, TResult>(dialogIdentifier, displayOptions, parameters);
                controller.CreateCallbacks(resultCallback, errorCallback, suspendTimeoutCallback, parameters);
                return controller;
            });             

            //add dialog to the queue
            _dialogQueue.Enqueue(dialogController);

            //notify of change
            DialogChanged?.Invoke(this, EventArgs.Empty);

            //return dialog result
            var result = new AddDialogResult<TResult>(dialogResult, dialogController, completionSource);

            return Task.FromResult(result);
        }

        public virtual Task<AddDialogResult<EmptyComponentResult>> ShowDialogAsync<TComponent>(IDictionary<string, object> parameters,
            DialogDisplayOptions? displayOptions = null,
            DialogAddOptions? addOptions = null,
            CancellationToken cancellationToken = default) where TComponent : ComponentBase, new()
        {
            return ShowDialogAsync<TComponent, EmptyComponentResult>(parameters, displayOptions, addOptions, cancellationToken);
        }

        public virtual Task<AddDialogResult<EmptyComponentResult>> ShowDialogAsync<TComponent, TParameters>(
            TParameters parameters,
            DialogDisplayOptions? displayOptions = null,
            DialogAddOptions? addOptions = null,
            CancellationToken cancellationToken = default)
                where TComponent : ComponentBase, new()
                where TParameters : DialogServiceComponentParameters, new()
        {
            return ShowDialogAsync<TComponent, EmptyComponentResult>(parameters.ToDictionary(), displayOptions, addOptions, cancellationToken);
        }

        public bool TryGetNext([MaybeNullWhen(false)] out IDialogController componentDialog)
        {
            return _dialogQueue.TryPeek(out componentDialog);
        }

        private bool TryRemove(int dialogId)
        {
            //try to obtain dialog from lookup
            if (!_dialogLookup.TryRemove(dialogId, out var dialog))
                return false;

            //try to remove dialog from queue
            return TryRemove(dialog);
        }

        private bool TryRemove(IDialogController componentDialog)
        {
            if (componentDialog == null)
                throw new ArgumentException(null, nameof(componentDialog));

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
