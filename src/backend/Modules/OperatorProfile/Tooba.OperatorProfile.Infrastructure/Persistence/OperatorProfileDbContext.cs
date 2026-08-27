using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OperatorProfileEntity = Tooba.OperatorProfile.Domain.OperatorProfile;
using Tooba.Persistence;

namespace Tooba.OperatorProfile.Infrastructure.Persistence;

/// <summary>DbContext مالک schema مستقل operator_profile و Outbox همان ماژول.</summary>
public sealed class OperatorProfileDbContext : DbContext
{
    /// <summary>نام schema اختصاصی پروفایل اپراتور.</summary>
    public const string Schema = "operator_profile";

    /// <summary>DbContext را با گزینه‌های ماژول می‌سازد.</summary>
    public OperatorProfileDbContext(DbContextOptions<OperatorProfileDbContext> options) : base(options)
    {
    }

    /// <summary>ردیف‌های خصوصی پروفایل اپراتور.</summary>
    public DbSet<OperatorProfileEntity> Profiles => Set<OperatorProfileEntity>();

    /// <summary>پیام‌های Outbox ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<OperatorProfileEntity>(entity =>
        {
            entity.ToTable("operator_profiles");
            entity.HasKey(x => x.OwnerUserId);
            entity.Property(x => x.OwnerUserId).ValueGeneratedNever();
            entity.Property(x => x.FirstName).HasMaxLength(OperatorProfileEntity.NamePartMaxLength).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(OperatorProfileEntity.NamePartMaxLength).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(OperatorProfileEntity.DisplayNameMaxLength).IsRequired();
            entity.Property(x => x.Bio).HasMaxLength(OperatorProfileEntity.BioMaxLength);
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ design-time مهاجرت‌های OperatorProfile.</summary>
public sealed class OperatorProfileDbContextFactory : IDesignTimeDbContextFactory<OperatorProfileDbContext>
{
    /// <inheritdoc />
    public OperatorProfileDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OperatorProfileDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            OperatorProfileDbContext.Schema,
            typeof(OperatorProfileDbContext));
        return new OperatorProfileDbContext(options.Options);
    }
}
