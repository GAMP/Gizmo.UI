using System.Collections.Concurrent;
using System.Reflection;
using Gizmo.UI.View.States;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Asp net core component extensions.
    /// </summary>
    public static class ComponentExtensions
    {
        #region READONLY FIELDS
        private static readonly string STATE_HAS_CHANGED_METHOD_NAME = "StateHasChanged";
        private static readonly string INVOKE_ASYNC_METHOD_NAME = "InvokeAsync";
        private static readonly Type COMPONENT_TYPE = typeof(ComponentBase);
        private static readonly MethodInfo? STATE_HAS_CHANGED_METHOD = COMPONENT_TYPE.GetMethod(STATE_HAS_CHANGED_METHOD_NAME, BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo? INVOKE_ASYNC_METHOD = COMPONENT_TYPE.GetMethod(INVOKE_ASYNC_METHOD_NAME, BindingFlags.NonPublic | BindingFlags.Instance, new Type[] { typeof(Action) });
        private static readonly ConcurrentDictionary<ComponentBase, EventHandler> _delegates = new();
        #endregion

        #region FUNCTIONS

        /// <summary>
        /// Subscribes to the <see cref="IViewState"/> change event and invokes StateHasChanged function on specified component.
        /// </summary>
        /// <param name="component">Component.</param>
        /// <param name="viewState">View state.</param>
        /// <exception cref="ArgumentNullException">thrown if any of the specified parameters is equal to null.</exception>
        public static void SubscribeChange(this ComponentBase component, IViewState viewState)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (viewState == null)
                throw new ArgumentNullException(nameof(viewState));

            //create or get event handler for the component
            var eventHandler = _delegates.GetOrAdd(component, new EventHandler((object? sender, EventArgs e) =>
            {

                if (STATE_HAS_CHANGED_METHOD is not null)
                {
                    var STATE_CHANGED_DELEGATE = Delegate.CreateDelegate(typeof(Action), component, STATE_HAS_CHANGED_METHOD);
                    if (INVOKE_ASYNC_METHOD is not null)
                    {
                        INVOKE_ASYNC_METHOD?.Invoke(component, new object[] { STATE_CHANGED_DELEGATE });
                    }
                }
            }));

            //remove any previous handler
            viewState.OnChange -= eventHandler;

            //add handler
            viewState.OnChange += eventHandler;
        }

        /// <summary>
        /// Unsubscribes specified component from the <see cref="IViewState"/> change event.
        /// </summary>
        /// <param name="component">Component.</param>
        /// <param name="viewState">View state.</param>
        /// <exception cref="ArgumentNullException">thrown if any of the specified parameters is equal to null.</exception>
        public static void UnsubscribeChange(this ComponentBase component, IViewState viewState)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (viewState == null)
                throw new ArgumentNullException(nameof(viewState));

            if (_delegates.TryRemove(component, out var eventHandler))
                viewState.OnChange -= eventHandler;
        }

        #endregion
    }
}
