using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gizmo.UI.Services;
using Gizmo.UI.View.States;
using Microsoft.Extensions.Logging;

namespace Gizmo.UI.View.Services
{
    /// <summary>
    /// Base cart view service.
    /// </summary>
    /// <typeparam name="TViewState">Cart view state.</typeparam>
    /// <remarks>
    /// Shared base cart handling implementation.
    /// </remarks>
    public abstract class CartViewServiceBase<TViewState> : ViewStateServiceBase<TViewState> where TViewState : ICartViewState
    {
        public CartViewServiceBase(TViewState viewState,
            GlobalCancellationService globalCancellationService,
            ILocalizationService localizationService,
            ILogger logger,
            IServiceProvider serviceProvider) : base(viewState, logger, serviceProvider)
        {
            _globalCancellationService = globalCancellationService;
            _localizationService = localizationService;
            _logger = logger;
        }

        protected delegate Task CartRequestHandler(ICartRequest request, CancellationToken cancellationToken);
        protected readonly Dictionary<Type, CartRequestHandler> _requestHandlers = [];

        private readonly ILogger _logger;
        private readonly Subject<ICartRequest> _cartRequestSubject = new();

        protected Guid? _currentCartId;
        protected readonly ILocalizationService _localizationService;
        protected readonly SemaphoreSlim _cartCreateLock = new(1);
        protected readonly GlobalCancellationService _globalCancellationService;

        private CancellationTokenSource? _refreshTokenSource;
        private int _totalPendingRequests;
        private IDisposable? _requestSubscription;

        /// <summary>
        /// Gets current cart id.
        /// </summary>
        /// <remarks>
        /// This value might be null if cart is not created yet.
        /// </remarks>
        public Guid? CurrentCartId => _currentCartId;

        /// <summary>
        /// Adds deposit to cart.
        /// </summary>
        /// <param name="amount">Deposit amount.</param>
        /// <remarks>
        /// This function will add or modify existing deposit for all users present in cart.
        /// </remarks>
        public void AddDeposit(decimal amount) => NotifyRequest(new AddDepositRequest() { Amount = amount });

        /// <summary>
        /// Adds product to cart.
        /// </summary>
        /// <param name="productId">Product id.</param>
        /// <param name="quantity">Product quantity.</param>
        /// <remarks>
        /// The product is added to all users present in cart.
        /// </remarks>
        public void AddProduct(int productId, decimal quantity = 1, string? mark = null) => NotifyRequest(new AddProductRequest() { ProductId = productId, Quantity = quantity, Mark = mark });

        /// <summary>
        /// Sets line entry quantity.
        /// </summary>
        /// <param name="entryId">Line entry id.</param>
        /// <param name="quantity">New quantity.</param>
        public void SetQuantity(Guid entryId, decimal quantity) => NotifyRequest(new SetQuantityRequest() { EntryId = entryId, Quantity = quantity });

        /// <summary>
        /// Sets line entry custom price.
        /// </summary>
        /// <param name="entryId">Line entry id.</param>
        /// <param name="price">Custom price.</param>
        public void SetCustomPrice(Guid entryId, decimal price) => NotifyRequest(new SetCustomPriceRequest() { EntryId = entryId, Price = price });

        /// <summary>
        /// Sets line entry pay type.
        /// </summary>
        /// <param name="entryId">Line entry id.</param>
        /// <param name="payType">Pay type.</param>
        public void SetPayType(Guid entryId, OrderLinePayType payType) => NotifyRequest(new SetPayTypeRequest() { EntryId = entryId, PayType = payType });

        /// <summary>
        /// Removes entry from cart.
        /// </summary>
        /// <param name="entryId">Line entry id.</param>
        public void RemoveEntry(Guid entryId) => NotifyRequest(new RemoveEntryRequest() { EntryId = entryId });

        /// <summary>
        /// Adds payment to the cart.
        /// </summary>
        /// <param name="amount">Payment amount.</param>
        /// <param name="paymentMethodId">Payment method id.</param>
        public void AddPayment(int paymentMethodId, decimal amount) => NotifyRequest(new AddPaymentRequest() { Amount = amount, PaymentMethodId = paymentMethodId });

        /// <summary>
        /// Removes payment from cart.
        /// </summary>
        /// <param name="paymentMethodId"></param>
        public void RemovePayment(int paymentMethodId) => NotifyRequest(new RemovePaymentRequest() { PaymentMethodId = paymentMethodId });

