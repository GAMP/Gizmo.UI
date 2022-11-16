using Gizmo.UI.Services;
using Gizmo.UI.View.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gizmo.UI.View.Services
{
    [Register()]
    public sealed class DialogHostViewStateService : ViewStateServiceBase<DialogHostViewState>
    {
        public DialogHostViewStateService(IDialogService dialogService,
            DialogHostViewState viewState, 
            ILogger<DialogHostViewStateService> logger,
            IServiceProvider serviceProvider)
            :base(viewState,logger,serviceProvider )
        {
            _dialogService= dialogService;
            _dialogService.DialogChanged += OnDialogServiceChanged;
        }

        #region FIELDS
        readonly IDialogService _dialogService;
        #endregion

        protected override Task OnInitializing(CancellationToken ct)
        {
            return base.OnInitializing(ct);
        }

        protected override void OnDisposing(bool isDisposing)
        {
            base.OnDisposing(isDisposing);

            _dialogService.DialogChanged -= OnDialogServiceChanged;
        }

        private void OnDialogServiceChanged(object? sender, EventArgs e)
        {
            if(_dialogService.TryPeek(out var dialog)) 
            {
                ViewState.Current = dialog;
            }
            else
            {
                ViewState.Current = null;
            }

            ViewState.RaiseChanged();
        }
    }
}
