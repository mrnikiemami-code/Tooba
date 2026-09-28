namespace Tooba.Wallet.Application.Errors;

/// <summary>Stable Wallet semantic error codes for HTTP/use-case outcomes.</summary>
public static class WalletErrorCodes
{
    public const string CustomerSessionRequired = "customer.session.required";
    public const string WalletRejected = "wallet.rejected";
    public const string RedeemRejected = "wallet.redeem.rejected";
    public const string GiftCardRejected = "giftcard.rejected";
    public const string GiftCardIssueRejected = "giftcard.issue.rejected";
    public const string GiftCardRevokeRejected = "giftcard.revoke.rejected";
    public const string GiftCardMissing = "giftcard.missing";
    public const string WalletMissing = "wallet.missing";
    public const string AdjustRejected = "wallet.adjust.rejected";

    /// <summary>
    /// Authorization service unavailable; admin capability must fail closed (503).
    /// The canonical Host-facing authority for this admin-auth code is
    /// <c>Tooba.Wallet.Endpoints.Admin.WalletAdminAuthorizationCodes.AuthorizationUnavailable</c>,
    /// colocated with <c>IWalletAdminAuthorizer</c> so Host never references this Application project.
    /// </summary>
    public const string AuthorizationUnavailable = "wallet.authorization.unavailable";
    public const string DemoNotReady = "wallet.demo.not_ready";
}
