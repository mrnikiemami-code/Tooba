using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.AddressBook.Domain;
using Tooba.Persistence;

namespace Tooba.AddressBook.Infrastructure.Persistence;

/// <summary>DbContext مالک schema مستقل address_book و Outbox همان ماژول.</summary>
public sealed class AddressBookDbContext : DbContext
{
    /// <summary>نام schema اختصاصی دفترچهٔ آدرس.</summary>
    public const string Schema = "address_book";

    /// <summary>DbContext را با گزینه‌های ماژول می‌سازد.</summary>
    public AddressBookDbContext(DbContextOptions<AddressBookDbContext> options) : base(options)
    {
    }

    /// <summary>ردیف‌های خصوصی نشانی مشتری.</summary>
    public DbSet<CustomerAddress> Addresses => Set<CustomerAddress>();

    /// <summary>پیام‌های Outbox ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<CustomerAddress>(entity =>
        {
            entity.ToTable("customer_addresses");
            entity.HasKey(x => x.AddressId);
            entity.Property(x => x.AddressId).ValueGeneratedNever();
            entity.Property(x => x.RecipientName).HasMaxLength(CustomerAddress.RecipientNameMaxLength).IsRequired();
            entity.Property(x => x.ContactMobile).HasMaxLength(CustomerAddress.ContactMobileMaxLength).IsRequired();
            entity.Property(x => x.Country).HasMaxLength(CustomerAddress.CountryMaxLength).IsRequired();
            entity.Property(x => x.ProvinceName).HasMaxLength(CustomerAddress.ProvinceNameMaxLength);
            entity.Property(x => x.CityName).HasMaxLength(CustomerAddress.CityNameMaxLength).IsRequired();
            entity.Property(x => x.PostalCode).HasMaxLength(CustomerAddress.PostalCodeMaxLength).IsRequired();
            entity.Property(x => x.PostalAddress).HasMaxLength(CustomerAddress.PostalAddressMaxLength).IsRequired();
            entity.Property(x => x.BuildingUnit).HasMaxLength(CustomerAddress.BuildingUnitMaxLength);
            entity.Property(x => x.Label).HasMaxLength(CustomerAddress.LabelMaxLength);
            entity.HasIndex(x => new { x.OwnerUserId, x.CreatedAt });
            entity.HasIndex(x => x.OwnerUserId)
                .IsUnique()
                .HasFilter("is_default = TRUE")
                .HasDatabaseName("ix_customer_addresses_one_default_per_owner");
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ design-time مهاجرت‌های AddressBook.</summary>
public sealed class AddressBookDbContextFactory : IDesignTimeDbContextFactory<AddressBookDbContext>
{
    /// <inheritdoc />
    public AddressBookDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AddressBookDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            AddressBookDbContext.Schema,
            typeof(AddressBookDbContext));
        return new AddressBookDbContext(options.Options);
    }
}
