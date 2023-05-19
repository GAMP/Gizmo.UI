using Microsoft.JSInterop;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// JS Runtime services, used to provide js runtime outside of blazor components.
    /// </summary>
    public sealed class JSRuntimeService
    {
        IJSRuntime? _jSRuntime;

        /// <summary>
        /// Gets associated js runtime.
        /// </summary>
        public IJSRuntime? JSRuntime { get { return _jSRuntime; } }

        /// <summary>
        /// Associates JS runtime with this service.
        /// </summary>
        /// <param name="jSRuntime">JS runtime.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AssociateJSRuntime(IJSRuntime jSRuntime)
        {
<<<<<<< HEAD
            _jSRuntime = jSRuntime ?? throw new ArgumentNullException(nameof(jSRuntime));
=======
            if (jSRuntime == null)
                throw new ArgumentNullException(nameof(jSRuntime));

            _jSRuntime = jSRuntime;
>>>>>>> 38d1fe8a69959c09e34b607d5064256177a47197
        }
    }
}
