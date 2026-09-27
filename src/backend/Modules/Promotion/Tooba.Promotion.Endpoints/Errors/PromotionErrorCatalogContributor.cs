using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Promotion.Application.Errors;
using Tooba.Promotion.Application.Merchandising.Admin;

namespace Tooba.Promotion.Endpoints.Errors;

public sealed class PromotionErrorCatalogContributor : IErrorCatalogContributor
{
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(PromotionErrorCodes.Missing, ErrorClassification.NotFound, 404, "Not Found"),
        D(PromotionErrorCodes.NameRequired, ErrorClassification.Validation, 400, "Bad Request"),
        D(PromotionErrorCodes.CouponRequired, ErrorClassification.Validation, 400, "Bad Request"),
        D(PromotionErrorCodes.MutationRejected, ErrorClassification.Business, 400, "Bad Request"),
        D(PromotionErrorCodes.ActivateRejected, ErrorClassification.Business, 400, "Bad Request"),
        D(PromotionErrorCodes.DeactivateRejected, ErrorClassification.Business, 400, "Bad Request"),
        D(MerchandisingAdminErrorCodes.Missing, ErrorClassification.NotFound, 404, "Not Found"),
        D(MerchandisingAdminErrorCodes.Validation, ErrorClassification.Validation, 400, "Bad Request"),
        D(MerchandisingAdminErrorCodes.Publish, ErrorClassification.Validation, 400, "Bad Request"),
        D(MerchandisingAdminErrorCodes.Member, ErrorClassification.Validation, 400, "Bad Request"),
        D(MerchandisingAdminErrorCodes.Reorder, ErrorClassification.Validation, 400, "Bad Request"),
        D(MerchandisingAdminErrorCodes.Price, ErrorClassification.Validation, 400, "Bad Request"),
    ];

    private static ErrorDescriptor D(string c, ErrorClassification k, int s, string f) =>
        new(c, k, s, c, ErrorSeverity.Warning, f);
}
