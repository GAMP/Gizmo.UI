using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Gizmo.UI.Services;
using Gizmo.UI.View.States;
using Microsoft.Extensions.DependencyInjection;
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
            _debounceActionAsyncService = ServiceProvider.GetRequiredService<DebounceActionAsyncService>();
            _debounceActionAsyncService.DebounceBufferTime = 1000;
        }

        private CancellationTokenSource? _cancellationTokenSource;
        private CancellationTokenSource? _suggestionInitTokenSource;
        private readonly DebounceActionAsyncService _debounceActionAsyncService;
        private readonly ConcurrentDictionary<object, TSuggestionViewState> _suggestionsViewStateCache = new();
        
        private string? _previousPattern;

        public Task SetSuggestionPatternAsync(string pattern)
        {
            if (pattern == _previousPattern)
                return Task.CompletedTask;

            _previousPattern = pattern;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            //we could validate pattern here if required

            ViewState.IsInitializing = true;//this will be reset once the debounce method completes

            ViewState.Pattern = pattern;
            ViewState.ResetSuggestions();
            ViewState.RaiseChanged();

            _debounceActionAsyncService.Debounce(GenerateSuggestionsAsyncInternal, _cancellationTokenSource.Token);

            return Task.CompletedTask;
        }

        private async Task GenerateSuggestionsAsyncInternal(CancellationToken cancellationToken = default)
        {
            try
            {
                await GenerateSuggestionsAsync(cancellationToken);

                var states = ViewState.Suggestions;

                foreach (var state in states)
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
            if(value == null) return null;

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
        /// When implemented should initialize suggestions based on <see cref="TSuggestionsViewState.Pattern"/>.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected abstract Task GenerateSuggestionsAsync(CancellationToken cancellationToken = default);

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

        protected override void OnDisposing(bool dis)
        {
            _debounceActionAsyncService?.Dispose();
            _suggestionsViewStateCache?.Clear();

            _cancellationTokenSource?.Cancel();
            _suggestionInitTokenSource?.Cancel();

            _cancellationTokenSource?.Dispose();
            _suggestionInitTokenSource?.Dispose();

            base.OnDisposing(dis);
        }
    }
}
