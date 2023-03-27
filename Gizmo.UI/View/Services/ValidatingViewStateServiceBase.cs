using Gizmo.UI.View.States;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;

namespace Gizmo.UI.View.Services
{
    /// <summary>
    /// View state service supporting validating view state.
    /// </summary>
    /// <typeparam name="TViewState">Validating view state.</typeparam>
    public abstract class ValidatingViewStateServiceBase<TViewState> : ViewStateServiceBase<TViewState> where TViewState : IValidatingViewState
    {
        #region CONSTRUCTOR
        public ValidatingViewStateServiceBase(TViewState viewState,
            ILogger logger,
            IServiceProvider serviceProvider) : base(viewState, logger, serviceProvider)
        {
            _editContext = new EditContext(viewState);

            //add current async validations to the edit context properties
            //we could just use the field as is BUT it might be usefull to have this value shared since some custom components can make use of it
            //for example some custom component could check this value when EditContext validation state changes
            //and provide some visual feedback if any async validation is still running for one or multiple fields
            _editContext.Properties[CURRENT_ASYNC_VALIDATING_PROPERTIES] = _asyncValidatingProperties;

            _validationMessageStore = new ValidationMessageStore(_editContext);
        }
        #endregion

        #region READ ONLY FIELDS
        private readonly EditContext _editContext;
        private readonly ValidationMessageStore _validationMessageStore;
        #endregion

        private const string CURRENT_ASYNC_VALIDATING_PROPERTIES = "CurrentAsyncValidations";
        private readonly HashSet<FieldIdentifier> _asyncValidatedProperties = new(); //use hashset so same field does not appear more than once
        private readonly HashSet<FieldIdentifier> _asyncValidatingProperties = new(); //use hashset so same field does not appear more than once

        #region PROPERTIES

        /// <summary>
        /// Gets user login model edit context.
        /// </summary>
        public EditContext EditContext
        {
            get { return _editContext; }
        }

        #endregion

        #region PROTECTED FUNCTIONS

        /// <summary>
        /// Resets current validation state.
        /// The method will do the following operation.<br></br>
        /// * Clear validation error message store.<br></br>
        /// * Clear any async validations from _asyncValidatedProperties.<br></br>
        /// * Mark edit context as unmodified.<br></br>
        /// * Call <see cref="EditContext.NotifyValidationStateChanged"/> function on current edit context.<br></br>        
        /// </summary>
        /// <remarks>
        /// The purpose of this function is to bring the validation to its initial state.
        /// </remarks>
        protected void ResetValidationState()
        {
            _validationMessageStore.Clear();
            _asyncValidatedProperties.Clear();
            _editContext.MarkAsUnmodified();
            _editContext.NotifyValidationStateChanged();
        }

        /// <summary>
        /// Adds error to validation message store for specified field.
        /// </summary>
        /// <param name="accessor">Field accessor.</param>
        /// <param name="error">Error message.</param>
        /// <param name="notifyValidationStateChanged">Indicates if <see cref="EditContext.NotifyValidationStateChanged"/> should be called.</param>
        protected void AddError<T>(Expression<Func<T>> accessor, string error, bool notifyValidationStateChanged = true)
        {
            AddError(FieldIdentifier.Create(accessor), error, notifyValidationStateChanged);
        }

        /// <summary>
        /// Adds error to validation message store for specified field.
        /// </summary>
        /// <param name="fieldIdentifier">Field identifier.</param>
        /// <param name="error">Error message.</param>
        /// <param name="notifyValidationStateChanged">Indicates if <see cref="EditContext.NotifyValidationStateChanged"/> should be called.</param>
        protected void AddError(FieldIdentifier fieldIdentifier, string error, bool notifyValidationStateChanged = true)
        {
            _validationMessageStore.Add(fieldIdentifier, error);

            if (notifyValidationStateChanged)
                EditContext.NotifyValidationStateChanged();
        }

        /// <summary>
        /// Clears all errors with specified field from error message store.
        /// </summary>
        /// <param name="accessor">Field accessor.</param>
        /// <param name="notifyValidationStateChanged">Indicates if <see cref="EditContext.NotifyValidationStateChanged"/> should be called.</param>
        protected void ClearError<T>(Expression<Func<T>> accessor, bool notifyValidationStateChanged = true)
        {
            ClearError(FieldIdentifier.Create(accessor), notifyValidationStateChanged);
        }

        /// <summary>
        /// Clears all errors with specified field from error message store.
        /// </summary>
        /// <param name="fieldIdentifier">Field identifier.</param>
        /// <param name="notifyValidationStateChanged">Indicates if <see cref="EditContext.NotifyValidationStateChanged"/> should be called.</param>
        protected void ClearError(FieldIdentifier fieldIdentifier, bool notifyValidationStateChanged = true)
        {
            _validationMessageStore.Clear(fieldIdentifier);

            if (notifyValidationStateChanged)
                EditContext.NotifyValidationStateChanged();
        }

