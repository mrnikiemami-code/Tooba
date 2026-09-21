using Microsoft.EntityFrameworkCore;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Domain.Aggregates;
using Tooba.Payment.Infrastructure.Persistence;

namespace Tooba.Payment.Infrastructure.Adapters;

/// <summary>پیاده‌سازی تنظیمات مهلت روش پرداخت روی schema payment.</summary>
public sealed class PaymentHoldSettingsDirectory : IPaymentHoldSettingsDirectory
{
    private readonly PaymentDbContext _db;

    /// <summary>دایرکتوری تنظیمات مهلت را به schema payment وصل می‌کند.</summary>
    public PaymentHoldSettingsDirectory(PaymentDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentMethodHoldOverrideDto>> ListMethodOverridesAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.MethodHoldOverrides.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(x => new PaymentMethodHoldOverrideDto(
            x.ProviderCode,
            x.OnlinePaymentHoldHours,
            x.ManualPaymentInitialHoldHours,
            x.ManualPaymentReviewHoldHours)).ToList();
    }

    /// <inheritdoc />
    public async Task UpsertMethodOverrideAsync(
        string providerCode,
        int? onlinePaymentHoldHours,
        int? manualPaymentInitialHoldHours,
        int? manualPaymentReviewHoldHours,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var code = (providerCode ?? string.Empty).Trim().ToLowerInvariant();
        if (code.Length == 0)
        {
            return;
        }

        var row = await _db.MethodHoldOverrides
            .SingleOrDefaultAsync(x => x.ProviderCode == code, cancellationToken)
            .ConfigureAwait(false);
        if (row is null)
        {
            row = PaymentMethodHoldOverride.Create(code, now);
            _db.MethodHoldOverrides.Add(row);
        }

        row.Replace(onlinePaymentHoldHours, manualPaymentInitialHoldHours, manualPaymentReviewHoldHours, now);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
