using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Gizmo.UI
{
    public class DataAnnotationsValidator
    {
        private static readonly ConcurrentDictionary<(Type ModelType, string FieldName), PropertyInfo> _propertyInfoCache = new ConcurrentDictionary<(Type, string), PropertyInfo>();

        private static bool TryGetValidatableProperty(in FieldIdentifier fieldIdentifier, [NotNullWhen(true)] out PropertyInfo propertyInfo)
        {
            (Type, string) key = (fieldIdentifier.Model.GetType(), fieldIdentifier.FieldName);
            if (!_propertyInfoCache.TryGetValue(key, out propertyInfo))
            {
                propertyInfo = key.Item1.GetProperty(key.Item2);
                _propertyInfoCache[key] = propertyInfo;
            }

            return propertyInfo != null;
        }

        public static void Validate(FieldIdentifier fieldIdentifier, ValidationMessageStore validationMessageStore)
        {
            if (TryGetValidatableProperty(in fieldIdentifier, out PropertyInfo propertyInfo))
            {
                object? value = propertyInfo.GetValue(fieldIdentifier.Model);
                ValidationContext validationContext = new ValidationContext(fieldIdentifier.Model)
                {
                    MemberName = propertyInfo.Name
                };
                List<ValidationResult> list = new List<ValidationResult>();
                Validator.TryValidateProperty(value, validationContext, list);
                Span<ValidationResult> span = CollectionsMarshal.AsSpan(list);
                foreach (ValidationResult validationResult in span)
                {
                    validationMessageStore.Add(in fieldIdentifier, validationResult.ErrorMessage);
                }
            }
        }

        public static void ClearCache()
        {
            _propertyInfoCache.Clear();
        }
    }
}
