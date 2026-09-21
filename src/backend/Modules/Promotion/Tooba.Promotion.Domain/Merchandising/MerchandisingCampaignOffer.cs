

namespace Tooba.Promotion.Domain.Merchandising;

/// <summary>
/// عضویت SellerOffer در کمپین. پرچم روی Offer نیست و موجودی/قیمت پایه را مالک نیست.
/// </summary>
public sealed class MerchandisingCampaignOffer
{
    /// <summary>سازندهٔ EF.</summary>
    private MerchandisingCampaignOffer()
    {
    }

    /// <summary>شناسهٔ UuidV7.</summary>
    public Guid Id { get; init; }

    /// <summary>کمپین.</summary>
    public Guid CampaignId { get; init; }

    /// <summary>شناسهٔ SellerOffer؛ بدون FK به schema offer.</summary>
    public Guid SellerOfferId { get; init; }

    /// <summary>ترتیب نمایش داخل کمپین.</summary>
    public int SortOrder { get; private set; }

    /// <summary>ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// عضویت می‌سازد. فیلد تخصیص/سقف کمپین عمداً نیست — سقف سفارش روی Offer است.
    /// </summary>
    public static MerchandisingCampaignOffer Create(
        Guid id,
        Guid campaignId,
        Guid sellerOfferId,
        int sortOrder,
        DateTimeOffset now)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("promotion.campaign_offer.id_required");
        }

        if (campaignId == Guid.Empty)
        {
            throw new InvalidOperationException("promotion.campaign.id_required");
        }

        if (sellerOfferId == Guid.Empty)
        {
            throw new InvalidOperationException("promotion.campaign_offer.offer_required");
        }

        return new MerchandisingCampaignOffer
        {
            Id = id,
            CampaignId = campaignId,
            SellerOfferId = sellerOfferId,
            SortOrder = sortOrder,
            CreatedAt = now,
        };
    }

    /// <summary>
    /// ترتیب را عوض می‌کند.
    /// </summary>
    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
