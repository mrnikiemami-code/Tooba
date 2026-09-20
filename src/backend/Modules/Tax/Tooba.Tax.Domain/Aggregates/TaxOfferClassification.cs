using Tooba.BuildingBlocks;

namespace Tooba.Tax.Domain;

/// <summary>
/// انتساب طبقه به Offer. نرخ و مبلغ مالیات اینجا ذخیره نمی‌شود.
/// </summary>
public sealed class TaxOfferClassification
{
    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private TaxOfferClassification()
    {
    }

    /// <summary>
    /// Offer طرف قرارداد؛ FK به schema offer نیست.
    /// </summary>
    public Guid OfferId { get; init; }

    /// <summary>
    /// طبقهٔ مالیاتی مات.
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// انتساب می‌سازد.
    /// </summary>
    public static TaxOfferClassification Assign(Guid offerId, Guid categoryId) =>
        new()
        {
            OfferId = offerId,
            CategoryId = categoryId,
        };
}
