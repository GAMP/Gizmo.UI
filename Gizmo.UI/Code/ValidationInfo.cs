using Gizmo.UI.View.States;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace Gizmo.UI
{
    /// <summary>
    /// Valiadation info generator class.
    /// </summary>
    public static class ValidationInfo
    {
        #region READ ONLY FIELDS
        private static readonly Type STRING_TYPE = typeof(string);
        private static readonly Type DECIMAL_TYPE = typeof(decimal);
        private static readonly Type DECIMAL_NULLABLE_TYPE = typeof(decimal?);
        private static readonly Type INT_NULLABLE_TYPE = typeof(int?);
        private static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> _propertyCache = new();
        #endregion

        #region PREDICTATE

        /// <summary>
        /// Predicate to filter out validating properties.
        /// </summary>
        private static readonly Predicate<PropertyInfo> _validatingPropertiesPredictate = (p) =>
        {
            //must not contain property change ignore attribute
            return p.GetCustomAttribute<PropertyChangeIgnoreAttribute>() == null &&
            //must contain validating attribute
            p.GetCustomAttribute<ValidatingPropertyAttribute>() != null;
        };

        /// <summary>
        /// Predicate to filter out non-class properties.
        /// </summary>
        private static readonly Predicate<PropertyInfo> _nonClassPropertiesPredictate = (p) =>
        {
            //must be a primitve type https://docs.microsoft.com/en-us/dotnet/api/system.type.isprimitive?view=net-6.0 or string
            //other types might need to be added
            return p.PropertyType.IsPrimitive || p.PropertyType == STRING_TYPE || p.PropertyType == DECIMAL_TYPE || p.PropertyType == DECIMAL_NULLABLE_TYPE || p.PropertyType == INT_NULLABLE_TYPE;
        };

        /// <summary>
        /// Predicate to filter out class properties.
        /// </summary>
        private static readonly Predicate<PropertyInfo> _classPropertiesPredictate = (p) =>
        {
            //must not be primitive type https://docs.microsoft.com/en-us/dotnet/api/system.type.isprimitive?view=net-6.0 and not string
            //some other types might need to be added
            return p.PropertyType.IsClass && !p.PropertyType.IsPrimitive && p.PropertyType != STRING_TYPE;
        };

        #endregion

        #region PUBLIC FUNCTIONS

        /// <summary>
        /// Gets validation information for specified view state object.
        /// </summary>
        /// <param name="viewState">View state instane.</param>
        /// <returns>List of validation info.</returns>
        public static IEnumerable<InstanceValidationInfo> Get(IValidatingViewState viewState)
        {
            if (viewState == null)
                throw new ArgumentNullException(nameof(viewState));

            List<InstanceValidationInfo> validationObjects = new();
            Get(viewState, viewState.GetType(), validationObjects);

            return validationObjects;
        }

        #endregion

        #region PRIVATE FUNCTIONS

        /// <summary>
        /// Gets all validation properties on specified type.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns>List of property info.</returns>
        private static IEnumerable<PropertyInfo> GetProperties(Type type)
        {
            //since we requesting propeties per type we can use cache to speed up the process
            return _propertyCache.GetOrAdd(type, k =>
            {
                //get all properties that are annotated with ValidatingProperty attribute.
                return k.GetProperties().Where(property => _validatingPropertiesPredictate(property));
            });
        }

        /// <summary>
        /// Recursevly gets validation info on specified object.
        /// </summary>
        /// <param name="instance">Object instance.</param>
        /// <param name="type">Object type.</param>
        /// <param name="validationInfos">Validation info collection.</param>
        private static void Get(object? instance, Type type, IList<InstanceValidationInfo> validationInfos)
        {
            //get all validating properties for the type specified
            var properties = GetProperties(type);

            //get all properties that are not class
            var nonClassProperties = properties.Where(p => _nonClassPropertiesPredictate(p));

            //create new info
            validationInfos.Add(new InstanceValidationInfo(instance, type, nonClassProperties));

            //get all properties that represent a class
            var classProperties = properties.Where(p => _classPropertiesPredictate(p));

            //recurse all properties
            foreach (var property in classProperties)
            {
                //get instance value
                //the obj value might be equal to null
                var obj = property.GetValue(instance);

                //recurse and get properties that should be validated
                Get(obj, property.PropertyType, validationInfos);
            }
        }

        #endregion
    }

    /// <summary>
    /// Object instance validation information.
    /// </summary>
    public sealed class InstanceValidationInfo
    {
        #region CONSTRUCTOR
        /// <summary>
        /// Creates new instance of <see cref="InstanceValidationInfo"/>.
        /// </summary>
        /// <param name="instance">Object instance.</param>
        /// <param name="instanceType">Instance type.</param>
        /// <param name="properties">Validating properties.</param>
        public InstanceValidationInfo(object? instance, Type instanceType, IEnumerable<PropertyInfo> properties)
        {
            Instance = instance;
            InstanceType = instanceType;
            Properties = properties;
        } 
        #endregion

        #region PROPERTIES

        /// <summary>
        /// Gets object instance.
        /// </summary>
        public object? Instance
        {
            get; init;
        }

        /// <summary>
        /// Gets instance type.
        /// </summary>
        public Type InstanceType
        {
            get; init;
        }

        /// <summary>
        /// Gets properties that participate in validation.
        /// </summary>
        public IEnumerable<PropertyInfo> Properties
        {
            get; init;
        }

        #endregion
    }
}