        /// <summary>
        /// Mark specified property as modified and returns associated <see cref="FieldIdentifier"/>.
        /// </summary>
        /// <param name="fieldIdentifier">Field identifier.</param>
        /// <param name="notifyFieldChanged">Indicates if <see cref="EditContext.NotifyFieldChanged"/> should be called.</param>
        protected void MarkModified(FieldIdentifier fieldIdentifier, bool notifyFieldChanged = true)
        {
            //check if property is marked as modified already
            //this should only occur if the property was updated from an InputComponent that is aware of EditContext
            if (!EditContext.IsModified(fieldIdentifier))
            {
                //mark as modified and raise FieldChanged on EditContext
                //if for some reason we need to have consistent FieldChanged event this migtht need to be called every time
                if (notifyFieldChanged)
                    EditContext.NotifyFieldChanged(fieldIdentifier);
            }
        }

        /// <summary>
        /// Checks if property is being validated asynchronosly.
        /// </summary>
        /// <param name="accessor">Field accessor.</param>
        /// <returns>True or false.</returns>
        protected bool IsAsyncValidating<T>(Expression<Func<T>> accessor)
        {
            return IsAsyncValidating(FieldIdentifier.Create(accessor));
        }

        /// <summary>
        /// Checks if property is being validated asynchronosly.
        /// </summary>
        /// <param name="fieldIdentifier">Field identifier.</param>
        /// <returns>True or false.</returns>
        protected bool IsAsyncValidating(FieldIdentifier fieldIdentifier)
        {
            return _asyncValidatingProperties.Contains(fieldIdentifier);
        }

        /// <summary>
        /// Validates property.
        /// </summary>
        /// <param name="accessor">Field accessor.</param>
        /// <param name="trigger">Trigger.</param>
        /// <param name="notifyFieldChanged">Indicates if <see cref="EditContext.NotifyFieldChanged"/> should be called.</param>
        protected void ValidateProperty<T>(Expression<Func<T>> accessor, ValidationTrigger trigger = ValidationTrigger.Input, bool notifyValidationStateChanged = true)
        {
            ValidateProperty(FieldIdentifier.Create(accessor), trigger, notifyValidationStateChanged);
        }

        /// <summary>
        /// Validates property.
        /// </summary>
        /// <param name="fieldIdentifier">Field identifier.</param>
        /// <param name="trigger">Trigger.</param>
        /// <param name="notifyFieldChanged">Indicates if <see cref="EditContext.NotifyFieldChanged"/> should be called.</param>
        protected void ValidateProperty(FieldIdentifier fieldIdentifier, ValidationTrigger trigger = ValidationTrigger.Input, bool notifyValidationStateChanged = true)
        {
            //get validation attribute
            var validationAttribute = GetValidatingPropertyAttribute(fieldIdentifier);

            //since we explicitly set which properties should be validated with the attribute
            //the absense of attribute should mean that we dont need to do any validation
            if (validationAttribute == null)
                return;

            // The following parameters will be available to us 
            // 1) the field identifier will have the property name and object
            // 2) the trigger that defines what have triggered the validation

            //since we revalidating we need to remove the messages associated with the field
            _validationMessageStore.Clear(fieldIdentifier);

            //pass data annotation validation
            DataAnnotationsValidator.Validate(fieldIdentifier, _validationMessageStore);

            //execute any custom validation            
            OnValidate(fieldIdentifier, trigger);
            OnCustomValidation(fieldIdentifier, _validationMessageStore);

            //check if normal validation produced any errors
            //in general we wont need to trigger async validation until those errors resolved
            if (!_validationMessageStore[fieldIdentifier].Any())
            {
                //consider a scenario where we have a username property that needs to be validated asynchronosly, usually this property will have required attribute
                //along with some other validation attributes, if validation was triggered by Validate method initialy and there where no input from the user
                //validation would already fail in previous step, in a scneario where we might allow the value to be null then we probably dont even need to trigger
                //async validation thus the property would be valid

                //TODO : This behaviour might need to be considered more carefully
                //check the validation trigger 
                if (trigger == ValidationTrigger.Input && validationAttribute.IsAsync)
                {
                    //schedule async validation
                    RunAsyncValidation(fieldIdentifier, trigger, notifyValidationStateChanged);
                }
            }

            //notify change if required
            if (notifyValidationStateChanged)
                EditContext.NotifyValidationStateChanged();
        }

