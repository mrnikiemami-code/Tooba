using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tooba.Tax.Domain;

namespace Tooba.Tax.Infrastructure.Persistence.Configurations;

/// <summary>
/// پیکربندی EF برای TaxCategory.
/// </summary>
public sealed class TaxCategoryConfiguration : IEntityTypeConfiguration<TaxCategory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TaxCategory> entity)
    {
        entity.ToTable("categories");
        entity.HasKey(x => x.CategoryId);
        entity.Property(x => x.CategoryId).ValueGeneratedNever();
        entity.Property(x => x.Code).HasMaxLength(64);
        entity.Property(x => x.DisplayName).HasMaxLength(256);
        entity.HasIndex(x => x.Code).IsUnique();
    }
}
