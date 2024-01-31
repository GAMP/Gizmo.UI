using System.Diagnostics.CodeAnalysis;

namespace Gizmo.UI.View.States
{
    public abstract class SuggestionViewStateBase : ViewStateBase, ISuggestionViewState
    {
        public abstract object GetDisplayValue();

        public abstract object GetSelectionValue();

        public bool TryGetValue<TValue>(string propertyName, [NotNullWhen(true)] TValue? value)
        {
            return false;
        }
    }
}
