using Microsoft.Extensions.Options;
using Tooba.Catalog.Contracts.Reservation;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Infrastructure.Adapters;
using Tooba.Payment.Infrastructure.Providers;
using Xunit;

namespace Tooba.Payment.Tests.Behavior;

/// <summary>
/// Characterization of the Payment-owned commerce hold policy that was moved out of Host root.
/// Precedence must stay: payment method &gt; store override &gt; Payment:Gateway default, with clamping.
/// </summary>
public sealed class CommerceHoldPolicySourceTests
{
    [Fact]
    public void Platform_defaults_are_used_when_no_store_or_method_override_exists()
    {
        var policy = Create(
            gateway: new PaymentGatewayOptions
            {
                OnlinePaymentHoldHours = 2,
                ManualPaymentInitialHoldHours = 3,
                ManualPaymentReviewHoldHours = 5,
            });

        Assert.Equal(2, policy.ResolveOnlineHoldHours("gateway"));
        Assert.Equal(3, policy.ResolveManualInitialHoldHours("gateway"));
        Assert.Equal(5, policy.ResolveManualReviewHoldHours("gateway"));
    }

    [Fact]
    public void Store_override_takes_precedence_over_platform_default()
    {
        var policy = Create(
            gateway: new PaymentGatewayOptions { OnlinePaymentHoldHours = 2 },
            store: new StoreHoldPolicyHoursSnapshot(null, OnlinePaymentHoldHours: 8, null, null));

        Assert.Equal(8, policy.ResolveOnlineHoldHours("gateway"));
    }

    [Fact]
    public void Method_override_takes_precedence_over_store_override()
    {
        var policy = Create(
            gateway: new PaymentGatewayOptions { OnlinePaymentHoldHours = 2 },
            store: new StoreHoldPolicyHoursSnapshot(null, OnlinePaymentHoldHours: 8, null, null),
            methods: [new PaymentMethodHoldOverrideDto("gateway", OnlinePaymentHoldHours: 11, null, null)]);

        Assert.Equal(11, policy.ResolveOnlineHoldHours("gateway"));
    }

    [Fact]
    public void Online_payment_hold_overrides_from_gateway_options_apply_when_store_row_is_absent()
    {
        var policy = Create(
            gateway: new PaymentGatewayOptions
            {
                OnlinePaymentHoldHours = 2,
                ManualPaymentInitialHoldHours = 3,
                OrderSupplyHoldOverrides = new OrderSupplyHoldOverrideOptions
                {
                    OnlinePaymentHoldHours = 9,
                    ManualPaymentInitialHoldHours = 4,
                },
            });

        Assert.Equal(9, policy.ResolveOnlineHoldHours("gateway"));
        Assert.Equal(4, policy.ResolveManualInitialHoldHours("gateway"));
    }

    [Fact]
    public void Hold_hours_are_clamped_into_the_one_to_seven_twenty_range()
    {
        var policy = Create(
            gateway: new PaymentGatewayOptions
            {
                OnlinePaymentHoldHours = -5,
                ManualPaymentInitialHoldHours = 100_000,
            });

        Assert.Equal(1, policy.ResolveOnlineHoldHours("gateway"));
        Assert.Equal(24 * 30, policy.ResolveManualInitialHoldHours("gateway"));
    }

    [Fact]
    public void Initial_expiry_is_the_maximum_of_online_and_manual_initial_hold()
    {
        var policy = Create(
            gateway: new PaymentGatewayOptions
            {
                OnlinePaymentHoldHours = 2,
                ManualPaymentInitialHoldHours = 3,
            });
        var now = DateTimeOffset.Parse("2026-09-12T00:00:00Z");

        Assert.Equal(now.AddHours(3), policy.ResolveInitialExpiresAt(now));
    }

    [Fact]
    public void Unpaid_timeout_uses_manual_initial_hold_for_the_manual_provider()
    {
        var policy = Create(
            gateway: new PaymentGatewayOptions
            {
                OnlinePaymentHoldHours = 2,
                ManualPaymentInitialHoldHours = 3,
            });
        var now = DateTimeOffset.Parse("2026-09-12T00:00:00Z");

        Assert.Equal(now.AddHours(3), policy.ResolveUnpaidTimeoutAt("manual", now));
        Assert.Equal(now.AddHours(2), policy.ResolveUnpaidTimeoutAt("gateway", now));
    }

    [Fact]
    public void Manual_review_expiry_uses_the_manual_review_hold()
    {
        var policy = Create(
            gateway: new PaymentGatewayOptions { ManualPaymentReviewHoldHours = 5 });
        var now = DateTimeOffset.Parse("2026-09-12T00:00:00Z");

        Assert.Equal(now.AddHours(5), policy.ResolveManualReviewExpiresAt(now));
    }

    private static CommerceHoldPolicySource Create(
        PaymentGatewayOptions gateway,
        StoreHoldPolicyHoursSnapshot? store = null,
        IReadOnlyList<PaymentMethodHoldOverrideDto>? methods = null) =>
        new(
            Options.Create(gateway),
            new FakeStoreHoldPolicyHoursReader(store),
            new FakePaymentHoldSettingsDirectory(methods ?? []));

    private sealed class FakeStoreHoldPolicyHoursReader(StoreHoldPolicyHoursSnapshot? snapshot)
        : IStoreHoldPolicyHoursReader
    {
        public Task<StoreHoldPolicyHoursSnapshot> GetHoursAsync(CancellationToken cancellationToken) =>
            Task.FromResult(snapshot ?? new StoreHoldPolicyHoursSnapshot(null, null, null, null));
    }

    private sealed class FakePaymentHoldSettingsDirectory(IReadOnlyList<PaymentMethodHoldOverrideDto> rows)
        : IPaymentHoldSettingsDirectory
    {
        public Task<IReadOnlyList<PaymentMethodHoldOverrideDto>> ListMethodOverridesAsync(
            CancellationToken cancellationToken) => Task.FromResult(rows);

        public Task UpsertMethodOverrideAsync(
            string providerCode,
            int? onlinePaymentHoldHours,
            int? manualPaymentInitialHoldHours,
            int? manualPaymentReviewHoldHours,
            DateTimeOffset updatedAtUtc,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
