using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tooba.Tax.Domain;

namespace Tooba.Tax.Infrastructure.Persistence.Configurations;

/// <summary>
/// پیکربندی EF برای TaxRule.
/// </summary>
public sealed class TaxRuleConfiguration : IEntityTypeConfiguration<TaxRule>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TaxRule> entity)
    {
        entity.ToTable("rules");
        entity.HasKey(x => x.RuleId);
        entity.Property(x => x.RuleId).ValueGeneratedNever();
        entity.Property(x => x.Jurisdiction).HasMaxLength(64);
        entity.Property(x => x.Market).HasMaxLength(16);
        entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.OverridePolicy).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.Rate).HasPrecision(19, 8);
        entity.Ignore(x => x.DomainEvents);
        entity.HasIndex(x => new { x.Jurisdiction, x.Market, x.CategoryId, x.EffectiveFrom });
    }
}
