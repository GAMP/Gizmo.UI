using System.Reflection;
using System.Runtime.InteropServices;

namespace Gizmo.UI
{
    /// <summary>
    /// WPF dispatcher helper class.
    /// </summary>
    /// <remarks>
    /// Provides functionality to invoke code on dispatcher in desktop hosts from libraries that dont target windows framework.
    /// </remarks>
    public static class DispatcherHelper
    {
        private static readonly bool _isWebBrowser = RuntimeInformation.IsOSPlatform(OSPlatform.Create("browser"));

        private static readonly object? _dispatcher = default; //dispatcher class
        private static readonly MethodInfo? _invokeAsyncMethod = default;
        private static readonly PropertyInfo? _taskProperty = default;

        static DispatcherHelper()
        {
            if(!_isWebBrowser)
            {
                var applicationType = Type.GetType("System.Windows.Application,PresentationFramework",true);
                var dispatcherType = Type.GetType("System.Windows.Threading.Dispatcher,WindowsBase",true);
                var dispatcherOperationType = Type.GetType("System.Windows.Threading.DispatcherOperation,WindowsBase", true);

                var currentProperty = applicationType?.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);
                var application = currentProperty?.GetValue(null);
                var dispatcherProperty = applicationType?.GetProperty("Dispatcher", BindingFlags.Public | BindingFlags.Instance);
                _dispatcher = dispatcherProperty?.GetValue(application);
                _invokeAsyncMethod = dispatcherType?.GetMethod("InvokeAsync", new[] { typeof(Action) });
                _taskProperty = dispatcherOperationType?.GetProperty("Task", BindingFlags.Public | BindingFlags.Instance);
            }
        }

        public static async Task InvokeAsync(Action action)
        {           
            var result = _invokeAsyncMethod?.Invoke(_dispatcher, new[] { action });
            if(result!= null && _taskProperty?.GetValue(result) is Task task)
            {
                await task;
            }
        }
    }
}
