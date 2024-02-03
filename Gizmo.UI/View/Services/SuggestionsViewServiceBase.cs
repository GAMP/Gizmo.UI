using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gizmo.UI.View.States;
using Microsoft.Extensions.Logging;

namespace Gizmo.UI.View.Services
{
    public abstract class SuggestionsViewServiceBase<TSuggestionsViewState, TSuggestionViewState> : ValidatingViewStateServiceBase<TSuggestionsViewState>,
        ISuggestionService
        where TSuggestionsViewState : ISuggestionsViewState<TSuggestionViewState>
        where TSuggestionViewState : ISuggestionViewState, new()
    {
        ISuggestionsViewState ISuggestionService.ViewState => ViewState;

        public SuggestionsViewServiceBase(TSuggestionsViewState viewState, ILogger<SuggestionsViewServiceBase<TSuggestionsViewState, TSuggestionViewState>> logger, IServiceProvider serviceProvider) :
            base(viewState, logger, serviceProvider)
        {
        }

        private CancellationTokenSource? _cancellationTokenSource;
        private CancellationTokenSource? _suggestionInitTokenSource;
        private readonly ConcurrentDictionary<object, TSuggestionViewState> _suggestionsViewStateCache = new();

        private Subject<string>? _patternSubject;
        private IDisposable? _patternSubscription;
        
        private string? _previousPattern;

        public Task SetSuggestionPatternAsync(string pattern)
        {
            if (pattern == _previousPattern)
                return Task.CompletedTask;

            _previousPattern = pattern;
     
            //we could validate pattern here if required
            ViewState.IsInitializing = true;//this will be reset once the debounce method completes

            ViewState.Pattern = pattern;
            ViewState.ResetSuggestions();
            ViewState.RaiseChanged();

            _patternSubject ??= new Subject<string>();
            _patternSubscription ??= _patternSubject
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(PatternChangeSubscriber);

            _patternSubject.OnNext(pattern);

            return Task.CompletedTask;
        }

        private async Task GenerateSuggestionsAsyncInternal(CancellationToken cancellationToken = default)
        {
            try
            {
                var suggestions = await GenerateSuggestionsAsync(cancellationToken);
                ViewState.SetSuggestions(suggestions);
                foreach (var state in suggestions)
                    _suggestionsViewStateCache.AddOrUpdate(state.GetSelectionValue(), (s) => state, (s, e) => state);          

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to generate suggestions.");
            }
            finally
            {
                ViewState.IsInitializing = false;
            }

            ViewState.RaiseChanged();
        }

        public ISuggestionViewState? GetSuggestionViewState(object? value)
        {
            if(value == null) 
                return null;

            if (TryGetFromCache(value, out var result))
                return result;

            var uninitializedInstance = new TSuggestionViewState()
            {
                IsInitialized = false,
                IsInitializing = true
            };

            _suggestionInitTokenSource?.Cancel();
            _suggestionInitTokenSource = new CancellationTokenSource();

            _ = InitializeSuggestionAsync(value, uninitializedInstance, _suggestionInitTokenSource.Token)
                .ContinueWith(InitializeSuggestionCallback, uninitializedInstance, _suggestionInitTokenSource.Token);

            return uninitializedInstance;
        }

        protected bool TryGetFromCache(object cacheKey, [NotNullWhen(true)] out TSuggestionViewState? cachedState)
        {
            return _suggestionsViewStateCache.TryGetValue(cacheKey, out cachedState);
        }

        /// <summary>
        /// When implemented should initialize suggestion instance.
        /// </summary>
        /// <param name="value">Suggestion value (key).</param>
        /// <param name="viewState">Suggestion state.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected abstract Task InitializeSuggestionAsync(object value, TSuggestionViewState viewState, CancellationToken cancellationToken);

        /// <summary>
        /// When implemented should generate suggestions based on <see cref="TSuggestionsViewState.Pattern"/>.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Generated suggestions.</returns>
        protected abstract Task<IEnumerable<TSuggestionViewState>> GenerateSuggestionsAsync(CancellationToken cancellationToken = default);

        protected void InitializeSuggestionCallback(Task task, object? suggestionState)
        {
            if (suggestionState is TSuggestionViewState suggestion)
            {
                //state is no longer initializing
                suggestion.IsInitializing = false;

                if (!task.IsFaulted)
                {
                    suggestion.IsInitialized = true;
                    _suggestionsViewStateCache.AddOrUpdate(suggestion.GetSelectionValue(), (s) => suggestion, (s, e) => suggestion);                    
                }
                else
                {
                    //we either cancelled or initialization have failed
                    //some other failes suggestion state ?

                    suggestion.IsInitialized = false;
                }

                suggestion.RaiseChanged();
            }
        }

        private void PatternChangeSubscriber(string pattern)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            _ = GenerateSuggestionsAsyncInternal(_cancellationTokenSource.Token)               
                .ContinueWith(task =>
                {
                    if (task.IsFaulted)
                        Logger.LogError(task.Exception, "GenerateSuggestionsAsyncInternal throttled handler error.");
                })
                .ConfigureAwait(false);
        }

        protected override void OnDisposing(bool dis)
        {
            _suggestionsViewStateCache?.Clear();

            _cancellationTokenSource?.Cancel();
            _suggestionInitTokenSource?.Cancel();

            _cancellationTokenSource?.Dispose();
            _suggestionInitTokenSource?.Dispose();

            _patternSubject?.Dispose();
            _patternSubscription?.Dispose();

            base.OnDisposing(dis);
        }
    }
}
