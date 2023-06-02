using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// Notification service base.
    /// </summary>
    public abstract class NotificationsServiceBase : INotificationsService
    {
        #region CONSTRCUTOR
        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="logger">Logger.</param>
        public NotificationsServiceBase(INotificationsHost notificationsHost, IServiceProvider serviceProvider, ILogger logger)
        {
            _notificationsHost = notificationsHost;
            _serviceProvider = serviceProvider;
            _logger = logger;

            //WARNING injecting GlobalCancellationService will fail due to the way we register it
            _globalCancellationService = _serviceProvider.GetRequiredService<GlobalCancellationService>();
        }
        #endregion

        #region FIELDS

        public event EventHandler<NotificationsChangedArgs>? NotificationsChanged;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly GlobalCancellationService _globalCancellationService;
        private readonly ConcurrentDictionary<int, NState> _notificationStates = new();
        private readonly INotificationsHost _notificationsHost;
        private int _dialogIdentifierCounter = 0;

        #endregion

        #region LOCAL

        private class NState
        {
            public NState(INotificationController notificationController, NotificationAddOptions addOptions)
            {
                Controller = notificationController;
                AddOptions = addOptions;
            }

            public NotificationAddOptions AddOptions
            {
                get;
            }

            /// <summary>
            /// Gets notification creation time.
            /// </summary>
            public DateTime CreationTime
            {
                get; init;
            } = DateTime.UtcNow;

            /// <summary>
            /// Gets state.
            /// </summary>
            public NotificationState State
            {
                get; set;
            } = NotificationState.Showing;

            /// <summary>
            /// Gets controller.
            /// </summary>
            public INotificationController Controller { get; init; }
        }

        #endregion

        public Task ShowAsync()
        {
            return _notificationsHost.ShowAsync();
        }

        public virtual Task<AddNotificationResult<TResult>> ShowNotificationAsync<TComponent, TResult>(IDictionary<string, object> parameters,
            NotificationDisplayOptions? displayOptions = null,
            NotificationAddOptions? addOptions = null,
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
                return Task.FromResult(new AddNotificationResult<TResult>(dialogResult, default));

            //create new notification identifier, right now we use int, this could be a string or any other key value.
            //this will give a dialog an unique id that we can capture in anonymous functions
            var notificationIdentifier = Interlocked.Add(ref _dialogIdentifierCounter, 1);

            //create completion source
            var completionSource = new TaskCompletionSource<TResult>();

            //cancel callback handler
            var cancelCallback = () =>
            {
                TryDismiss(notificationIdentifier);
                _logger.LogTrace("Cancelling dialog ({dialogId}).", notificationIdentifier);
                completionSource.TrySetCanceled();
            };

            //result callback handler
            var resultCallback = (TResult result) =>
            {
                TryAcknowledge(notificationIdentifier);
                _logger.LogTrace("Setting dialog ({dialogId}) result, result {result}.", notificationIdentifier, result);
                completionSource.TrySetResult(result);
            };

            //user provider token cancellation handler
            cancellationToken.Register(() =>
            {
                cancelCallback();
            });

            //create and add cancel event callback
            EventCallback cancelEventCallback = EventCallback.Factory.Create(this, cancelCallback);
            parameters.TryAdd("CancelCallback", cancelEventCallback);

            //create and add result event callback
            EventCallback<TResult> resultEventCallabck = EventCallback.Factory.Create(this, resultCallback);
            parameters.TryAdd("ResultCallback", resultEventCallabck);

            //add display options
            parameters.TryAdd("DisplayOptions", displayOptions);

            //create dialog controller and pass the parameters
            var state = _notificationStates.GetOrAdd(notificationIdentifier, (id) =>
            {
                //create controller
                var controller = new NotificationController<TComponent, TResult>(displayOptions, parameters)
                {
                    CancelCallback = cancelEventCallback,
                    ResultCallback = resultEventCallabck,
                    Identifier = notificationIdentifier
                };

                //create state
                return new NState(controller, addOptions);
            });

            //notify of change
            NotificationsChanged?.Invoke(this, new NotificationsChangedArgs() { NotificationId = notificationIdentifier });

            //return dialog result
            var result = new AddNotificationResult<TResult>(dialogResult, completionSource)
            {
                Controller = state.Controller,
            };

            _= _notificationsHost.ShowAsync();

            return Task.FromResult(result);
        }

        public virtual Task<AddNotificationResult<EmptyComponentResult>> ShowNotificationAsync<TComponent>(IDictionary<string, object> parameters,
           NotificationDisplayOptions? displayOptions = null,
           NotificationAddOptions? addOptions = null,
           CancellationToken cancellationToken = default) where TComponent : ComponentBase, new()
        {
            return ShowNotificationAsync<TComponent, EmptyComponentResult>(parameters, displayOptions, addOptions, cancellationToken);
        }

        /// <summary>
        /// Dismisses all notifications.
        /// </summary>
        public void TryDismissAll()
        {
            foreach (var state in _notificationStates)
            {
                if (!TryDismiss(state.Key))
                {
                    //log
                }
            }
        }

        public bool TryDismiss(int notificationId)
        {
            if (!_notificationStates.TryGetValue(notificationId, out var state))
                return false;

            if (state.State == NotificationState.Showing && !state.AddOptions.NotificationAckOptions.HasFlag(NotificationAckOptions.Dismiss))
            {
                state.State = NotificationState.Dismissed;
                //notify
                NotificationsChanged?.Invoke(this, new NotificationsChangedArgs() { NotificationId = notificationId });
            }
            else
            {
                TryAcknowledge(notificationId);
            }          

            return true;
        }

        public bool TryAcknowledge(int notificationId)
        {
            if (!_notificationStates.TryRemove(notificationId, out var state))
                return false;

            NotificationsChanged?.Invoke(this, new NotificationsChangedArgs() { NotificationId = notificationId });

            //get visible notifications count
            var currentCount = _notificationStates.Where(x => x.Value.State == NotificationState.Showing).Count();

            if (currentCount <= 0)
            {
                _ = _notificationsHost.HideAsyc();
            }

            return true;

        }

        public bool TryTimeOut(int notificationId)
        {
            if (!_notificationStates.TryGetValue(notificationId, out var state))
                return false;

            if (state.State == NotificationState.Showing && !state.AddOptions.NotificationAckOptions.HasFlag(NotificationAckOptions.TimeOut))
            {
                state.State = NotificationState.TimedOut;
            }
            else
            {
                _notificationStates.TryRemove(notificationId, out state);
            }

            //notify
            NotificationsChanged?.Invoke(this, new NotificationsChangedArgs() { NotificationId = notificationId });

            return true;
        }

        public void TryAcknowledgeAll()
        {
            foreach (var state in _notificationStates)
            {
                if (!TryAcknowledge(state.Key))
                {
                    //log
                }
            }

           
        }

        public IEnumerable<INotificationController> GetVisible()
        {
            return _notificationStates.Where(x=> x.Value.State == NotificationState.Showing).Select(x=>x.Value.Controller).ToList();
        }

        public IEnumerable<INotificationController> GetDismissed()
        {
            return _notificationStates.Where(x => x.Value.State != NotificationState.Showing).Select(x => x.Value.Controller).ToList();
        }
    }
}
