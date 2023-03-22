using Gizmo.UI.View.States;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reactive.Linq;

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
            _validationMessageStore = new ValidationMessageStore(_editContext);
        }
        #endregion

        #region READ ONLY FIELDS
        private readonly EditContext _editContext;
        private readonly ValidationMessageStore _validationMessageStore;
        #endregion

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
        /// * Mark edit context as unmodified.<br></br>
        /// * Call <see cref="EditContext.NotifyValidationStateChanged"/> function on current edit context.<br></br>
        /// </summary>
        protected void ResetValidationErrors()
        {
            _validationMessageStore.Clear();
            _editContext.MarkAsUnmodified();
            _editContext.NotifyValidationStateChanged();
        }

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

        protected void ValidateProperty(Expression<Func<TViewState, string?>> property)
        {
            MemberExpression body = (MemberExpression)property.Body;
            var propertyName = body.Member.Name;

            ValidateProperty(new FieldIdentifier(ViewState, propertyName));
            EditContext.NotifyValidationStateChanged();
        }

        protected async Task ValidatePropertyAsync(Expression<Func<TViewState, string?>> property)
        {
            MemberExpression body = (MemberExpression)property.Body;
            var propertyName = body.Member.Name;

            await ValidatePropertyAsync(new FieldIdentifier(ViewState, propertyName));
            EditContext.NotifyValidationStateChanged();
        }

        protected void ValidateProperty(FieldIdentifier fieldIdentifier)
        {
            //the field identifier will have the property name and obect

            //since we revalidating we need to remove the messages associated with the field
            _validationMessageStore.Clear(fieldIdentifier);

            //data annotation validation
            DataAnnotationsValidator.Validate(fieldIdentifier, _validationMessageStore);

            //custom validation
            OnCustomValidation(fieldIdentifier, _validationMessageStore);
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

        /// <summary>
        /// Mark specified property as modified and returns associated <see cref="FieldIdentifier"/>.
        /// </summary>
        /// <param name="model">Owning model.</param>
        /// <param name="propertyName">Property name.</param>
        /// <returns>Filed identifier.</returns>
        protected FieldIdentifier MarkModified(object model, string propertyName)
        {
            //get property field, here we could use some kind of caching <object,property>
            var fieldIdentifier = new FieldIdentifier(model, propertyName);

            //check if property is marked as modified already
            //this should only occur if the property was updated from an InputComponent that is aware of EditContext
            if (!EditContext.IsModified(fieldIdentifier))
            {
                //mark as modified and raise FieldChanged on EditContext
                //if for some reason we need to have consistent FieldChanged event this migtht need to be called every time
                EditContext.NotifyFieldChanged(fieldIdentifier);
            }

            return fieldIdentifier;
        }

        #endregion

        #region PROTECTED VIRTUAL

        protected virtual void OnCustomValidation(FieldIdentifier fieldIdentifier, ValidationMessageStore validationMessageStore)
        {

        }

        protected virtual Task OnCustomValidationAsync(FieldIdentifier fieldIdentifier, ValidationMessageStore validationMessageStore)
        {
            return Task.CompletedTask;
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
        private async void OnEditContextValidationRequested(object? sender, ValidationRequestedEventArgs e)
        {
            //when validation is requested on the context all view state properties that participate in validation should be revalidated

            //it should not be required to call ResetValidationErrors() since each individual property will be re-validated

            //revalidate all properties
            await ValidatePropertiesAsync();
        }

        private void OnEditContextValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
        {
            //here we could have an state object that would indicate that an async validation is currently running so it would help determine
            //if the state is valid or not, just a example for now
            _editContext.Properties.TryGetValue("IsAsyncValidationRunning", out object? value);

            using (ViewStateChangeDebounced())
            {
                ViewState.IsValid = !EditContext.GetValidationMessages().Any();
                ViewState.IsDirty = EditContext.IsModified();
            }
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
