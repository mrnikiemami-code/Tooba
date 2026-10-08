using Tooba.Promotion.Contracts.Errors;

namespace Tooba.Promotion.Application.Merchandising.Admin;

/// <summary>
/// Merchandising-campaign Admin use-case outcome aliases. These are thin aliases onto the single
/// canonical Promotion stable-code surface (<see cref="PromotionErrorCodes"/>) so the module has exactly
/// one declared-code home and one literal per code; they exist only to keep the Admin handlers readable.
/// No literal code string may be re-inlined in Application/Domain.
/// </summary>
public static class MerchandisingAdminErrorCodes
{
    /// <summary>کمپین در فروشگاه حل‌شده پیدا نشد.</summary>
    public const string Missing = PromotionErrorCodes.MerchandisingCampaignMissing;

    /// <summary>payload کمپین قاعدهٔ شکل کسب‌وکار را رد کرد.</summary>
    public const string Validation = PromotionErrorCodes.CampaignValidation;

    /// <summary>کمپین در وضعیت فعلی قابل انتشار نیست.</summary>
    public const string Publish = PromotionErrorCodes.CampaignPublish;

    /// <summary>عملیات عضو کمپین رد شد.</summary>
    public const string Member = PromotionErrorCodes.CampaignMember;

    /// <summary>بازچینش اعضای کمپین رد شد.</summary>
    public const string Reorder = PromotionErrorCodes.CampaignReorder;

    /// <summary>قیمت عضو کمپین رد شد.</summary>
    public const string Price = PromotionErrorCodes.CampaignPrice;
}
