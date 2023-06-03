using Microsoft.AspNetCore.Components;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// Base class for the Blazor component of Dialog service with required parameters.
    /// </summary>
    public abstract class DialogServiceComponentBase : ComponentBase
    {
        [Parameter]
        public EventCallback DismissCallback { get; set; }

        [Parameter]
        public EventCallback<EmptyComponentResult> ResultCallback { get; set; }

        [Parameter]
        public DialogDisplayOptions? DisplayOptions { get; set; }

        /// <summary>
        /// This property is used for version compatibility while passing parameters to the Blazor component in case of changing the required parameters of DialogServiceComponentBase.
        /// The Blazor DynamicComponent will map all its parameters to this object.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object>? Values { get; set; }
    }

    /// <summary>
    /// Base class for the Blazor component of Dialog service with required parameters that contains a class of parameters as Parameters property.
    /// </summary>
    /// <typeparam name="T">
    /// Type of Blazor Component parameters of Dialog service that inherits from DialogServiceComponentParameters.
    /// </typeparam>
    public abstract class DialogServiceComponentBase<T> : DialogServiceComponentBase where T : DialogServiceComponentParameters
    {
        [Parameter]
        public T Parameters { get; set; } = default!;
    }
}
