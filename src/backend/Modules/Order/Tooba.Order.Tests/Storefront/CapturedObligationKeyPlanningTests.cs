using Tooba.BuildingBlocks;
using Tooba.Order.Domain.Checkout;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R3 — focused facts for the CAPTURED obligation seam.
/// A supplied obligation key is an authoritative server token: it must match exactly one eligible row of the
/// same seller with the same canonical service/option, and it must never fall back to "first row of the same
/// seller+method" (that fallback is the wrong-price defect). Legacy requests without a key keep the historic
/// one-group-per-seller+method rule.
/// </summary>
public sealed class CapturedObligationKeyPlanningTests
{
    private static readonly Guid SellerA = Guid.Parse("aaaaaaaa-0000-7000-8000-000000000001");
    private static readonly Guid SellerB = Guid.Parse("bbbbbbbb-0000-7000-8000-000000000002");
    private static readonly Guid Cart = Guid.Parse("cccccccc-0000-7000-8000-000000000003");
    private static readonly DateOnly MinDate = new(2026, 10, 30);
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 6, 0, 0, TimeSpan.Zero);

    private static readonly Guid LineA = Guid.Parse("00000000-0000-7000-8000-00000000000a");
    private static readonly Guid LineB = Guid.Parse("00000000-0000-7000-8000-00000000000b");
    private static readonly Guid LineC = Guid.Parse("00000000-0000-7000-8000-00000000000c");

    [Fact]
    public void Keyed_commitments_of_one_seller_with_one_method_keep_their_own_prices()
    {
        // The real Marketplace shape: the SAME seller is Platform-fulfilled for one line set and Seller-fulfilled
        // for another, over the SAME one-level catalog method. Each commitment must keep its own priced row.
        var lines = new[]
        {
            Line(LineA, SellerA),
            Line(LineB, SellerA),
        };
        var requests = new[]
        {
            Request(LineA, "cap|platform|" + SellerA.ToString("N")),
            Request(LineB, "cap|seller|" + SellerA.ToString("N")),
        };

        var groups = Plan(lines, requests, Eligible());

        Assert.Equal(2, groups.Count);
        Assert.All(groups, g => Assert.Equal(SellerA, g.SellerPartyId));
        Assert.All(groups, g => Assert.Equal("post:standard", g.MethodCode));
        var byLine = groups.ToDictionary(g => g.Lines.Single().CartLineId, g => g.ShippingAmount);
        Assert.Equal(95_000m, byLine[LineA]);
        Assert.Equal(62_000m, byLine[LineB]);
    }

    [Fact]
    public void Unknown_obligation_key_refuses_and_never_falls_back_to_the_seller_method_row()
    {
        // A same seller+method row EXISTS; a missing key match must still refuse instead of borrowing it.
        var lines = new[] { Line(LineA, SellerA) };
        var requests = new[] { Request(LineA, "cap|not-a-known-key") };

        AssertFault(CartShippingGroupFault.MethodNotEligible, lines, requests, Eligible());
    }

    [Fact]
    public void Two_rows_claiming_the_same_obligation_key_are_refused()
    {
        var lines = new[] { Line(LineA, SellerA) };
        var requests = new[] { Request(LineA, "cap|platform|" + SellerA.ToString("N")) };

        // Duplicate the keyed row: one commitment cannot be priced by two rows.
        var eligible = Eligible().Concat(new[]
        {
            new CartShippingGroupEligibleMethod(
                "post:standard", "post", "standard", "پست", 1m, "IRR", 2, MinDate, SellerA,
                "cap|platform|" + SellerA.ToString("N")),
        }).ToArray();

        AssertFault(CartShippingGroupFault.MethodIdentityMismatch, lines, requests, eligible);
    }

    [Fact]
    public void Obligation_key_of_a_foreign_seller_is_refused()
    {
        var lines = new[] { Line(LineA, SellerA) };
        // The key names SellerB's commitment while the line belongs to SellerA.
        var requests = new[] { Request(LineA, "cap|seller|" + SellerB.ToString("N")) };

        AssertFault(CartShippingGroupFault.MethodIdentityMismatch, lines, requests, Eligible());
    }

    [Fact]
    public void Contradictory_declared_service_or_option_is_refused_even_with_a_matching_key()
    {
        var lines = new[] { Line(LineA, SellerA) };
        var key = "cap|platform|" + SellerA.ToString("N");

        AssertFault(
            CartShippingGroupFault.MethodIdentityMismatch,
            lines,
            [
                new CartShippingGroupRequest(
                    "post:standard", "tipax", "standard", null, [LineA], ObligationKey: key),
            ],
            Eligible());
    }

    [Fact]
    public void Legacy_request_without_a_key_still_refuses_one_seller_and_one_method()
    {
        var lines = new[]
        {
            Line(LineA, SellerA),
            Line(LineB, SellerA),
        };
        var requests = new[]
        {
            new CartShippingGroupRequest("post:standard", "post", "standard", null, [LineA]),
            new CartShippingGroupRequest("post:standard", "post", "standard", null, [LineB]),
        };

        AssertFault(CartShippingGroupFault.DuplicateSellerMethod, lines, requests, Eligible());
    }

    [Fact]
    public void Keyed_requests_still_refuse_a_repeated_line()
    {
        var lines = new[] { Line(LineA, SellerA) };
        var requests = new[]
        {
            Request(LineA, "cap|platform|" + SellerA.ToString("N")),
            Request(LineA, "cap|seller|" + SellerA.ToString("N")),
        };

        // Two different obligations over the SAME line and method are the same commitment, so the whole-commitment
        // key (seller|method|membership) already collapses them and the write is refused before any line is attached.
        // The legacy duplicate-line guard is independently proven by CartShippingGroupPlannerTests.
        AssertFault(CartShippingGroupFault.DuplicateSellerMethod, lines, requests, Eligible());
    }

    private static IReadOnlyList<CartShippingGroupEligibleMethod> Eligible() =>
    [
        // Platform-owned commitment of SellerA.
        new("post:standard", "post", "standard", "پست — پلتفرم", 95_000m, "IRR", 2, MinDate, SellerA,
            "cap|platform|" + SellerA.ToString("N")),
        // SellerA's OWN commitment on the same catalog binding/method.
        new("post:standard", "post", "standard", "پست — فروشنده", 62_000m, "IRR", 2, MinDate, SellerA,
            "cap|seller|" + SellerA.ToString("N")),
        // A different seller's commitment with its own key.
        new("post:standard", "post", "standard", "پست — فروشندهٔ دوم", 47_000m, "IRR", 2, MinDate, SellerB,
            "cap|seller|" + SellerB.ToString("N")),
    ];

    private static CartShippingGroupRequest Request(Guid lineId, string key) =>
        new("post:standard", "post", "standard", null, [lineId], ObligationKey: key);

    private static IReadOnlyList<CartShippingDraftGroup> Plan(
        IReadOnlyList<CartShippingGroupCartLine> lines,
        IReadOnlyList<CartShippingGroupRequest> requests,
        IReadOnlyList<CartShippingGroupEligibleMethod> eligible)
    {
        var next = 0;
        return CartShippingGroupPlanner.Plan(
            Cart,
            lines,
            requests,
            eligible,
            null,
            null,
            Now,
            () => Guid.Parse($"00000000-0000-7000-9000-0000000000{++next:D2}"));
    }

    private static void AssertFault(
        string expectedFault,
        IReadOnlyList<CartShippingGroupCartLine> lines,
        IReadOnlyList<CartShippingGroupRequest> requests,
        IReadOnlyList<CartShippingGroupEligibleMethod> eligible)
    {
        var exception = Assert.Throws<ContractOperationException>(() => Plan(lines, requests, eligible));
        Assert.Equal(expectedFault, exception.Code);
    }

    private static CartShippingGroupCartLine Line(Guid lineId, Guid seller) => new(lineId, seller, 1m);
}
