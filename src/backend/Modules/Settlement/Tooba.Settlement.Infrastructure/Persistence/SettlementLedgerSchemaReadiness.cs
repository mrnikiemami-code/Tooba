using Microsoft.EntityFrameworkCore;
using Tooba.Settlement.Application.Payouts.Ports;

namespace Tooba.Settlement.Infrastructure.Persistence;

/// <summary>
/// آمادگی relation پایدار ledger هزینهٔ ارسال. سنجش با یک پرس‌وجوی scalar روی <c>to_regclass</c> انجام
/// می‌شود که برای schema/relation غایب استثنا نمی‌دهد؛ بنابراین handler می‌تواند پیش از هر SELECT روی
/// جدول تسویه، fail-closed و بدون retry مسموم‌کننده رد شود.
/// </summary>
public sealed class SettlementLedgerSchemaReadiness : ISettlementLedgerSchemaReadiness
{
    private static readonly string LedgerRelation =
        $"{SettlementDbContext.Schema}.shipping_group_fees";

    private readonly SettlementDbContext _db;

    /// <summary>آمادگی را به schema تسویه وصل می‌کند.</summary>
    public SettlementLedgerSchemaReadiness(SettlementDbContext db) =>
        _db = db ?? throw new ArgumentNullException(nameof(db));

    /// <inheritdoc />
    public Task<bool> IsLedgerReadyAsync(CancellationToken cancellationToken) =>
        _db.Database
            .SqlQueryRaw<bool>("select to_regclass({0}) is not null as \"Value\"", LedgerRelation)
            .SingleAsync(cancellationToken);
}
