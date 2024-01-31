using Gizmo.UI.Services;
using Gizmo.UI.View.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gizmo.UI.View.Services
{
    public abstract class SuggestionsViewServiceBase<TSuggestionsViewState, TSuggestionViewState> : ValidatingViewStateServiceBase<TSuggestionsViewState>, ISuggestionService
      where TSuggestionsViewState : ISuggestionsViewState<TSuggestionViewState>
      where TSuggestionViewState : ISuggestionViewState
    {
        ISuggestionsViewState ISuggestionService.ViewState => ViewState;

        public SuggestionsViewServiceBase(TSuggestionsViewState viewState, ILogger<SuggestionsViewServiceBase<TSuggestionsViewState, TSuggestionViewState>> logger, IServiceProvider serviceProvider) :
            base(viewState, logger, serviceProvider)
        {
            _debounceActionAsyncService = ServiceProvider.GetRequiredService<DebounceActionAsyncService>();
        }

        private CancellationTokenSource? _cancellationTokenSource;
        private readonly DebounceActionAsyncService _debounceActionAsyncService;

        public Task SetSuggestionPatternAsync(string pattern)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            //we could validate pattern here if required

            ViewState.Pattern = pattern;
            ViewState.RaiseChanged();

            _debounceActionAsyncService.Debounce(GenerateSuggestionsAsyncInternal, _cancellationTokenSource.Token);

            return Task.CompletedTask;
        }

        private async Task GenerateSuggestionsAsyncInternal(CancellationToken cancellationToken = default)
        {
            ViewState.IsInitializing = true;

            try
            {
                await GenerateSuggestionsAsync(cancellationToken);
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

        protected abstract Task GenerateSuggestionsAsync(CancellationToken cancellationToken = default);

        public abstract ISuggestionViewState GetSuggestionViewState(object value);
    }
}
