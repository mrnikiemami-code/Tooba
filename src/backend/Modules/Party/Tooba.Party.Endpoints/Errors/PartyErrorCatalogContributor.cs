using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Party.Application.Seller;

namespace Tooba.Party.Endpoints.Errors;

/// <summary>
/// کاتالوگ صریح کدهای خطای مالک Party برای مسیرهای تنظیمات فروشنده.
/// <para>
/// <c>seller.authorization.denied</c> کد عرضی مشترک است و مالک توصیف/محلی‌سازی آن
/// <c>FoundationErrorCatalogContributor</c> است؛ Party آن را تکرار ثبت نمی‌کند.
/// </para>
/// </summary>
public sealed class PartyErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(PartySellerSettingsErrorCodes.Missing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Not Found"),
        D(PartySellerSettingsErrorCodes.Rejected, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Rejected"),
    ];

    private static ErrorDescriptor D(
        string code,
        ErrorClassification classification,
        int status,
        string fallback) =>
        new(
            Code: code,
            Classification: classification,
            HttpStatus: status,
            LocalizationKey: code,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: fallback);
}
