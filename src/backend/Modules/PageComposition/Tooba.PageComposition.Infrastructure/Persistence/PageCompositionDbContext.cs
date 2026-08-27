using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.PageComposition.Domain;
using Tooba.Persistence;

namespace Tooba.PageComposition.Infrastructure.Persistence;

/// <summary>DbContext مالک schema مستقل page_composition.</summary>
public sealed class PageCompositionDbContext : DbContext
{
    /// <summary>schema اختصاصی PageComposition.</summary>
    public const string Schema = "page_composition";

    /// <summary>DbContext را می‌سازد.</summary>
    public PageCompositionDbContext(DbContextOptions<PageCompositionDbContext> options) : base(options) { }

    /// <summary>تعاریف صفحه.</summary>
    public DbSet<PageDefinition> PageDefinitions => Set<PageDefinition>();

    /// <summary>sectionهای صفحه.</summary>
    public DbSet<PageSection> PageSections => Set<PageSection>();

    /// <summary>Outbox ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<PageDefinition>(entity =>
        {
            entity.ToTable("page_definitions");
            entity.HasKey(x => x.PageDefinitionId);
            entity.Property(x => x.PageDefinitionId).ValueGeneratedNever();
            entity.Property(x => x.PageKey).HasMaxLength(PageDefinition.PageKeyMaxLength).IsRequired();
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.Locale).HasMaxLength(PageDefinition.LocaleMaxLength);
            entity.Property(x => x.VersionToken).IsConcurrencyToken();
            entity.HasIndex(x => new { x.TenantId, x.PageKey, x.Locale }).IsUnique();
            entity.Ignore(x => x.Sections);
        });
        modelBuilder.Entity<PageSection>(entity =>
        {
            entity.ToTable("page_sections");
            entity.HasKey(x => x.PageSectionId);
            entity.Property(x => x.PageSectionId).ValueGeneratedNever();
            entity.Property(x => x.PageDefinitionId).IsRequired();
            entity.Property(x => x.SectionType).HasMaxLength(PageSection.SectionTypeMaxLength).IsRequired();
            entity.Property(x => x.Variant).HasMaxLength(PageSection.VariantMaxLength).IsRequired();
            entity.Property(x => x.ConfigurationJson).HasMaxLength(PageSection.ConfigurationJsonMaxLength).IsRequired();
            entity.HasIndex(x => new { x.PageDefinitionId, x.DisplayOrder });
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ design-time مهاجرت PageComposition.</summary>
public sealed class PageCompositionDbContextFactory : IDesignTimeDbContextFactory<PageCompositionDbContext>
{
    /// <inheritdoc />
    public PageCompositionDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PageCompositionDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            PageCompositionDbContext.Schema,
            typeof(PageCompositionDbContext));
        return new PageCompositionDbContext(options.Options);
    }
}
