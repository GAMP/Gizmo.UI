using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;

namespace Gizmo.UI
{
    public static class FieldIdentifierExtensions
    {
        /// <summary>
        /// Compares field equality.
        /// </summary>
        /// <param name="fieldIdentifier">Field identifier.</param>
        /// <param name="accessor">Field accessor method.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool FieldEquals<T>(this FieldIdentifier fieldIdentifier, Expression<Func<T>> accessor)
        {
            return fieldIdentifier.Equals(FieldIdentifier.Create(accessor));
        }
    }
}
