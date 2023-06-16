using System.Drawing;

namespace Gizmo.UI.Services
{
    public sealed class NotificationHostSizeRequestArgs
    {
        /// <summary>
        /// Gets requested size.
        /// </summary>
        public Size RequestedSize
        {
            get;init;
        }

        /// <summary>
        /// Gets if request satisifed.
        /// </summary>
        public bool IsSatisfied
        {
            get;set;
        }
    }
}
