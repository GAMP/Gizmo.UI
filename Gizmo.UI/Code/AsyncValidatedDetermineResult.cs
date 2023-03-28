namespace Gizmo.UI.View.Services
{
    /// <summary>
    /// Result allowing provide information for current async properties validation state.
    /// </summary>
    public class AsyncValidatedDetermineResult
    {
        /// <summary>
        /// Default unhandled result.
        /// </summary>
        public static readonly AsyncValidatedDetermineResult DefaultUnhandled = new() { IsHandled = false };

        /// <summary>
        /// Default handled and true result.
        /// </summary>
        public static readonly AsyncValidatedDetermineResult DefaultTrue = new() { IsHandled = true, IsAsyncValidated = true };

        /// <summary>
        /// Indicates that check was handled.
        /// </summary>
        public bool IsHandled
        {
            get; init;
        }

        /// <summary>
        /// Indicates that all async fields considered validated.
        /// </summary>
        public bool IsAsyncValidated
        {
            get; init;
        }
    }
}