        /// <summary>
        /// Schedules and runs async validation.
        /// </summary>
        /// <param name="fieldIdentifier">Field identifier.</param>
        /// <param name="validationTrigger">Trigger.</param>
        /// <param name="notifyValidationStateChanged">Indicates if <see cref="EditContext.NotifyValidationStateChanged"/> should be called once async validation is scheduled.</param>
        private void RunAsyncValidation(FieldIdentifier fieldIdentifier, ValidationTrigger validationTrigger, bool notifyValidationStateChanged = true)
        {
            //previous validation is no longer valid, remove it from validated list
            _asyncValidatedProperties.Remove(fieldIdentifier);

            //create async validation and modify edit context properties

            try
            {
                _asyncValidatingProperties.Add(fieldIdentifier);

                Task.Run(()=>OnValidateAsync(fieldIdentifier, default));

                //once async validation completes we need to remove the property from _asyncValidatingProperties since validation have completed
                //add any errors to the message store and add property to _asyncValidatedProperties since validation on it have been done
            }
            catch
            {
                //remove field identifier if we failed to schedule
                _asyncValidatingProperties.Remove(fieldIdentifier);
            }

            //notify change if required
            if (notifyValidationStateChanged)
                EditContext.NotifyValidationStateChanged();
        }

        /// <summary>
        /// Validates all properties.
        /// </summary>
        /// <remarks>
        /// This function is called upon <see cref="EditContext.Validate"/> request.
        /// </remarks>
        protected void ValidateProperties()
        {
            //get validation information from the view state
            var validationObjects = ValidationInfo.Get(ViewState);

            //process each validating object instance
            foreach (var validationObject in validationObjects)
            {
                foreach (var property in validationObject.Properties)
                {
                    if (validationObject.Instance == null)
                        continue;

                    //validate each individual property on the object instance
                    ValidateProperty(new FieldIdentifier(validationObject.Instance, property.Name), ValidationTrigger.Request, false);
                }
            }

            //once we have validated the properties raise validation state change event            
            _editContext.NotifyValidationStateChanged();
        }        

        /// <summary>
        /// Gets validating property attribute for specified field identifier.
        /// </summary>
        /// <param name="fieldIdentifier">Field identifier.</param>
        /// <returns>Validating property attribute.</returns>
        /// <remarks>
        /// Null will be returned if attibute is not set.
        /// </remarks>
        protected static ValidatingPropertyAttribute? GetValidatingPropertyAttribute(FieldIdentifier fieldIdentifier)
        {
            return fieldIdentifier.Model.GetType()
                .GetProperty(fieldIdentifier.FieldName, BindingFlags.Public | BindingFlags.Instance)
                ?.GetCustomAttribute<ValidatingPropertyAttribute>();
        }

