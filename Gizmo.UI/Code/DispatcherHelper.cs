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
        private static readonly bool IsWebBrowser = RuntimeInformation.IsOSPlatform(OSPlatform.Create("browser"));

        private static readonly object? Dispatcher = default; //dispatcher class
        private static readonly MethodInfo? InvokeAsyncMethod = default;
        private static readonly MethodInfo? InvokeMethod = default;
        private static readonly PropertyInfo? TaskProperty = default;

        static DispatcherHelper()
        {
            if(!IsWebBrowser)
            {
                var applicationType = Type.GetType("System.Windows.Application,PresentationFramework",true);
                var dispatcherType = Type.GetType("System.Windows.Threading.Dispatcher,WindowsBase",true);
                var dispatcherOperationType = Type.GetType("System.Windows.Threading.DispatcherOperation,WindowsBase", true);

                var currentProperty = applicationType?.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);
                var application = currentProperty?.GetValue(null);
                var dispatcherProperty = applicationType?.GetProperty("Dispatcher", BindingFlags.Public | BindingFlags.Instance);
                Dispatcher = dispatcherProperty?.GetValue(application);
                InvokeAsyncMethod = dispatcherType?.GetMethod("InvokeAsync", new[] { typeof(Action) });
                InvokeMethod = dispatcherType?.GetMethods()
                    .Where(m=> m.Name =="Invoke" && m.ReturnType.Name =="TResult")
                    .First()
                    .MakeGenericMethod(typeof(object));
                TaskProperty = dispatcherOperationType?.GetProperty("Task", BindingFlags.Public | BindingFlags.Instance);
            }
        }

        /// <summary>
        /// Invokes specified action on current dispatcher.
        /// </summary>
        /// <param name="action">Action.</param>
        /// <exception cref="NotSupportedException">is thrown in case this method invoked in non desktop environment.</exception>
        public static async Task InvokeAsync(Action action)
        {
            //this operation is only supported on wpf host
            if (IsWebBrowser)
                throw new NotSupportedException();

            var result = InvokeAsyncMethod?.Invoke(Dispatcher, new[] { action });
            if(result!= null && TaskProperty?.GetValue(result) is Task task)
                await task;           
        }

        /// <summary>
        /// Invokes specified action on current dispatcher.
        /// </summary>
        /// <param name="action">Action.</param>
        /// <exception cref="NotSupportedException">is thrown in case this method invoked in non desktop environment.</exception>
        public static T? Invoke<T>(Func<T> action)
        {
            //this operation is only supported on wpf host
            if (IsWebBrowser)
                throw new NotSupportedException();

            return (T?)InvokeMethod?.Invoke(Dispatcher, new[] { action });
        }
    }
}
