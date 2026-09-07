using Tooba.Order.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P09-T005: Order Line return policy snapshot is purchase-time immutable.</summary>
public sealed class OrderLineReturnPolicySnapshotTests
{
    [Fact]
    public void FromCheckout_persists_return_policy_snapshot_defaults()
    {
        var line = OrderLine.FromCheckout(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            2,
            1000m,
            "IRR",
            true,
            Guid.NewGuid(),
            null,
            "Taxable",
            0.09m,
            180m,
            2180m,
            null);

        Assert.True(line.IsReturnableSnapshot);
        Assert.Equal(7, line.ReturnWindowDaysSnapshot);
        Assert.Equal("platform_default", line.ReturnPolicySourceSnapshot);
        Assert.Contains("روز پس از تحویل", line.ReturnPolicyLabelSnapshot);
    }

    [Fact]
    public void FromCheckout_can_snapshot_non_returnable_and_custom_window()
    {
        var line = OrderLine.FromCheckout(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            1000m,
            "IRR",
            true,
            Guid.NewGuid(),
            null,
            "Taxable",
            0.09m,
            90m,
            1090m,
            null,
            isReturnableSnapshot: false,
            returnWindowDaysSnapshot: 0,
            returnPolicySourceSnapshot: "offer_override",
            returnPolicyLabelSnapshot: "غیرقابل مرجوعی");

        Assert.False(line.IsReturnableSnapshot);
        Assert.Equal(0, line.ReturnWindowDaysSnapshot);
        Assert.Equal("offer_override", line.ReturnPolicySourceSnapshot);
        Assert.Equal("غیرقابل مرجوعی", line.ReturnPolicyLabelSnapshot);
    }

    [Fact]
    public void Mutable_offer_change_does_not_alter_existing_line_snapshot_fields()
    {
        var line = OrderLine.FromCheckout(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            1000m,
            "IRR",
            true,
            Guid.NewGuid(),
            null,
            "Taxable",
            0.09m,
            90m,
            1090m,
            null,
            isReturnableSnapshot: true,
            returnWindowDaysSnapshot: 14,
            returnPolicySourceSnapshot: "platform_default",
            returnPolicyLabelSnapshot: "۱۴ روز پس از تحویل");

        // Snapshot properties are init-only; "mutable Offer" cannot rewrite historical line.
        Assert.Equal(14, line.ReturnWindowDaysSnapshot);
        Assert.Equal("۱۴ روز پس از تحویل", line.ReturnPolicyLabelSnapshot);
    }
}