        /// <summary>
        /// Checks if all async validated properties have been validated.
        /// </summary>
        /// <returns>True or false.</returns>
        protected bool IsAsyncPropertiesValidated()
        {
            //TODO : Not the most optimal way
            var instanceInfo = ValidationInfo.Get(ViewState);

            foreach (var info in instanceInfo)
            {
                if (info.Instance == null)
                    continue;

                foreach (var property in info.Properties)
                {
                    var validateAttribute = property.GetCustomAttribute<ValidatingPropertyAttribute>();

                    if (validateAttribute?.IsAsync == true && !_asyncValidatedProperties.Contains(new FieldIdentifier(info.Instance, property.Name)))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Runs validation.
        /// </summary>
        /// <remarks>
        /// Use this method instead of <see cref="EditContext.Validate"/> as this method will not return true or false and 
        /// we need to use <see cref="TViewState.IsValid"/> to check for validity instead.
        /// </remarks>
        protected void Validate()
        {
            EditContext.Validate();
        }

        #region OBSOLETE

        /// <summary>
        /// Validates all validation participating properties on current <see cref="ViewStateServiceBase.ViewState"/>.
        /// </summary>
        protected async Task ValidatePropertiesAsync()
        {
            //get validation information from the view state
            var validationObjects = ValidationInfo.Get(ViewState);

            //process each validating object instance
            foreach (var validationObject in validationObjects)
            {
                foreach (var property in validationObject.Properties)
                {
                    if (validationObject.Instance == null)
                        continue;

                    //validate each individual property on the object instance
                    await ValidatePropertyAsync(new FieldIdentifier(validationObject.Instance, property.Name));
                }
            }

            //once we have validated the properties raise validation state change event            
            _editContext.NotifyValidationStateChanged();
        }

        protected async Task ValidatePropertyAsync(Expression<Func<TViewState, object?>> property)
        {
            MemberExpression body = (MemberExpression)property.Body;
            var propertyName = body.Member.Name;

            await ValidatePropertyAsync(new FieldIdentifier(ViewState, propertyName));
            EditContext.NotifyValidationStateChanged();
        }

        protected async Task ValidatePropertyAsync(FieldIdentifier fieldIdentifier)
        {
            //the field identifier will have the property name and obect

            //since we revalidating we need to remove the messages associated with the field
            _validationMessageStore.Clear(fieldIdentifier);

            //data annotation validation
            DataAnnotationsValidator.Validate(fieldIdentifier, _validationMessageStore);

            //custom validation
            OnCustomValidation(fieldIdentifier, _validationMessageStore);

            await OnCustomValidationAsync(fieldIdentifier, _validationMessageStore);
        }

        protected virtual void OnCustomValidation(FieldIdentifier fieldIdentifier, ValidationMessageStore validationMessageStore)
        {
        }

        protected virtual Task OnCustomValidationAsync(FieldIdentifier fieldIdentifier, ValidationMessageStore validationMessageStore)
        {
            return Task.CompletedTask;
        }

        #endregion

        #endregion

        #region PROTECTED VIRTUAL

        /// <summary>
        /// Does custom validation.
        /// </summary>
        /// <param name="fieldIdentifier">Field identifier.</param>
        /// <param name="validationTrigger">Validation trigger.</param>
        /// <remarks>
        /// This method is always called by <see cref="ValidateProperty(FieldIdentifier, ValidationTrigger, bool)"/> method and responsible of doing custom validation.
        /// </remarks>
        protected virtual void OnValidate(FieldIdentifier fieldIdentifier, ValidationTrigger validationTrigger)
        {
            //do custom validation here
        }

        /// <summary>
        /// Does custom async validation.<br></br>
        /// <b>This function should not be called directly.</b>
        /// </summary>
        /// <param name="fieldIdentifier">Field identifier.</param>
        /// <param name="validationTrigger">Trigger.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// This function will run for any property that have <see cref="ValidatingPropertyAttribute.IsAsync"/> set.<br></br>
        /// This function will run after all data annotation validation rules have passed and will not be executed if any erros are found.<br></br>
        /// This function is only responsible validating the field specified <paramref name="fieldIdentifier"/> and adding any associated errors with <see cref="AddError"/> method.
        /// </remarks>
        protected virtual Task OnValidateAsync(FieldIdentifier fieldIdentifier, ValidationTrigger validationTrigger, CancellationToken cancellationToken =default)
        {  
            //do custom async validation here
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called once <see cref="EditContext.OnValidationStateChanged"/> have been processed and all <see cref="TViewState"/> validation status properties are set.
        /// </summary>
        protected virtual void OnValidationStateChanged()
        {
            //this is helper method can be used to parse current validation state and update any desired view state values
            //for example we can check if async validation is still running for some property and provide visual feedback through view state
            //for exampl ViewState.IsUserNameValidating = true
        }

        #endregion

        #region EVENT HANDLERS

        /// <summary>
        /// Handles edit context validation request.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Args.</param>
        /// <remarks>
        /// This method will only be invoked after <see cref="EditContext.Validate"/> is called.
        /// </remarks>
        private void OnEditContextValidationRequested(object? sender, ValidationRequestedEventArgs e)
        {
            //when validation is requested on the context all view state properties that participate in validation should be revalidated
            //it should not be required to call ResetValidationState() since each individual property will be re-validated

            //revalidate all properties
            ValidateProperties();
        }

        /// <summary>
        /// Handles edit context validation state change event.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Args.</param>
        /// <remarks>
        /// This method will be invoked each time <see cref="EditContext.NotifyValidationStateChanged"/> is called.
        /// </remarks>
        private void OnEditContextValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
        {
            if (!IsAsyncPropertiesValidated())
            {
                //if not all async validations have completed then the state is invalid
                ViewState.IsValid = false;
            }
            //check if any async validations are still running
            else if (_asyncValidatingProperties.Count > 0)
            {
                //if async validations are running mark as isvalidating
                ViewState.IsValidating = true;

                //for as long as async validations are running the object is not valid
                ViewState.IsValid = false;
            }
            else
            {
                //clear is validating if no async validations are running
                ViewState.IsValidating = false;

                //check if any validation errors present in stores
                ViewState.IsValid = !EditContext.GetValidationMessages().Any();

                //not used anywhere yet but lets mark as IsDirty based on any edit context field modification state
                ViewState.IsDirty = EditContext.IsModified();
            }

            //call into an custom implementation
            OnValidationStateChanged();

            //debounce view state change
            DebounceViewStateChanged();
        }

        #endregion

        #region OVERRIDES

        protected override Task OnInitializing(CancellationToken ct)
        {
            _editContext.OnValidationStateChanged += OnEditContextValidationStateChanged;
            _editContext.OnValidationRequested += OnEditContextValidationRequested;

            return base.OnInitializing(ct);
        }

        protected override void OnDisposing(bool dis)
        {
            base.OnDisposing(dis);
            _editContext.OnValidationStateChanged -= OnEditContextValidationStateChanged;
            _editContext.OnValidationRequested -= OnEditContextValidationRequested;
        }

        #endregion
    }
}
