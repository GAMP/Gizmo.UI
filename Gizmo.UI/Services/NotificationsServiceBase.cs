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
        public NotificationsServiceBase(IServiceProvider serviceProvider, ILogger logger)
        {
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
        private int _dialogIdentifierCounter = 0;

        #endregion

        #region LOCAL

        private class NState : IDisposable
        {
            public NState(INotificationController notificationController, 
                NotificationAddOptions addOptions, 
                Action cancel)
            {
                Controller = notificationController;
                AddOptions = addOptions;
                CancelDelegate = cancel;
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

            /// <summary>
            /// Optional timeout timer.
            /// </summary>
            public Timer? Timer { get; set; }

            public Action CancelDelegate { get;  }

            public void Dispose()
            {
                Timer?.Dispose();
            }
        }

        #endregion

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
                return Task.FromResult(new AddNotificationResult<TResult>(dialogResult,default, default));

            //create new notification identifier, right now we use int, this could be a string or any other key value.
            //this will give a dialog an unique id that we can capture in anonymous functions
            var notificationIdentifier = Interlocked.Add(ref _dialogIdentifierCounter, 1);

            //create completion source
            var completionSource = new TaskCompletionSource<TResult>();

            //cancel callback handler
            var cancelCallback = () =>
            {
                TryDismiss(notificationIdentifier);
                completionSource.TrySetException(IComponentController.DismissedException);
            };

            //result callback handler
            var resultCallback = (TResult result) =>
            {
                TryAcknowledge(notificationIdentifier);
                completionSource.TrySetResult(result);
            };

            //error callback
            var errorCallback = (Exception error) =>
            {
                TryAcknowledge(notificationIdentifier);
                completionSource.TrySetException(error);
            };

            //suspend timeout callback
            var suspendTimeoutCallback = (bool suspend) =>
            {
                TrySuspendTimeout(notificationIdentifier, suspend);
            };

            //user provider token cancellation handler
            cancellationToken.Register(() =>
            {
                TryDismiss(notificationIdentifier);
                completionSource.TrySetCanceled();
            });

            //create dialog controller and pass the parameters
            var state = _notificationStates.GetOrAdd(notificationIdentifier, (id) =>
            {
                var controller = new NotificationController<TComponent, TResult>(notificationIdentifier, displayOptions, parameters);
                controller.CreateCallbacks(resultCallback, errorCallback, cancelCallback, suspendTimeoutCallback, parameters);
                return new NState(controller, addOptions,cancelCallback);
            });

            //check if timeout is not null and greater than zero
            //negative value means infinite timeout
            if (addOptions.Timeout > 0)
            {
                state.Timer = new Timer(OnTimerCallback, state, TimeSpan.FromSeconds(addOptions.Timeout.Value), Timeout.InfiniteTimeSpan);
            }

            //notify of change
            NotificationsChanged?.Invoke(this, new NotificationsChangedArgs() { NotificationId = notificationIdentifier });

            //return dialog result
            var result = new AddNotificationResult<TResult>(dialogResult, state.Controller, completionSource);
            return Task.FromResult(result);
        }

        public virtual Task<AddNotificationResult<EmptyComponentResult>> ShowNotificationAsync<TComponent>(IDictionary<string, object> parameters,
           NotificationDisplayOptions? displayOptions = null,
           NotificationAddOptions? addOptions = null,
           CancellationToken cancellationToken = default) where TComponent : ComponentBase, new()
        {
            return ShowNotificationAsync<TComponent, EmptyComponentResult>(parameters, displayOptions, addOptions, cancellationToken);
        }

        private async void OnTimerCallback(object? state)
        {
            if (state is NState nState)
            {
                nState.Timer?.Dispose();
                await nState.Controller.TimeOutResultAsync();
                TryTimeOut(nState.Controller.Identifier);
            }
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

            //dispose state
            state.Dispose();

            NotificationsChanged?.Invoke(this, new NotificationsChangedArgs() { NotificationId = notificationId });

            return true;
        }

        public bool TryTimeOut(int notificationId)
        {
            if (!_notificationStates.TryGetValue(notificationId, out var state))
                return false;

            if (state.State == NotificationState.Showing && !state.AddOptions.NotificationAckOptions.HasFlag(NotificationAckOptions.TimeOut))
            {
                state.State = NotificationState.TimedOut;
                //notify
                NotificationsChanged?.Invoke(this, new NotificationsChangedArgs() { NotificationId = notificationId });
            }
            else
            {
                TryAcknowledge(notificationId);
            }

            return true;
        }

        public bool TrySuspendTimeout(int notificationId, bool suspend)
        {
            if (!_notificationStates.TryGetValue(notificationId, out var state))
                return false;

            if (state.Timer == null)
                return false;

            if (!suspend)
            {
                if (state.AddOptions.Timeout > 0)
                    state.Timer.Change(TimeSpan.FromSeconds(state.AddOptions.Timeout.Value), Timeout.InfiniteTimeSpan);
            }
            else
            {
                state.Timer.Change(Timeout.Infinite, Timeout.Infinite);
            }


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
            return _notificationStates.Where(x => x.Value.State == NotificationState.Showing)
                .Select(x => x.Value.Controller)
                .ToList();
        }

        public IEnumerable<INotificationController> GetDismissed()
        {
            return _notificationStates.Where(x => x.Value.State != NotificationState.Showing)
                .Select(x => x.Value.Controller)
                .ToList();
        }
    }
}
