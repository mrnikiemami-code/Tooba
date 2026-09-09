using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Fulfillment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(FulfillmentDbContext))]
    [Migration("20260907121000_AddShipmentProviderMetadata")]
    public partial class AddShipmentProviderMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Replay-safe: tooba_alpha already has these T006 columns while
            // fulfillment.__ef_migrations_history omitted this row.
            migrationBuilder.Sql(
                """
                ALTER TABLE fulfillment.shipments
                    ADD COLUMN IF NOT EXISTS shipping_method_code character varying(64) NOT NULL DEFAULT '';
                ALTER TABLE fulfillment.shipments
                    ADD COLUMN IF NOT EXISTS shipping_method_label character varying(128) NOT NULL DEFAULT '';
                ALTER TABLE fulfillment.shipments
                    ADD COLUMN IF NOT EXISTS provider_metadata_json text NULL;
                ALTER TABLE fulfillment.shipments
                    ADD COLUMN IF NOT EXISTS provider_metadata_version integer NOT NULL DEFAULT 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "provider_metadata_version",
                schema: "fulfillment",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "provider_metadata_json",
                schema: "fulfillment",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "shipping_method_label",
                schema: "fulfillment",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "shipping_method_code",
                schema: "fulfillment",
                table: "shipments");
        }
    }
}
