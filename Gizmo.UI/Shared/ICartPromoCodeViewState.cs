namespace Gizmo.UI.View.States
{
    public interface ICartPromoCodeViewState : IViewState
    {
        string? Description { get; set; }

        IEnumerable<string> DiscountNames { get; set; }
        
        string InputPromoCode { get; set; }
        
        bool IsLoading { get; set; }
        
        string? Name { get; set; }
        
        string? PromoCode { get; set; }
        
        int? PromoCodeId { get; set; }
    }
}
