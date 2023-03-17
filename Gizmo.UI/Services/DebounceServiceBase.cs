using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gizmo.UI.View.States;
using Microsoft.Extensions.Logging;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// Gneric debouncig service base.
    /// </summary>
    /// <typeparam name="TDebounceType">Debounce item type.</typeparam>
    public abstract class DebounceServiceBase<TDebounceType> : IDisposable where TDebounceType : class
    {
        #region CONSTRUCTOR
        public DebounceServiceBase(ILogger logger)
        {
            _logger = logger;
        } 
        #endregion

        #region FIELDS
        private readonly Subject<TDebounceType> _subject = new();
        private IDisposable? _subscription;
        private readonly ILogger _logger;
        private int _debounceBufferTime = 1000; // 1 sec by default
        #endregion

        #region PROPERTIES

        protected ILogger Logger => _logger;

        protected int DebounceBufferTime
        {
            get { return _debounceBufferTime; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(DebounceBufferTime));

                //update current value
                _debounceBufferTime = value;

                //resubscribe
                DebounceSubscribe();
            }
        }

        #endregion

        #region PUBLIC FUNCTIONS

        public void Debounce(TDebounceType debounceType)
        {
            _subject.OnNext(debounceType);
        }

        #endregion

        #region PROTECTED FUNCTIONS

        private void DebounceSubscribe()
        {
            //dispose any existing subscriptions
            _subscription?.Dispose();

            //resubscribe
            _subscription = _subject
                .Buffer(TimeSpan.FromMilliseconds(DebounceBufferTime))
                .Where(buffer => buffer.Count > 0)
                .Distinct()
                .Subscribe(viewStates =>
                {
                    foreach (IViewState changedViewState in viewStates)
                    {
                        try
                        {
                            changedViewState.RaiseChanged();
                        }
                        catch (Exception ex)
                        {
                            //the handlers are outside of our code so we should handle the exception and log it
                            Logger.LogError(ex, "Error in view state change debounce handler.");
                        }
                    }
                });
        }

        protected void OnDebounce(IEnumerable<TDebounceType> debounceTypes)
        {
            foreach (var debounceType in debounceTypes)
            {
                try
                {
                    OnDebounce(debounceType);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to debounce item {item}", debounceType.ToString());
                }
            }
        }

        protected abstract void OnDebounce(TDebounceType debounceType);

        public void Dispose()
        {
            _subject?.Dispose();
            _subscription?.Dispose();
        }

        #endregion
    }
}
