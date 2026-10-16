using Microsoft.Extensions.Configuration;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Contracts.Settlement;
using Tooba.Settlement.Application.Payouts.Ports;
using Tooba.Settlement.Infrastructure.DependencyInjection;
using Tooba.Settlement.Infrastructure.Observability;

namespace Tooba.Settlement.Infrastructure.Gateways;

/// <summary>
/// دروازهٔ per-event ثبت ledger تسویه. دو حقیقت را از هم جدا می‌کند:
/// <list type="number">
/// <item>قابلیت ثبت‌نام (deployment capability) که در <c>SettlementModule</c> تصمیم گرفته می‌شود؛</item>
/// <item>واجد شرایط بودن همین رویداد: کانال فروش پایدار Order و آمادگی schema تسویه.</item>
/// </list>
/// تصمیم کانال از Contracts سفارش خوانده می‌شود و آمادگی schema از درز صریح
/// <see cref="ISettlementLedgerSchemaReadiness"/> می‌آید؛ هیچ relation ناموجودی SELECT نمی‌شود تا شکست
/// handler، حمل‌ونقل retrying را مسموم نکند و هیچ payout/سهمی ساخته نشود.
/// </summary>
public sealed class SettlementChannelLedgerEligibilityGate : ISettlementChannelLedgerEligibilityGate
{
    private readonly bool _marketplaceChannelOnly;
    private readonly IOrderCheckoutSalesChannelSettlementReader _channels;
    private readonly ISettlementLedgerSchemaReadiness _readiness;
    private readonly SettlementInstrumentation _telemetry;

    /// <summary>دروازه را به پیکربندی استقرار، درز کانال سفارش و آمادگی schema تسویه وصل می‌کند.</summary>
    public SettlementChannelLedgerEligibilityGate(
        IConfiguration configuration,
        IOrderCheckoutSalesChannelSettlementReader channels,
        ISettlementLedgerSchemaReadiness readiness,
        SettlementInstrumentation telemetry)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _channels = channels ?? throw new ArgumentNullException(nameof(channels));
        _readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _marketplaceChannelOnly = SettlementMarketplaceChannelLedgerPolicy.RequiresMarketplaceChannelOnly(configuration);
    }

    /// <inheritdoc />
    public async Task<SettlementChannelLedgerEligibility> EvaluateAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        if (!_marketplaceChannelOnly)
        {
            // Edition=Marketplace (یا قابلیت خاموش): behavior موجود دست‌نخورده و بدون فیلتر کانال.
            return SettlementChannelLedgerEligibility.Eligible;
        }

        if (checkoutId == Guid.Empty)
        {
            return Skip(SettlementChannelLedgerEligibility.SaleChannelNotMarketplace);
        }

        var channel = await _channels.GetChannelAsync(checkoutId, cancellationToken);
        if (channel is not { Channel: SalesChannel.Marketplace })
        {
            return Skip(SettlementChannelLedgerEligibility.SaleChannelNotMarketplace);
        }

        if (!await _readiness.IsLedgerReadyAsync(cancellationToken))
        {
            return Skip(SettlementChannelLedgerEligibility.SchemaNotReady);
        }

        return SettlementChannelLedgerEligibility.Eligible;
    }

    private SettlementChannelLedgerEligibility Skip(SettlementChannelLedgerEligibility decision)
    {
        _telemetry.RecordLedgerSkipped();
        return decision;
    }
}
