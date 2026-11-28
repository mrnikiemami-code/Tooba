using Tooba.Order.Application.Storefront.Shipping;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

public sealed class StorefrontOfferShippingEligibilityTests
{
    [Fact]
    public void Separate_groups_keep_their_own_methods_but_a_combined_group_requires_intersection()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var rules = new StorefrontOfferShippingEligibility(new Dictionary<Guid, HashSet<string>>
        {
            [first] = new(StringComparer.OrdinalIgnoreCase) { "truck", "post" },
            [second] = new(StringComparer.OrdinalIgnoreCase) { "freight", "post" },
        });

        Assert.True(rules.AllowsAny("truck"));
        Assert.True(rules.AllowsAny("freight"));
        Assert.True(rules.AllowsGroup([first], "TRUCK"));
        Assert.True(rules.AllowsGroup([second], "freight"));
        Assert.False(rules.AllowsGroup([first, second], "truck"));
        Assert.False(rules.AllowsGroup([first, second], "freight"));
        Assert.True(rules.AllowsGroup([first, second], "post"));
    }

    [Fact]
    public void Missing_empty_or_disjoint_membership_never_falls_back_to_automatic()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var unavailable = Guid.NewGuid();
        var rules = new StorefrontOfferShippingEligibility(new Dictionary<Guid, HashSet<string>>
        {
            [first] = new(StringComparer.OrdinalIgnoreCase) { "truck" },
            [second] = new(StringComparer.OrdinalIgnoreCase) { "freight" },
            [unavailable] = new(StringComparer.OrdinalIgnoreCase),
        });

        Assert.False(rules.AllowsGroup([first, second], "truck"));
        Assert.False(rules.AllowsGroup([first, second], "freight"));
        Assert.False(rules.AllowsGroup([first, unavailable], "truck"));
        Assert.False(rules.AllowsGroup([Guid.NewGuid()], "truck"));
        Assert.False(rules.AllowsGroup([], "truck"));
        Assert.False(rules.AllowsGroup([first], ""));
    }
}
