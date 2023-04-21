using Microsoft.JSInterop;

namespace Gizmo.UI.Services
{
    /// <summary>
    /// JS Runtime services, used to provide js runtime outside of blazor components.
    /// </summary>
    public sealed class JSRuntimeService
    {
        IJSRuntime? _jSRuntime;

        public IJSRuntime? JSRuntime { get { return _jSRuntime; } }

        public void AssociateJSRuntime(IJSRuntime jSRuntime)
        {
            if(jSRuntime == null)
                throw new ArgumentNullException(nameof(jSRuntime));

            _jSRuntime = jSRuntime;
        }
    }
}
