namespace Gizmo.UI.View.States
{
    /// <summary>
    /// Cart view state.
    /// </summary>
    /// <remarks>
    /// Shared implementation contract for manager, client etc.
    /// </remarks>
    public interface ICartViewState : IViewState
    {
        /// <summary>
        /// Indicates that state update required.
        /// </summary>
        public bool IsStateUpdateRequired { get; set; }

        /// <summary>
        /// Indicates that state update is currently ongoing.
        /// </summary>
        public bool IsStateUpdating { get; }

        /// <summary>
        /// Cart points total.
        /// </summary>
        public int PointsTotal { get; }

        /// <summary>
        /// Cart sub total.
        /// </summary>
        public decimal SubTotal { get; }

        /// <summary>
        /// Tax total.
        /// </summary>
        public decimal TaxTotal { get; }

        /// <summary>
        /// Fee total.
        /// </summary>
        public decimal FeeTotal { get; }

        /// <summary>
        /// Cart discount.
        /// </summary>
        public decimal Discount { get; }

        /// <summary>
        /// Total.
        /// </summary>
        public decimal Total { get; }

        /// <summary>
        /// Points award.
        /// </summary>
        public int PointsAward { get; }

        public ICartPromoCodeViewState PromoCodeViewState { get; }
    }
}
