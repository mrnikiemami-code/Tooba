using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tooba.AddressBook.Domain.Aggregates;

namespace Tooba.AddressBook.Infrastructure.Persistence.Configurations;

/// <summary>نگاشت ماندگاری دانهٔ <see cref="CustomerAddress"/> در schema دفترچهٔ آدرس.</summary>
public sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CustomerAddress> entity)
    {
        entity.ToTable("customer_addresses");
        entity.HasKey(x => x.AddressId);
        entity.Property(x => x.AddressId).ValueGeneratedNever();
        entity.Property(x => x.RecipientName).HasMaxLength(CustomerAddress.RecipientNameMaxLength).IsRequired();
        entity.Property(x => x.FirstName).HasMaxLength(64);
        entity.Property(x => x.LastName).HasMaxLength(64);
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
    }
}
