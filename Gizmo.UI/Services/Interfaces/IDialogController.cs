namespace Gizmo.UI.Services
{
    /// <summary>
    /// Dialog controller interface.
    /// </summary>
    public interface IDialogController : IComponentController
    {
        /// <summary>
        /// Gets dialog display options.
        /// </summary>
        /// <remarks>
        /// This property is a shortcut to the display options for the dialog host, same options are contained in the <see cref="Parameters"/> dictionary and can be used by dialog component itself.
        /// </remarks>
        DialogDisplayOptions DisplayOptions
        {
            get;
        }
    }
}
