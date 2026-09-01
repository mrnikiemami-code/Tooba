using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Localization.Domain;
using Tooba.Persistence;

namespace Tooba.Localization.Infrastructure.Persistence;

/// <summary>DbContext مالک schema localization.</summary>
public sealed class LocalizationDbContext : DbContext
{
    public const string Schema = "localization";

    public LocalizationDbContext(DbContextOptions<LocalizationDbContext> options) : base(options) { }

    public DbSet<Language> Languages => Set<Language>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<Language>(entity =>
        {
            entity.ToTable("languages");
            entity.HasKey(x => x.LanguageId);
            entity.Property(x => x.LanguageId).ValueGeneratedNever();
            entity.Property(x => x.Code).HasMaxLength(Language.CodeMaxLength).IsRequired();
            entity.Property(x => x.UrlPrefix).HasMaxLength(Language.UrlPrefixMaxLength).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(Language.DisplayNameMaxLength).IsRequired();
            entity.Property(x => x.NativeName).HasMaxLength(Language.NativeNameMaxLength).IsRequired();
            entity.Property(x => x.Culture).HasMaxLength(Language.CultureMaxLength).IsRequired();
            entity.Property(x => x.Direction).HasConversion<string>().HasMaxLength(8);
            entity.Property(x => x.CalendarDisplay).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.UrlPrefix).IsUnique();
            entity.HasIndex(x => new { x.IsActive, x.SortOrder });
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

public sealed class LocalizationDbContextFactory : IDesignTimeDbContextFactory<LocalizationDbContext>
{
    public LocalizationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LocalizationDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            LocalizationDbContext.Schema,
            typeof(LocalizationDbContext));
        return new LocalizationDbContext(options.Options);
    }
}
