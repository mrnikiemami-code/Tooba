using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Tooba.Fulfillment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(FulfillmentDbContext))]
    partial class FulfillmentDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("fulfillment");

            modelBuilder.Entity("Tooba.Fulfillment.Domain.ConsolidatedPackage", b =>
                {
                    b.Property<Guid>("ConsolidatedPackageId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset?>("CancelledAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("CheckoutId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid?>("CreatedBy")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset?>("DeliveredAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset?>("DispatchedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Note")
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)");

                    b.Property<string>("PackageNumber")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)");

                    b.Property<string>("ShippingMethodCode")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("ShippingMethodLabel")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)");

                    b.Property<string>("TrackingReference")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("ConsolidatedPackageId");

                    b.HasIndex("CheckoutId");

                    b.HasIndex("PackageNumber")
                        .IsUnique();

                    b.ToTable("consolidated_packages", "fulfillment");
                });

            modelBuilder.Entity("Tooba.Fulfillment.Domain.ConsolidatedPackageMember", b =>
                {
                    b.Property<Guid>("ConsolidatedPackageMemberId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("ConsolidatedPackageId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("FulfillmentId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("JoinedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset?>("ReleasedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("SellerPartyId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("ShipmentId")
                        .HasColumnType("uuid");

                    b.HasKey("ConsolidatedPackageMemberId");

                    b.HasIndex("ConsolidatedPackageId");

                    b.HasIndex("ShipmentId")
                        .IsUnique()
                        .HasFilter("released_at IS NULL");

                    b.ToTable("consolidated_package_members", "fulfillment");
                });
        }
    }
}
