namespace Gizmo.UI.View.States
{
    /// <summary>
    /// Generic suggestions view state base.
    /// </summary>
    /// <typeparam name="TSuggestion">Suggestion type.</typeparam>
    public abstract class SuggestionsViewStateBase<TSuggestion> : ValidatingViewStateBase, ISuggestionsViewState<TSuggestion> where TSuggestion : ISuggestionViewState
    {
        public string? Pattern { get; set; }

        public IEnumerable<TSuggestion> Suggestions { get; set; } = Enumerable.Empty<TSuggestion>();

        IEnumerable<ISuggestionViewState> ISuggestionsViewState.Suggestions => Suggestions.OfType<ISuggestionViewState>();

        public void ResetSuggestions()
        {
            Suggestions = Enumerable.Empty<TSuggestion>();
        }
    }
}
