namespace Gizmo.UI.View.States
{
    /// <summary>
    /// View state reference class.
    /// </summary>
    /// <remarks>
    /// Provides functionality for the components to remove reference for the state.<br></br>
    /// <br></br>
    /// This will allow the view state producer to release any instances when they are no more references to them.<br></br>
    /// Any consumer needs to increase reference by calling <see cref="AddReference"/> when obtaining the object instance and <see cref="RemoveReference"/> when its not longer needed.<br></br>
    /// <br></br>
    /// <b>Usually removal will happen on disposal of consumer.</b>
    /// </remarks>
    public abstract class ViewStateReferenceBase : ViewStateBase
    {
        private int _referenceCount;

        /// <summary>
        /// Indicates if view state is referenced.
        /// </summary>
        public bool IsReferenced
        {
            get { return _referenceCount > 0; }
        }

        /// <summary>
        /// Adds reference.
        /// </summary>
        public void AddReference()
        {
            Interlocked.Add(ref _referenceCount, 1);
        }

        /// <summary>
        /// Removes reference.
        /// </summary>
        public void RemoveReference()
        {
            Interlocked.Add(ref _referenceCount, -1);
        }
    }
}
