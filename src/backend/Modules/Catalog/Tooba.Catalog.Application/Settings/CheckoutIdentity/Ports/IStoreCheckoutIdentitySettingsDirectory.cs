using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.CheckoutIdentity.Models;

namespace Tooba.Catalog.Application.Settings.CheckoutIdentity.Ports;

/// <summary>Catalog persistence for store checkout-identity Admin settings.</summary>
public interface IStoreCheckoutIdentitySettingsDirectory
{
    /// <summary>Loads effective policy view (defaults if missing).</summary>
    Task<CheckoutIdentitySettingsView> GetAsync(CancellationToken cancellationToken);

    /// <summary>Saves policy (Host coerce: unknown → AuthenticatedOnly).</summary>
    Task<Result<CheckoutIdentitySettingsView>> SaveAsync(
        string? policy,
        CancellationToken cancellationToken);
}
