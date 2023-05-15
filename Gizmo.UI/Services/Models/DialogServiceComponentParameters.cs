namespace Gizmo.UI.Services
{
    /// <summary>
    /// Base class for Blazor Component parameters of Dialog service.
    /// </summary>
    public abstract class DialogServiceComponentParameters
    {
        /// <summary>
        /// Converts properties of this class to the Blazor component parameters object of Dialog service.
        /// </summary>
        /// <returns>
        /// Returns a dictionary of parameters.
        /// </returns>
        public Dictionary<string, object> ToDictionary() => new()
        {
            { "Parameters", this }
        };
    }
}
