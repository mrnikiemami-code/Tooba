using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.CheckoutAbuse.Models;
using Tooba.Catalog.Application.Settings.CheckoutAbuse.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>Catalog persistence for store checkout-abuse Admin settings.</summary>
public sealed class StoreCheckoutAbuseSettingsDirectory : IStoreCheckoutAbuseSettingsDirectory
{
    private readonly CatalogDbContext _catalog;
    private readonly IClock _clock;

    /// <summary>Creates the directory.</summary>
    public StoreCheckoutAbuseSettingsDirectory(CatalogDbContext catalog, IClock clock)
    {
        _catalog = catalog;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<CheckoutAbuseSettingsView> GetAsync(CancellationToken cancellationToken)
    {
        var row = await _catalog.StoreCheckoutAbuseSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreCheckoutAbuseSettings.SingletonId, cancellationToken);
        return ToView(row);
    }

    /// <inheritdoc />
    public async Task<Result<CheckoutAbuseSettingsView>> SaveAsync(
        int maxOpenUnpaidOrdersPerCustomer,
        int reservationCommitWindowMinutes,
        int maxCheckoutCommitsPerCustomerInWindow,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var row = await _catalog.StoreCheckoutAbuseSettings
            .SingleOrDefaultAsync(x => x.SettingsId == StoreCheckoutAbuseSettings.SingletonId, cancellationToken);
        if (row is null)
        {
            row = StoreCheckoutAbuseSettings.CreateDefault(now);
            _catalog.StoreCheckoutAbuseSettings.Add(row);
        }

        var oldOpen = row.MaxOpenUnpaidOrdersPerCustomer;
        var oldWindow = row.ReservationCommitWindowMinutes;
        var oldCommits = row.MaxCheckoutCommitsPerCustomerInWindow;

        try
        {
            row.Replace(
                maxOpenUnpaidOrdersPerCustomer,
                reservationCommitWindowMinutes,
                maxCheckoutCommitsPerCustomerInWindow,
                now);
        }
        catch (InvalidOperationException ex)
        {
            var code = ex.Message switch
            {
                "settings.max_open_unpaid.invalid" => CatalogErrorCodes.CheckoutAbuseMaxOpenUnpaidInvalid,
                "settings.reservation_commit_window.invalid" => CatalogErrorCodes.CheckoutAbuseWindowInvalid,
                "settings.max_checkout_commits.invalid" => CatalogErrorCodes.CheckoutAbuseMaxCommitsInvalid,
                _ => CatalogErrorCodes.CheckoutAbuseInvalid,
            };
            return Result.Failure<CheckoutAbuseSettingsView>(new SemanticError(code));
        }

        RecordField("MaxOpenUnpaidOrdersPerCustomer", oldOpen, row.MaxOpenUnpaidOrdersPerCustomer, actorUserId, now);
        RecordField("ReservationCommitWindowMinutes", oldWindow, row.ReservationCommitWindowMinutes, actorUserId, now);
        RecordField("MaxCheckoutCommitsPerCustomerInWindow", oldCommits, row.MaxCheckoutCommitsPerCustomerInWindow, actorUserId, now);
        await _catalog.SaveChangesAsync(cancellationToken);
        return Result.Success(ToView(row));
    }

    private void RecordField(string field, int oldValue, int newValue, Guid actorUserId, DateTimeOffset now)
    {
        if (oldValue == newValue)
        {
            return;
        }

        _catalog.ReservationPolicyAuditEvents.Add(
            ReservationPolicyAuditEvent.Create("store", null, field, oldValue, newValue, actorUserId, now));
    }

    private static CheckoutAbuseSettingsView ToView(StoreCheckoutAbuseSettings? row) =>
        new(
            row?.MaxOpenUnpaidOrdersPerCustomer ?? StoreCheckoutAbuseSettings.DefaultMaxOpenUnpaid,
            row?.ReservationCommitWindowMinutes ?? StoreCheckoutAbuseSettings.DefaultWindowMinutes,
            row?.MaxCheckoutCommitsPerCustomerInWindow ?? StoreCheckoutAbuseSettings.DefaultMaxCommits,
            StoreCheckoutAbuseSettings.MinValue,
            StoreCheckoutAbuseSettings.MaxOpenUnpaidCap,
            StoreCheckoutAbuseSettings.MinValue,
            StoreCheckoutAbuseSettings.MaxWindowMinutesCap,
            StoreCheckoutAbuseSettings.MinValue,
            StoreCheckoutAbuseSettings.MaxCommitsCap);
}
