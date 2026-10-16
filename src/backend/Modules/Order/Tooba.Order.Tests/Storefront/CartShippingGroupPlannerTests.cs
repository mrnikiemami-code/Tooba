using Tooba.BuildingBlocks;
using Tooba.Order.Domain.Checkout;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-126 — focused proof for the Order-owned multi-group cart shipping draft.
/// Pure domain proof: no Host, no shared database, no process, no frontend.
/// </summary>
public sealed class CartShippingGroupPlannerTests
{
    private static readonly Guid SellerA = Guid.Parse("aaaaaaaa-0000-7000-8000-000000000001");
    private static readonly Guid SellerB = Guid.Parse("bbbbbbbb-0000-7000-8000-000000000002");
    private static readonly Guid Cart = Guid.Parse("cccccccc-0000-7000-8000-000000000003");
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void One_seller_with_two_methods_produces_two_groups_with_authoritative_amounts()
    {
        var lines = new[]
        {
            Line("00000000-0000-7000-8000-00000000000a", SellerA),
            Line("00000000-0000-7000-8000-00000000000b", SellerA),
        };
        var requests = new[]
        {
            Request("post:express", lines[0].LineId),
            Request("tipax:standard", lines[1].LineId),
        };

        var groups = Plan(lines, requests);

        Assert.Equal(2, groups.Count);
        Assert.All(groups, g => Assert.Equal(SellerA, g.SellerPartyId));
        Assert.Equal(["post:express", "tipax:standard"], groups.Select(g => g.MethodCode).ToArray());
        Assert.All(groups, g => Assert.Single(g.Lines));
        Assert.Equal("IRT", groups[0].Currency);
        Assert.Equal(120_000m, groups[1].ShippingAmount);
    }

    [Fact]
    public void Two_sellers_with_the_identical_method_stay_separate()
    {
        var lines = new[]
        {
            Line("00000000-0000-7000-8000-00000000000a", SellerA),
            Line("00000000-0000-7000-8000-00000000000b", SellerB),
        };
        var requests = new[]
        {
            Request("post:express", lines[0].LineId),
            Request("post:express", lines[1].LineId),
        };

        var groups = Plan(lines, requests);

        Assert.Equal(2, groups.Count);
        Assert.Equal(2, groups.Select(g => g.SellerPartyId).Distinct().Count());
        Assert.All(groups, g => Assert.Equal("post:express", g.MethodCode));
    }

    [Fact]
    public void Duplicate_line_membership_is_refused()
    {
        var lines = new[] { Line("00000000-0000-7000-8000-00000000000a", SellerA) };
        var requests = new[]
        {
            Request("post:express", lines[0].LineId),
            Request("tipax:standard", lines[0].LineId),
        };

        AssertFault(CartShippingGroupFault.DuplicateLineMembership, lines, requests);
    }

    [Fact]
    public void Omitted_cart_line_is_refused()
    {
        var lines = new[]
        {
            Line("00000000-0000-7000-8000-00000000000a", SellerA),
            Line("00000000-0000-7000-8000-00000000000b", SellerB),
        };
        var requests = new[] { Request("post:express", lines[0].LineId) };

        AssertFault(CartShippingGroupFault.MissingLineMembership, lines, requests);
    }

    [Fact]
    public void Unknown_or_foreign_cart_line_is_refused()
    {
        var lines = new[] { Line("00000000-0000-7000-8000-00000000000a", SellerA) };
        var requests = new[] { Request("post:express", Guid.Parse("00000000-0000-7000-8000-0000000000ff")) };

        AssertFault(CartShippingGroupFault.UnknownLine, lines, requests);
    }

    [Fact]
    public void Merging_two_sellers_into_one_group_is_refused()
    {
        var lines = new[]
        {
            Line("00000000-0000-7000-8000-00000000000a", SellerA),
            Line("00000000-0000-7000-8000-00000000000b", SellerB),
        };
        var requests = new[]
        {
            Request("post:express", lines[0].LineId, lines[1].LineId),
        };

        AssertFault(CartShippingGroupFault.MixedSellerLines, lines, requests);
    }

    [Fact]
    public void Same_seller_and_same_method_cannot_be_two_groups_in_v1()
    {
        var lines = new[]
        {
            Line("00000000-0000-7000-8000-00000000000a", SellerA),
            Line("00000000-0000-7000-8000-00000000000b", SellerA),
        };
        var requests = new[]
        {
            Request("post:express", lines[0].LineId),
            Request("post:express", lines[1].LineId),
        };

        AssertFault(CartShippingGroupFault.DuplicateSellerMethod, lines, requests);
    }

