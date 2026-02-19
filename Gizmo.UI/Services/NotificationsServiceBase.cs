using System.Collections.Concurrent;
using System.Drawing;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// Notification service base.
    /// </summary>
    public abstract class NotificationsServiceBase : INotificationsService
    {
        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="logger">Logger.</param>
        public NotificationsServiceBase(IOptionsMonitor<NotificationsOptions> options,
            IServiceProvider serviceProvider,
            ILogger logger)
        {
            _options = options;
            _serviceProvider = serviceProvider;
            _logger = logger;

            //WARNING injecting GlobalCancellationService will fail due to the way we register it
            _globalCancellationService = _serviceProvider.GetRequiredService<GlobalCancellationService>();
        }

        public event EventHandler<NotificationsChangedArgs>? NotificationsChanged;
        public event EventHandler<NotificationHostSizeRequestArgs>? SizeRequest;

        private readonly IOptionsMonitor<NotificationsOptions> _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly GlobalCancellationService _globalCancellationService;
        private readonly ConcurrentDictionary<int, NState> _notificationStates = new();
        private int _dialogIdentifierCounter = 0;
        private const int DEFAULT_NOTIFICATION_TIMEOUT = 5;

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

            //check if timeout value specified
            if (!addOptions.Timeout.HasValue)
            {
                var configuredTimeOutValue = _options.CurrentValue.DefaultTimeout;
                if (configuredTimeOutValue == null)
                {
                    //use default timeout value if none provided
                    addOptions.Timeout = DEFAULT_NOTIFICATION_TIMEOUT;
                }
                else
                {
                    addOptions.Timeout = configuredTimeOutValue;
                }
            }

            // 1) check parameters
            // 2) confirm that based on parameters dialog can be added

            //check if dialog can be added
            AddComponentResultCode dialogResult = AddComponentResultCode.Opened;

            //if not return the result with null task completion source (Task.CompletedTask), this will make any await calls to complete instantly
            if (dialogResult != AddComponentResultCode.Opened)
                return Task.FromResult(new AddNotificationResult<TResult>(dialogResult, default, default));

            //create new notification identifier, right now we use int, this could be a string or any other key value.
            //this will give a dialog an unique id that we can capture in anonymous functions
            var notificationIdentifier = Interlocked.Add(ref _dialogIdentifierCounter, 1);

            //create completion source
            var completionSource = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            //result callback handler
            void resultCallback(TResult result)
            {
                TryAcknowledge(notificationIdentifier);
                completionSource.TrySetResult(result);
            }

            //error callback
            void errorCallback(Exception error)
            {
                if (error == IComponentController.TimeoutException)
                {
                    TryTimeOut(notificationIdentifier);
                }
                else if (error == IComponentController.DismissedException)
                {
                    TryDismiss(notificationIdentifier);
                }
                else
                {
                    TryAcknowledge(notificationIdentifier);
                }

                completionSource.TrySetException(error);
            }

            //suspend timeout callback
            void suspendTimeoutCallback(bool suspend)
            {
                TrySuspendTimeOut(notificationIdentifier, suspend);
            }

            //create dialog controller and pass the parameters
            var state = _notificationStates.GetOrAdd(notificationIdentifier, (id) =>
            {
                var controller = new NotificationController<TComponent, TResult>(notificationIdentifier, displayOptions, parameters);
                controller.CreateCallbacks(resultCallback, errorCallback, suspendTimeoutCallback, notificationIdentifier, parameters);

                var state = new NState(notificationIdentifier, controller, addOptions)
                {
                    //user provider token cancellation handler
                    CancellationTokenRegistration = cancellationToken.Register(() =>
                    {
                        TryAcknowledge(notificationIdentifier);
                        completionSource.TrySetCanceled();
                    })
                };

                //check if timeout is not null and greater than zero, negative value means infinite timeout
                if (addOptions.Timeout > 0)
                    state.Timer = new Timer(OnTimerCallback, state, TimeSpan.FromSeconds(addOptions.Timeout.Value), Timeout.InfiniteTimeSpan);

                return state;
            });

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

        /// <inheritdoc/>
        public void DismissAll()
        {
            foreach (var notificationId in _notificationStates.Keys.ToArray())
            {
                TryDismiss(notificationId);
            }
        }

        /// <inheritdoc/>
        public void AcknowledgeAll()
        {
            foreach (var notificationId in _notificationStates.Keys.ToArray())
            {
                TryAcknowledge(notificationId);
            }
        }

        /// <inheritdoc/>
        public void SuspendTimeOutAll()
        {
            foreach (var notificationId in _notificationStates.Keys.ToArray())
            {
                TrySuspendTimeOut(notificationId, true);
            }
        }

        /// <inheritdoc/>
        public void ResumeTimeOutAll()
        {
            foreach (var notificationId in _notificationStates.Keys.ToArray())
            {
                TrySuspendTimeOut(notificationId, false);
            }
        }

        public bool TryDismiss(int notificationId)
        {
            if (!_notificationStates.TryGetValue(notificationId, out var state))
                return false;

            bool changed = false;
            bool shouldAck = false;

            lock (state.Gate)
            {
                if (state.State == NotificationState.Showing &&
                    !state.AddOptions.NotificationAckOptions.HasFlag(NotificationAckOptions.Dismiss))
                {
                    state.State = NotificationState.Dismissed;
                    state.Dispose();
                    changed = true;
                }
                else
                {
                    shouldAck = true;
                }
            }

            if (shouldAck)
                TryAcknowledge(notificationId);
            else if (changed)
                NotificationsChanged?.Invoke(this, new NotificationsChangedArgs() { NotificationId = notificationId });

            return true;
        }

        public bool TryAcknowledge(int notificationId)
        {
            if (!_notificationStates.TryRemove(notificationId, out var state))
                return false;

            lock (state.Gate)
            {
                state.Dispose();
            }

            NotificationsChanged?.Invoke(this, new NotificationsChangedArgs() { NotificationId = notificationId });

            return true;
        }

        public bool TryTimeOut(int notificationId)
        {
            if (!_notificationStates.TryGetValue(notificationId, out var state))
                return false;

            bool changed = false;

            lock (state.Gate)
            {
                if (state.State == NotificationState.Showing &&
                    !state.AddOptions.NotificationAckOptions.HasFlag(NotificationAckOptions.TimeOut))
                {
                    state.State = NotificationState.TimedOut;
                    state.Dispose();
                    changed = true;
                }
            }

            if (changed)
                NotificationsChanged?.Invoke(this, new NotificationsChangedArgs() { NotificationId = notificationId });
            else
                TryAcknowledge(notificationId);

            return true;
        }

        /// <inheritdoc/>
        public bool TryResetTimeout(int notificationId)
        {
            return TrySuspendTimeOut(notificationId, false);
        }

        public bool TrySuspendTimeout(int notificationId)
        {
            return TrySuspendTimeOut(notificationId, true);
        }

        /// <inheritdoc/>
        public bool TrySuspendTimeOut(int notificationId, bool suspend)
        {
            if (!_notificationStates.TryGetValue(notificationId, out var state))
                return false;

            lock (state.Gate)
            {
                var timer = state.Timer;

                if (timer == null)
                    return false;

                if (!suspend)
                {
                    if (state.AddOptions.Timeout > 0)
                        timer.Change(TimeSpan.FromSeconds(state.AddOptions.Timeout.Value), Timeout.InfiniteTimeSpan);
                }
                else
                {
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                }

                return true;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<INotificationController> GetVisible()
        {
            var snapshot = _notificationStates.ToArray();
            return snapshot
                .OrderByDescending(x => x.Value.CreationTime)
                .ThenByDescending(x => x.Value.AddOptions.Priority)
                .Where(x => x.Value.State == NotificationState.Showing)
                .Select(x => x.Value.Controller)
                .ToList();
        }

        /// <inheritdoc/>
        public IEnumerable<INotificationController> GetDismissed()
        {
            var snapshot = _notificationStates.ToArray();
            return snapshot.Where(x => x.Value.State != NotificationState.Showing)
                .OrderByDescending(x => x.Value.CreationTime)
                .ThenByDescending(x => x.Value.AddOptions.Priority)
                .Select(x => x.Value.Controller)
                .ToList();
        }

        /// <inheritdoc/>
        public bool RequestNotificationHostSize(Size size)
        {
            var handler = SizeRequest;
            if (handler != null)
            {
                var args = new NotificationHostSizeRequestArgs() { RequestedSize = size };
                handler.Invoke(this, args);
                return args.IsSatisfied;
            }

            //nobody handles the event
            return true;
        }

        private void OnTimerCallback(object? state)
        {
            // this callback is called per notification, basically if timeout configured the timer will trigger this callback
            // we need to make checks in order to avoid reentrancy

            if (state is NState nState)
            {
                if (!_notificationStates.TryGetValue(nState.Id, out var current) || !ReferenceEquals(current, nState))
                    return;

                lock (nState.Gate)
                {
                    if (!_notificationStates.TryGetValue(nState.Id, out current) || !ReferenceEquals(current, nState))
                        return;

                    if (nState.State != NotificationState.Showing)
                        return;

                    var timer = nState.Timer;
                    nState.Timer = null;
                    timer?.Dispose();
                }

                nState.Controller.TimeOutResult(); // <- we need to call this function in order to trigger callback and release any waiting threads
            }
        }

        private void RaiseChanged(int notificationId)
        {
            NotificationsChanged?.Invoke(this, new NotificationsChangedArgs() { NotificationId = notificationId });
        }

        private sealed class NState : IDisposable
        {
            public NState(int id, INotificationController notificationController,
                NotificationAddOptions addOptions)
            {
                Id = id;
                Controller = notificationController;
                AddOptions = addOptions;
            }

            public object Gate { get; } = new();

            public int Id { get; init; }

            public CancellationTokenRegistration CancellationTokenRegistration { get; set; }

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

            public void Dispose()
            {
                var reg = CancellationTokenRegistration;
                CancellationTokenRegistration = default;
                reg.Dispose();

                var timer = Timer;
                Timer = null;
                timer?.Dispose();
            }
        }
    }
}
