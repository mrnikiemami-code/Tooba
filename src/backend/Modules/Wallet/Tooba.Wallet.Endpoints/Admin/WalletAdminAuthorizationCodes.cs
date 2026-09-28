namespace Tooba.Wallet.Endpoints.Admin;

/// <summary>
/// Canonical Wallet-owned admin authorization code surface, colocated with
/// <see cref="IWalletAdminAuthorizer"/> so the Host admin adapter can consume the module's
/// auth-failure code without referencing Tooba.Wallet.Application.
/// </summary>
/// <remarks>
/// The 403 denial re-states the shared cross-cutting <c>admin.authorization.denied</c> code so the
/// module's auth surface is self-describing at the seam; its single canonical descriptor authority
/// remains <c>Tooba.BuildingBlocks.Presentation.Errors.FoundationErrorCodes</c>.
/// </remarks>
public static class WalletAdminAuthorizationCodes
{
    /// <summary>
    /// Denial code shared with the Foundation catalog; canonical authority is
    /// <c>Tooba.BuildingBlocks.Presentation.Errors.FoundationErrorCodes.AdminAuthorizationDenied</c>.
    /// </summary>
    public const string AdminAuthorizationDenied = "admin.authorization.denied";

    /// <summary>سرویس مجوز در دسترس نیست؛ مسیر admin کیف پول باید fail-closed بماند (503).</summary>
    public const string AuthorizationUnavailable = "wallet.authorization.unavailable";
}
