using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Fulfillment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(FulfillmentDbContext))]
    [Migration("20260910120000_AddConsolidatedPackages")]
    public partial class AddConsolidatedPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS fulfillment.consolidated_packages
                (
                    consolidated_package_id uuid NOT NULL,
                    package_number character varying(32) NOT NULL,
                    checkout_id uuid NOT NULL,
                    status character varying(32) NOT NULL,
                    shipping_method_code character varying(64) NOT NULL,
                    shipping_method_label character varying(128) NOT NULL,
                    tracking_reference character varying(128) NULL,
                    note character varying(512) NULL,
                    created_by uuid NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    dispatched_at timestamp with time zone NULL,
                    delivered_at timestamp with time zone NULL,
                    cancelled_at timestamp with time zone NULL,
                    CONSTRAINT pk_consolidated_packages PRIMARY KEY (consolidated_package_id)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_consolidated_packages_package_number
                    ON fulfillment.consolidated_packages (package_number);

                CREATE INDEX IF NOT EXISTS ix_consolidated_packages_checkout_id
                    ON fulfillment.consolidated_packages (checkout_id);

                CREATE TABLE IF NOT EXISTS fulfillment.consolidated_package_members
                (
                    consolidated_package_member_id uuid NOT NULL,
                    consolidated_package_id uuid NOT NULL,
                    shipment_id uuid NOT NULL,
                    seller_party_id uuid NOT NULL,
                    fulfillment_id uuid NOT NULL,
                    joined_at timestamp with time zone NOT NULL,
                    released_at timestamp with time zone NULL,
                    CONSTRAINT pk_consolidated_package_members PRIMARY KEY (consolidated_package_member_id)
                );

                CREATE INDEX IF NOT EXISTS ix_consolidated_package_members_package_id
                    ON fulfillment.consolidated_package_members (consolidated_package_id);

                CREATE UNIQUE INDEX IF NOT EXISTS ix_consolidated_package_members_shipment_active
                    ON fulfillment.consolidated_package_members (shipment_id)
                    WHERE released_at IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS fulfillment.consolidated_package_members;
                DROP TABLE IF EXISTS fulfillment.consolidated_packages;
                """);
        }
    }
}
