using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.CheckoutAbuse.Models;

namespace Tooba.Catalog.Application.Settings.CheckoutAbuse.Ports;

/// <summary>Catalog persistence for store checkout-abuse Admin settings.</summary>
public interface IStoreCheckoutAbuseSettingsDirectory
{
    /// <summary>Loads settings or defaults.</summary>
    Task<CheckoutAbuseSettingsView> GetAsync(CancellationToken cancellationToken);

    /// <summary>Saves settings and reservation-policy audit fields.</summary>
    Task<Result<CheckoutAbuseSettingsView>> SaveAsync(
        int maxOpenUnpaidOrdersPerCustomer,
        int reservationCommitWindowMinutes,
        int maxCheckoutCommitsPerCustomerInWindow,
        Guid actorUserId,
        CancellationToken cancellationToken);
}
