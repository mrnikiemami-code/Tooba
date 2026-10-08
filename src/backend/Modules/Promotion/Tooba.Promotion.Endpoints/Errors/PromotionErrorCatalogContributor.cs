using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Promotion.Contracts.Errors;

namespace Tooba.Promotion.Endpoints.Errors;

/// <summary>
/// Promotion's contribution to the composed error catalog. Every descriptor is a Promotion-owned stable
/// code (<c>promotion.*</c> / <c>merchandising.*</c> / <c>campaign.*</c>) with
/// <c>LocalizationKey = code</c>; the user-facing text is owned by
/// <see cref="PromotionErrorResourceSet"/> so no Promotion key falls back to a generic title.
/// Transport validation codes (<c>promotion.validation.*</c>) are deliberately not catalogued — they
/// travel inside the canonical <c>validation.failed</c> envelope. Cross-cutting
/// <c>seller.authorization.denied</c> / <c>admin.authorization.denied</c> belong to Foundation and are
/// deliberately not registered here.
/// </summary>
public sealed class PromotionErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(PromotionErrorCodes.Missing, ErrorClassification.NotFound, 404, "Not Found"),
        D(PromotionErrorCodes.NameRequired, ErrorClassification.Validation, 400, "Bad Request"),
        D(PromotionErrorCodes.CouponRequired, ErrorClassification.Validation, 400, "Bad Request"),
        D(PromotionErrorCodes.MutationRejected, ErrorClassification.Business, 400, "Bad Request"),
        D(PromotionErrorCodes.ActivateRejected, ErrorClassification.Business, 400, "Bad Request"),
        D(PromotionErrorCodes.DeactivateRejected, ErrorClassification.Business, 400, "Bad Request"),
        D(PromotionErrorCodes.MerchandisingCampaignMissing, ErrorClassification.NotFound, 404, "Not Found"),
        D(PromotionErrorCodes.CampaignValidation, ErrorClassification.Validation, 400, "Bad Request"),
        D(PromotionErrorCodes.CampaignPublish, ErrorClassification.Validation, 400, "Bad Request"),
        D(PromotionErrorCodes.CampaignMember, ErrorClassification.Validation, 400, "Bad Request"),
        D(PromotionErrorCodes.CampaignReorder, ErrorClassification.Validation, 400, "Bad Request"),
        D(PromotionErrorCodes.CampaignPrice, ErrorClassification.Validation, 400, "Bad Request"),
        D(PromotionErrorCodes.OutboxUnmappedEventType, ErrorClassification.Platform, 500, "Internal Server Error"),
    ];

    private static ErrorDescriptor D(string c, ErrorClassification k, int s, string f) =>
        new(c, k, s, c, ErrorSeverity.Warning, f);
}