        public void SetInputPromoCode(string value)
        {
            ViewState.PromoCodeViewState.InputPromoCode = value;
            ViewState.PromoCodeViewState.RaiseChanged();
        }

        /// <summary>
        /// Adds promo code to the cart.
        /// </summary>
        /// <param name="promoCode"></param>
        public void AddPromoCode(string promoCode) => NotifyRequest(new AddPomoCodeRequest() { PromoCode = promoCode });

        /// <summary>
        /// Removes promo code from the cart.
        /// </summary>
        public void RemovePromoCode() => NotifyRequest(new RemovePromoCodeRequest());

        /// <summary>
        /// Accepts current cart.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// Accept parameters provided by view state since they will differ between implementations.
        /// </remarks>
        public abstract Task AcceptAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Clears cart.
        /// </summary>
        /// <remarks>
        /// This will remove all user carts from cart.
        /// </remarks>
        public void Clear() => NotifyRequest(new ClearCartRequest());

        /// <summary>
        /// Pushes cart request into processing sequence.
        /// </summary>
        /// <param name="cartRequest">Cart request.</param>
        protected void NotifyRequest(ICartRequest cartRequest)
        {
            _cartRequestSubject.OnNext(cartRequest);
        }

        /// <summary>
        /// When overridden in derived class, validates the cart request.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>If <see langword="false"/> returned ignores request processing.</returns>
        protected abstract Task<bool> ValidateRequestAsync(ICartRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// When overridden in derived class, preprocess request before processing.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Processed request.</returns>
        /// <remarks>
        /// Each cart request will pass through this function and can be used to mutate associated view states making any updates to them immediate without delays, for example when changing quantity we could set the quantity on the line right away and update the rest of the data later.
        /// </remarks>
        protected abstract Task<ICartRequest> PreProcessRequestAsync(ICartRequest request, CancellationToken cancellationToken);

        protected async Task ProcessRequestsAsync(IEnumerable<ICartRequest> requests, CancellationToken cancellationToken = default)
        {
            // its guaranteed that this method will only be called once concurrently by observable sequence

            // keep the total request count we about to process
            int totalRequestCount = requests.Count();

            try
            {
                // cancel any existing refresh state procedure
                _refreshTokenSource?.Cancel();

                // create linked cancellation token
                var globalCancellationToken = _globalCancellationService.GetLinkedCancellationToken(default);

                // group requests by type
                var requestGroups = requests
                    .GroupBy(request => request.GetType());

                // NOTE it might seem intuitive to call HandleRequestErrorAsync here in case of failure but the problem is that
                // we cant enrich CartRequestContext available here, so the only option would be to pass null which is not ideal
                // and that is why each handler is responsible to call HandleRequestErrorAsync on failure

                foreach (var requestGroup in requestGroups)
                {
                    if (TryGetHandler(requestGroup.Key, out var handler))
                    {
                        if (CanCombine(requestGroup.Key))
                        {
                            await handler(requestGroup.First(), globalCancellationToken);
                        }
                        else
                        {
                            foreach (var request in requestGroup)
                            {
                                await handler(request, globalCancellationToken);
                            }
                        }
                    }
                }

                _refreshTokenSource = new CancellationTokenSource();

                // TODO : this can also should be debounced
                await RefreshStateAsync(_refreshTokenSource.Token);
            }
            catch (Exception ex)
            {
                // the exceptions should be logged here, throwing will break the observable sequence
                _logger.LogCritical(ex, "Error processing cart requests.");

                // handle non-recoverable processing error
                await HandleProcessingErrorAsync(ex, cancellationToken);
            }
        }

        protected override Task OnInitializing(CancellationToken ct)
        {
            // map request handlers

            _requestHandlers.TryAdd(typeof(AddProductRequest), (request, cancellationToken) => HandleRequestAsync((AddProductRequest)request, cancellationToken));
            _requestHandlers.TryAdd(typeof(SetQuantityRequest), (request, cancellationToken) => HandleRequestAsync((SetQuantityRequest)request, cancellationToken));
            _requestHandlers.TryAdd(typeof(SetPayTypeRequest), (request, cancellationToken) => HandleRequestAsync((SetPayTypeRequest)request, cancellationToken));
            _requestHandlers.TryAdd(typeof(RemoveEntryRequest), (request, cancellationToken) => HandleRequestAsync((RemoveEntryRequest)request, cancellationToken));
            _requestHandlers.TryAdd(typeof(AddDepositRequest), (request, cancellationToken) => HandleRequestAsync((AddDepositRequest)request, cancellationToken));
            _requestHandlers.TryAdd(typeof(AddPaymentRequest), (request, cancellationToken) => HandleRequestAsync((AddPaymentRequest)request, cancellationToken));
            _requestHandlers.TryAdd(typeof(RemovePaymentRequest), (request, cancellationToken) => HandleRequestAsync((RemovePaymentRequest)request, cancellationToken));
            _requestHandlers.TryAdd(typeof(AddPomoCodeRequest), (request, cancellationToken) => HandleRequestAsync((AddPomoCodeRequest)request, cancellationToken));
            _requestHandlers.TryAdd(typeof(RemovePromoCodeRequest), (request, cancellationToken) => HandleRequestAsync((RemovePromoCodeRequest)request, cancellationToken));
            _requestHandlers.TryAdd(typeof(ClearCartRequest), (request, cancellationToken) => HandleRequestAsync((ClearCartRequest)request, cancellationToken));

            // create batch subscription, this will buffer the requests for desired time and call processing procedure
            // while processing procedure is running any items pushed into sequence will be buffered
            _requestSubscription = _cartRequestSubject
                .Synchronize()
                .AsyncWhereSequential(ValidateRequestAsync)
                .Select(request => Observable.FromAsync((cancellationToken) =>
                {
                    // only validated requests will be pre-processed here

                    // add pending request here so we don't need to call it in processing procedure
                    AddRemovePendingRequest(1);
                    return PreProcessRequestAsync(request, cancellationToken);
                })) // preprocess request
                .Concat()
                .Buffer(TimeSpan.FromMilliseconds(250))
                .Where(batch => batch.Count > 0)
                .Select(requestBatch => Observable.FromAsync(async (cancellationToken) =>
                {
                    await ProcessRequestsAsync(requestBatch, cancellationToken);
                    // remove request count from pending, at this stage even if we failed here they are no longer pending
                    AddRemovePendingRequest(-requestBatch.Count);
                }))
                .Concat()
                .Subscribe();

            return base.OnInitializing(ct);
        }

        protected override void OnDisposing(bool isDisposing)
        {
            _requestSubscription?.Dispose();

            base.OnDisposing(isDisposing);
        }

        /// <summary>
        /// Adds or removes active request count.
        /// </summary>
        /// <param name="count">Count of requests.</param>
        /// <remarks>
        /// Use negative value to remove outstanding requests.
        /// </remarks>
        private void AddRemovePendingRequest(int count)
        {
            var newReq = Interlocked.Add(ref _totalPendingRequests, count) > 0;
            var changed = newReq != ViewState.IsStateUpdateRequired;
            ViewState.IsStateUpdateRequired = newReq;
            if (changed)
            {
                DebounceViewStateChanged();
            }
        }

        /// <summary>
        /// Checks if specified request type can be combined.
        /// </summary>
        /// <param name="type">Request type.</param>
        /// <returns><see langword="true"/> or <see langword="false"/>.</returns>
        /// <remarks>
        /// When we can combine requests of same type into one we only process the first one.
        /// </remarks>
        protected virtual bool CanCombine(Type type)
        {
            return type == typeof(ClearCartRequest);
        }

        /// <summary>
        /// Attempts to get request handler for specified request type.
        /// </summary>
        /// <param name="requestType">Request type.</param>
        /// <param name="handler">Found handler.</param>
        /// <returns><see langword="true"/> or <see langword="false"/>.</returns>
        protected virtual bool TryGetHandler(Type requestType, [NotNullWhen(true)] out CartRequestHandler? handler)
        {
            return _requestHandlers.TryGetValue(requestType, out handler);
        }

        /// <summary>
        /// Responsible of handling single request processing error.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <param name="request">Request.</param>
        /// <param name="cartRequestContext">Request context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        /// <remarks>
        /// <b>This function must throw any exceptions that are not recoverable, for example invalid cart id or others that will block us from working with cart.</b>
        /// </remarks>
        protected abstract ValueTask HandleRequestErrorAsync(Exception exception, ICartRequest request, CartRequestContext cartRequestContext, CancellationToken cancellationToken = default);

        /// <summary>
        /// Responsible of handling processing error.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        /// <remarks>
        /// <b>This function will be called once an non-recoverable exception is thrown and should not throw any exceptions itself.
        /// </b>
        /// </remarks>
        protected abstract ValueTask HandleProcessingErrorAsync(Exception exception, CancellationToken cancellationToken = default);

        /// <summary>
        /// When overridden in derived class, refreshes the cart state.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected abstract Task RefreshStateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// When overridden in derived class, gets or creates cart.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task<Guid> CartGetOrCreateAsync(CancellationToken cancellationToken = default);

        protected abstract Task HandleRequestAsync(AddProductRequest request, CancellationToken cancellationToken = default);

        protected abstract Task HandleRequestAsync(SetQuantityRequest request, CancellationToken cancellationToken = default);

        protected abstract Task HandleRequestAsync(SetPayTypeRequest request, CancellationToken cancellationToken = default);

        protected abstract Task HandleRequestAsync(AddDepositRequest request, CancellationToken cancellationToken = default);

        protected abstract Task HandleRequestAsync(RemoveEntryRequest request, CancellationToken cancellationToken = default);

        protected abstract Task HandleRequestAsync(AddPaymentRequest request, CancellationToken cancellationToken = default);

        protected abstract Task HandleRequestAsync(RemovePaymentRequest request, CancellationToken cancellationToken = default);

        protected abstract Task HandleRequestAsync(AddPomoCodeRequest request, CancellationToken cancellationToken = default);

        protected abstract Task HandleRequestAsync(RemovePromoCodeRequest request, CancellationToken cancellationToken = default);

        protected abstract Task HandleRequestAsync(ClearCartRequest request, CancellationToken cancellationToken = default);

        #region REQUESTS

        /// <summary>
        /// Generic cart request contract.
        /// </summary>
        public interface ICartRequest
        {
        }

        /// <summary>
        /// Cart request context.
        /// </summary>
        /// <remarks>
        /// Provides additional context for request handling.
        /// </remarks>
        protected sealed class CartRequestContext
        {
            /// <summary>
            /// User id.
            /// </summary>
            public int? UserId { get; init; }

            /// <summary>
            /// Cart entry id.
            /// </summary>
            public Guid? EntryId { get; init; }

            /// <summary>
            /// Payment method id.
            /// </summary>
            public int? PaymentMethodId { get; init; }
        }

        protected sealed class AddProductRequest : ICartRequest
        {
            /// <summary>
            /// Product id.
            /// </summary>
            public int ProductId { get; init; }

            /// <summary>
            /// Product quantity.
            /// </summary>
            public decimal Quantity { get; init; }

            /// <summary>
            /// Product mark.
            /// </summary>
            public string? Mark { get; init; }
        }

        protected sealed class SetCustomPriceRequest : ICartRequest
        {
            /// <summary>
            /// Entry id.
            /// </summary>
            public required Guid EntryId { get; init; }

            /// <summary>
            /// Custom price.
            /// </summary>
            public required decimal Price { get; init; }
        }

        protected sealed class SetQuantityRequest : ICartRequest
        {
            /// <summary>
            /// Cart entry id.
            /// </summary>
            public required Guid EntryId { get; init; }

            /// <summary>
            /// Desired quantity.
            /// </summary>
            public decimal Quantity { get; init; }
        }

        protected sealed class SetPayTypeRequest : ICartRequest
        {
            /// <summary>
            /// Entry id.
            /// </summary>
            public required Guid EntryId { get; init; }

            /// <summary>
            /// Pay type.
            /// </summary>
            public required OrderLinePayType PayType { get; init; }
        }

        protected sealed class AddDepositRequest : ICartRequest
        {
            public decimal Amount { get; init; }
        }

        protected sealed class RemoveEntryRequest : ICartRequest
        {
            /// <summary>
            /// Entry id.
            /// </summary>
            public required Guid EntryId { get; init; }
        }

        protected sealed class AddPaymentRequest : ICartRequest
        {
            /// <summary>
            /// Payment amount.
            /// </summary>
            public required decimal Amount { get; init; }

            /// <summary>
            /// Received amount.
            /// </summary>
            public decimal? ReceivedAmount { get; init; }

            /// <summary>
            /// Payment method id.
            /// </summary>
            public required int PaymentMethodId { get; init; }
        }

        protected sealed class RemovePaymentRequest : ICartRequest
        {
            /// <summary>
            /// Payment method id.
            /// </summary>
            public required int PaymentMethodId { get; init; }
        }

        protected sealed class AddPomoCodeRequest : ICartRequest
        {
            /// <summary>
            /// Promo code value.
            /// </summary>
            public required string PromoCode { get; init; }
        }

        protected sealed class RemovePromoCodeRequest : ICartRequest
        {
        }

        protected sealed class ClearCartRequest : ICartRequest
        {
        }

        #endregion
    }
}
