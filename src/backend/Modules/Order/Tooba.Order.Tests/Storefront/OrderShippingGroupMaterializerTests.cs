using Tooba.BuildingBlocks;
using Tooba.Order.Domain.Checkout;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-127 — مادی‌سازی قطعی گروه‌های ارسال پایدار سفارش پذیرفته‌شده (M2).
/// ناوردایی‌های مالکیت، پوشش کامل خطوط، پایداری شناسه و جمع هزینهٔ یک‌بار ارسال اینجا اثبات می‌شوند.
/// </summary>
public sealed class OrderShippingGroupMaterializerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Single_seller_two_methods_materializes_two_durable_groups()
    {
        var checkoutId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var cartLineA = Guid.NewGuid();
        var cartLineB = Guid.NewGuid();

        var plans = new[]
        {
            Plan(seller, "post:express", 25_000m, [cartLineA]),
            Plan(seller, "post:standard", 10_000m, [cartLineB]),
        };

        var groups = OrderShippingGroupMaterializer.Materialize(
            plans,
            [(lineA, seller), (lineB, seller)],
            new Dictionary<Guid, Guid> { [seller] = sellerOrderId },
            new Dictionary<Guid, Guid> { [cartLineA] = lineA, [cartLineB] = lineB },
            checkoutId,
            Now);

        Assert.Equal(2, groups.Count);
        Assert.All(groups, group => Assert.Equal(sellerOrderId, group.SellerOrderId));
        Assert.All(groups, group => Assert.Equal(checkoutId, group.CheckoutId));
        Assert.All(groups, group => Assert.Equal(OrderShippingGroup.ConfirmedState, group.State));
        Assert.Equal(2, groups.SelectMany(x => x.Lines).Select(x => x.OrderLineId).Distinct().Count());
        Assert.Equal(35_000m, OrderShippingGroupMaterializer.SumShippingCharge(groups));
    }

    [Fact]
    public void Two_sellers_same_method_materializes_separate_seller_scoped_groups()
    {
        var sellerA = Guid.NewGuid();
        var sellerB = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var cartLineA = Guid.NewGuid();
        var cartLineB = Guid.NewGuid();

        var plans = new[]
        {
            Plan(sellerA, "post:standard", 10_000m, [cartLineA]),
            Plan(sellerB, "post:standard", 12_000m, [cartLineB]),
        };

        var groups = OrderShippingGroupMaterializer.Materialize(
            plans,
            [(lineA, sellerA), (lineB, sellerB)],
            new Dictionary<Guid, Guid> { [sellerA] = Guid.NewGuid(), [sellerB] = Guid.NewGuid() },
            new Dictionary<Guid, Guid> { [cartLineA] = lineA, [cartLineB] = lineB },
            Guid.NewGuid(),
            Now);

        Assert.Equal(2, groups.Count);
        Assert.Single(groups.Single(x => x.SellerPartyId == sellerA).Lines);
        Assert.Single(groups.Single(x => x.SellerPartyId == sellerB).Lines);
        Assert.Equal(22_000m, OrderShippingGroupMaterializer.SumShippingCharge(groups));
    }

    [Fact]
    public void Group_and_line_identities_are_carried_from_the_draft_with_exactly_once_membership()
    {
        var seller = Guid.NewGuid();
        var line = Guid.NewGuid();
        var cartLine = Guid.NewGuid();
        var draftGroupId = Guid.NewGuid();

        var groups = OrderShippingGroupMaterializer.Materialize(
            [Plan(seller, "post:standard", 10_000m, [cartLine], draftGroupId)],
            [(line, seller)],
            new Dictionary<Guid, Guid> { [seller] = Guid.NewGuid() },
            new Dictionary<Guid, Guid> { [cartLine] = line },
            Guid.NewGuid(),
            Now);

        var group = Assert.Single(groups);
        Assert.Equal(draftGroupId, group.ShippingGroupId);
        Assert.Equal(line, Assert.Single(group.Lines).OrderLineId);
    }

    [Fact]
    public void Duplicate_line_across_groups_aborts_without_partial_groups()
    {
        var seller = Guid.NewGuid();
        var line = Guid.NewGuid();
        var cartLine = Guid.NewGuid();
        var map = new Dictionary<Guid, Guid> { [cartLine] = line };

        var fault = Assert.Throws<ContractOperationException>(() =>
            OrderShippingGroupMaterializer.Materialize(
                [
                    Plan(seller, "post:standard", 10_000m, [cartLine]),
                    Plan(seller, "post:express", 20_000m, [cartLine]),
                ],
                [(line, seller)],
                new Dictionary<Guid, Guid> { [seller] = Guid.NewGuid() },
                map,
                Guid.NewGuid(),
                Now));

        Assert.Equal(OrderShippingGroupFault.DuplicateLineMembership, fault.Code);
    }

    [Fact]
    public void Uncovered_order_line_aborts_instead_of_silent_drop()
    {
        var seller = Guid.NewGuid();
        var coveredLine = Guid.NewGuid();
        var orphanLine = Guid.NewGuid();
        var cartLine = Guid.NewGuid();

        var fault = Assert.Throws<ContractOperationException>(() =>
            OrderShippingGroupMaterializer.Materialize(
                [Plan(seller, "post:standard", 10_000m, [cartLine])],
                [(coveredLine, seller), (orphanLine, seller)],
                new Dictionary<Guid, Guid> { [seller] = Guid.NewGuid() },
                new Dictionary<Guid, Guid> { [cartLine] = coveredLine },
                Guid.NewGuid(),
                Now));

        Assert.Equal(OrderShippingGroupFault.MissingLineMembership, fault.Code);
    }

    [Fact]
    public void Wrong_seller_mapping_aborts_before_any_side_effect()
    {
        var sellerA = Guid.NewGuid();
        var sellerB = Guid.NewGuid();
        var lineOfA = Guid.NewGuid();
        var cartLine = Guid.NewGuid();

        var fault = Assert.Throws<ContractOperationException>(() =>
            OrderShippingGroupMaterializer.Materialize(
                [Plan(sellerB, "post:standard", 10_000m, [cartLine])],
                [(lineOfA, sellerA)],
                new Dictionary<Guid, Guid> { [sellerA] = Guid.NewGuid(), [sellerB] = Guid.NewGuid() },
                new Dictionary<Guid, Guid> { [cartLine] = lineOfA },
                Guid.NewGuid(),
                Now));

        Assert.Equal(OrderShippingGroupFault.SellerMismatch, fault.Code);
    }

    [Fact]
    public void Missing_cart_line_mapping_aborts_instead_of_guessing()
    {
        var seller = Guid.NewGuid();
        var line = Guid.NewGuid();

        var fault = Assert.Throws<ContractOperationException>(() =>
            OrderShippingGroupMaterializer.Materialize(
                [Plan(seller, "post:standard", 10_000m, [Guid.NewGuid()])],
                [(line, seller)],
                new Dictionary<Guid, Guid> { [seller] = Guid.NewGuid() },
                new Dictionary<Guid, Guid>(),
                Guid.NewGuid(),
                Now));

        Assert.Equal(OrderShippingGroupFault.CartLineMappingMissing, fault.Code);
    }

    [Fact]
    public void Legacy_single_method_order_without_plans_materializes_no_groups()
    {
        var groups = OrderShippingGroupMaterializer.Materialize(
            [],
            [(Guid.NewGuid(), Guid.NewGuid())],
            new Dictionary<Guid, Guid>(),
            new Dictionary<Guid, Guid>(),
            Guid.NewGuid(),
            Now);

        Assert.Empty(groups);
    }

    [Fact]
    public void Unverified_origin_is_refused_rather_than_forged()
    {
        var seller = Guid.NewGuid();
        var line = Guid.NewGuid();
        var cartLine = Guid.NewGuid();

        var fault = Assert.Throws<ContractOperationException>(() =>
            OrderShippingGroupMaterializer.Materialize(
                [Plan(seller, "post:standard", 10_000m, [cartLine]) with { OriginContext = "warehouse-1" }],
                [(line, seller)],
                new Dictionary<Guid, Guid> { [seller] = Guid.NewGuid() },
                new Dictionary<Guid, Guid> { [cartLine] = line },
                Guid.NewGuid(),
                Now));

        Assert.Equal(OrderShippingGroupFault.OriginUnverified, fault.Code);
    }

    private static OrderShippingGroupPlan Plan(
        Guid sellerPartyId,
        string methodCode,
        decimal charge,
        IReadOnlyList<Guid> cartLineIds,
        Guid? groupId = null) =>
        new(
            groupId ?? Guid.NewGuid(),
            sellerPartyId,
            methodCode,
            null,
            null,
            methodCode,
            null,
            charge,
            "IRR",
            new DateOnly(2026, 10, 20),
            null,
            null,
            1,
            cartLineIds);
}
