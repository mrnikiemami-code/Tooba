using Xunit;
using Tooba.Tax.Domain;

namespace Tooba.Tax.Tests.Domain;

public sealed class TaxRuleInvariantTests
{
    [Fact]
    public void Create_percentage_rule_starts_draft()
    {
        var rule = TaxRule.Create(
            Guid.NewGuid(),
            "IR-NAT",
            "IR",
            Guid.NewGuid(),
            TaxRuleKind.Percentage,
            0.09m,
            DateTimeOffset.Parse("2026-01-01Z"),
            null,
            100,
            TaxOverridePolicy.Disabled,
            DateTimeOffset.Parse("2026-01-01Z"));
        Assert.Equal(TaxRuleStatus.Draft, rule.Status);
        Assert.Contains(rule.DomainEvents, e => e is TaxRuleCreatedDomainEvent);
    }

    [Fact]
    public void Create_rejects_percentage_rate_above_one()
    {
        Assert.Throws<InvalidOperationException>(() => TaxRule.Create(
            Guid.NewGuid(),
            "IR-NAT",
            "IR",
            Guid.NewGuid(),
            TaxRuleKind.Percentage,
            1.5m,
            DateTimeOffset.UtcNow,
            null,
            1,
            TaxOverridePolicy.Disabled,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void TaxRounding_irr_has_zero_scale()
    {
        Assert.Equal(9m, TaxRounding.Round(9.4m, "IRR"));
    }
}
