using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Wallet.Application.Errors;

namespace Tooba.Wallet.Endpoints.Errors;

/// <summary>Explicit Wallet error catalog for ApiResponseFactory.</summary>
public sealed class WalletErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(WalletErrorCodes.CustomerSessionRequired, ErrorClassification.Forbidden, StatusCodes.Status401Unauthorized, "Unauthorized"),
        D(WalletErrorCodes.WalletRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(WalletErrorCodes.RedeemRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(WalletErrorCodes.GiftCardRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(WalletErrorCodes.GiftCardIssueRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(WalletErrorCodes.GiftCardRevokeRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(WalletErrorCodes.GiftCardMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Not Found"),
        D(WalletErrorCodes.WalletMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Not Found"),
        D(WalletErrorCodes.AdjustRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(WalletErrorCodes.AdminAuthorizationDenied, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden, "Forbidden"),
        D(WalletErrorCodes.DemoNotReady, ErrorClassification.Platform, StatusCodes.Status503ServiceUnavailable, "Wallet demo seed not ready"),
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
