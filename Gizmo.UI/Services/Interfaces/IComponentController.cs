namespace Gizmo.UI.Services
{
    public interface IComponentController
    {
        /// <summary>
        /// Gets component type.
        /// </summary>
        /// <remarks>
        /// This type represent an Razor component.
        /// </remarks>
        Type ComponentType { get; }

        /// <summary>
        /// Gets component parameters.
        /// </summary>
        /// <remarks>
        /// This parameter passed to <see cref="Microsoft.AspNetCore.Components.DynamicComponent.Parameters"/>.<br></br>
        /// This dictonary will always contain following values <br></br>
        /// <br></br>
        /// 1) CancelCallback (EventCallback)<br></br>
        /// 2) ResultCallback (EventCallback[T]) where T will be eqault to <see cref="EmptyComponentResult"/> for dialogs without custom result or to any other custom result return type depending on dialog implementation.<br></br>
        /// 3) ErrorCallback  (EventCallback[Exception])<br></br>
        /// 3) DisplayOptions (<see cref="DialogDisplayOptions"/>)<br></br>
        /// </remarks>
        IDictionary<string, object> Parameters { get; }

        /// <summary>
        /// Gets component identifier.
        /// </summary>
        /// <remarks>
        /// This identifier represents an unique id provided by component service.
        /// </remarks>
        public int Identifier { get; }

        /// <summary>
        /// Initiates cancellation.
        /// </summary>
        Task CancelAsync();

        /// <summary>
        /// Provides custom result.
        /// </summary>
        /// <param name="result">Dialog result.</param>
        /// <exception cref="InvalidCastException">thrown in case result type does not match type of object provided by <paramref name="result"/>.</exception>
        Task ResultAsync(object result);

        /// <summary>
        /// Provides default empty result.
        /// </summary>
        /// <remarks>
        /// This can be used when component does not provide any custom result in order to signal component closure.
        /// </remarks>
        Task EmptyResultAsync();

        /// <summary>
        /// Provides error.
        /// </summary>
        /// <param name="error">Error exception.</param>
        /// <remarks>
        /// This method is also used by <see cref="TimeOutResultAsync"/> and signals error by providing <see cref="TimeoutException"/>.
        /// </remarks>
        Task ErrorResultAsync(Exception error);

        /// <summary>
        /// Times out.
        /// </summary>
        Task TimeOutResultAsync();
    }
}
