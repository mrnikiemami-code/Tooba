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
    public const string AdminAuthorizationDenied = "admin.authorization.denied";
    public const string DemoNotReady = "wallet.demo.not_ready";
}