    [Fact]
    public void Method_outside_the_authoritative_eligible_set_is_refused()
    {
        var lines = new[] { Line("00000000-0000-7000-8000-00000000000a", SellerA) };
        var requests = new[] { Request("forged:method", lines[0].LineId) };

        AssertFault(CartShippingGroupFault.MethodNotEligible, lines, requests);
    }

    [Fact]
    public void Declared_service_or_option_identity_must_match_authoritative_identity()
    {
        var lines = new[] { Line("00000000-0000-7000-8000-00000000000a", SellerA) };
        var requests = new[]
        {
            new CartShippingGroupRequest("tipax:standard", "post", null, null, [lines[0].LineId]),
        };

        AssertFault(CartShippingGroupFault.MethodIdentityMismatch, lines, requests);
    }

    [Fact]
    public void Unverified_origin_context_is_refused_instead_of_forged()
    {
        var lines = new[] { Line("00000000-0000-7000-8000-00000000000a", SellerA) };
        var requests = new[]
        {
            new CartShippingGroupRequest("post:express", null, null, "WH-01", [lines[0].LineId]),
        };

        AssertFault(CartShippingGroupFault.OriginUnverified, lines, requests);
    }

    [Fact]
    public void Group_order_is_deterministic_for_a_stable_legacy_scalar_projection()
    {
        var lines = new[]
        {
            Line("00000000-0000-7000-8000-00000000000a", SellerB),
            Line("00000000-0000-7000-8000-00000000000b", SellerA),
        };
        var requests = new[]
        {
            Request("tipax:standard", lines[0].LineId),
            Request("post:express", lines[1].LineId),
        };

        var groups = Plan(lines, requests);

        Assert.Equal([SellerA, SellerB], groups.Select(g => g.SellerPartyId).ToArray());
    }

    [Fact]
    public void Legacy_single_method_selection_becomes_exactly_one_group_covering_every_line()
    {
        var lines = new[]
        {
            Line("00000000-0000-7000-8000-00000000000a", SellerA),
            Line("00000000-0000-7000-8000-00000000000b", SellerA),
        };
        // Legacy shorthand: the old scalar request expands to ONE group over all cart lines.
        var requests = new[]
        {
            new CartShippingGroupRequest(
                "post:express",
                null,
                null,
                null,
                lines.Select(x => x.LineId).ToArray()),
        };

        var groups = Plan(lines, requests);

        Assert.Single(groups);
        Assert.Equal(SellerA, groups[0].SellerPartyId);
        Assert.Equal("post:express", groups[0].MethodCode);
        Assert.Equal(2, groups[0].Lines.Count);
        Assert.Equal(100_000m, groups[0].ShippingAmount);
    }

    [Fact]
    public void Legacy_shorthand_for_a_multi_seller_cart_is_refused_rather_than_merged()
    {
        var lines = new[]
        {
            Line("00000000-0000-7000-8000-00000000000a", SellerA),
            Line("00000000-0000-7000-8000-00000000000b", SellerB),
        };
        var requests = new[]
        {
            new CartShippingGroupRequest(
                "post:express",
                null,
                null,
                null,
                lines.Select(x => x.LineId).ToArray()),
        };

        AssertFault(CartShippingGroupFault.MixedSellerLines, lines, requests);
    }

    private static CartShippingGroupCartLine Line(string id, Guid seller) =>
        new(Guid.Parse(id), seller, 1m);

    private static CartShippingGroupRequest Request(string method, params Guid[] lineIds) =>
        new(method, null, null, null, lineIds);

    private static IReadOnlyList<CartShippingGroupEligibleMethod> EligibleMethods() =>
    [
        new("post:express", "post", "express", "پست پیشتاز", 100_000m, "IRT", 2,
            new DateOnly(2026, 10, 19)),
        new("tipax:standard", "tipax", "standard", "تیپاکس عادی", 120_000m, "IRT", 3,
            new DateOnly(2026, 10, 20)),
    ];

    private static IReadOnlyList<CartShippingDraftGroup> Plan(
        IReadOnlyList<CartShippingGroupCartLine> lines,
        IReadOnlyList<CartShippingGroupRequest> requests) =>
        CartShippingGroupPlanner.Plan(
            Cart,
            lines,
            requests,
            EligibleMethods(),
            null,
            null,
            Now,
            NewIds());

    private static void AssertFault(
        string expectedFault,
        IReadOnlyList<CartShippingGroupCartLine> lines,
        IReadOnlyList<CartShippingGroupRequest> requests)
    {
        var exception = Assert.Throws<ContractOperationException>(() => Plan(lines, requests));
        Assert.Equal(expectedFault, exception.Code);
    }

    private static Func<Guid> NewIds()
    {
        var next = 0;
        return () => Guid.Parse($"00000000-0000-7000-9000-0000000000{++next:D2}");
    }
}
