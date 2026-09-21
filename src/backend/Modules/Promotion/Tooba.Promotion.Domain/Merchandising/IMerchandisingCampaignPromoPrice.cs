

namespace Tooba.Promotion.Domain.Merchandising;

/// <summary>
/// نقطهٔ توسعه‌ٔ رزروشده برای قیمت پروموشن کمپین.
/// ذخیره‌سازی قیمت کمپین عمداً به تعویق افتاده است: <c>AuthoredPrice.QualifierKind</c>
/// فعلاً فقط Base است و بدون بازطراحی Pricing نمی‌توان qualifier کمپین افزود.
/// اسکالر <c>PromoAmount</c> بدون ارز/ابعاد ممنوع است.
/// </summary>
public interface IMerchandisingCampaignPromoPrice
{
}
