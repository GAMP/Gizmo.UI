namespace Gizmo.UI.Services
{
    /// <summary>
    /// Dynamic component dialog interface.
    /// </summary>
    public interface IDynamicComponentDialog
    {
        #region PROPERTIES

        /// <summary>
        /// Gets dialog display options.
        /// </summary>
        DialogDisplayOptions DisplayOptions
        {
            get;
        }

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
        /// This parameter passed to <see cref="Microsoft.AspNetCore.Components.DynamicComponent.Parameters"/>.
        /// </remarks>
        IDictionary<string, object> Parameters { get; }

        #endregion

        #region FUNCTIONS
        
        /// <summary>
        /// Cancels dialog.
        /// </summary>
        Task CancelAsync();

        /// <summary>
        /// Provides dialog result.
        /// </summary>
        /// <param name="result">Dialog result.</param>
        /// <exception cref="InvalidCastException">thrown in case result type does not match type of object provided by <paramref name="result"/>.</exception>
        Task ProvideResultAsync(object result); 

        #endregion
    }
}