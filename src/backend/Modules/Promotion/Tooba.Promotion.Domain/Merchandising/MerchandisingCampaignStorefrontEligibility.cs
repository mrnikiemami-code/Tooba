using Tooba.Promotion.Contracts.Merchandising;

namespace Tooba.Promotion.Domain.Merchandising;

/// <summary>
/// قاعدهٔ Promotion-owned واجدشرایطی نمایش در source فروشگاهی «پیشنهاد شگفت‌انگیز».
/// این قاعده پیش‌تر در اسمبلی Contracts قرار داشت که مالکیت Domain را جعل می‌کرد؛ حالا روی مدل
/// زمان‌اجرای Contracts کار می‌کند ولی در Domain زندگی می‌کند. یک گزارهٔ خالص است و وضعیت ذخیره‌شده ندارد.
/// </summary>
public static class MerchandisingCampaignStorefrontEligibility
{
    /// <summary>
    /// عضو قابل‌فروش + موجود + قیمت کمپین معتبر اکیداً کمتر از قیمت عادی.
    /// </summary>
    public static bool IsAmazingRailEligible(MerchandisingCampaignMemberRuntimeModel member) =>
        member.IsMarketable
        && member.AvailableQuantity > 0
        && member.PriceAmount is decimal selling and > 0
        && member.CompareAtAmount is decimal compareAt and > 0
        && selling < compareAt;
}
