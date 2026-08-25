using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using CustomerProfileEntity = Tooba.CustomerProfile.Domain.CustomerProfile;
using Tooba.Persistence;

namespace Tooba.CustomerProfile.Infrastructure.Persistence;

/// <summary>DbContext مالک schema مستقل customer_profile و Outbox همان ماژول.</summary>
public sealed class CustomerProfileDbContext : DbContext
{
    /// <summary>نام schema اختصاصی پروفایل مشتری.</summary>
    public const string Schema = "customer_profile";

    /// <summary>DbContext را با گزینه‌های ماژول می‌سازد.</summary>
    public CustomerProfileDbContext(DbContextOptions<CustomerProfileDbContext> options) : base(options)
    {
    }

    /// <summary>ردیف‌های خصوصی پروفایل مشتری.</summary>
    public DbSet<CustomerProfileEntity> Profiles => Set<CustomerProfileEntity>();

    /// <summary>پیام‌های Outbox ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<CustomerProfileEntity>(entity =>
        {
            entity.ToTable("customer_profiles");
            entity.HasKey(x => x.OwnerUserId);
            entity.Property(x => x.OwnerUserId).ValueGeneratedNever();
            entity.Property(x => x.FirstName).HasMaxLength(CustomerProfileEntity.NamePartMaxLength).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(CustomerProfileEntity.NamePartMaxLength).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(CustomerProfileEntity.DisplayNameMaxLength).IsRequired();
            entity.Property(x => x.BirthDate).HasMaxLength(CustomerProfileEntity.BirthDateMaxLength);
            entity.Property(x => x.Bio).HasMaxLength(CustomerProfileEntity.BioMaxLength);
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ design-time مهاجرت‌های CustomerProfile.</summary>
public sealed class CustomerProfileDbContextFactory : IDesignTimeDbContextFactory<CustomerProfileDbContext>
{
    /// <inheritdoc />
    public CustomerProfileDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CustomerProfileDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            CustomerProfileDbContext.Schema,
            typeof(CustomerProfileDbContext));
        return new CustomerProfileDbContext(options.Options);
    }
}
