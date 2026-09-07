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
            migrationBuilder.AddColumn<string>(
                name: "shipping_method_code",
                schema: "fulfillment",
                table: "shipments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_method_label",
                schema: "fulfillment",
                table: "shipments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "provider_metadata_json",
                schema: "fulfillment",
                table: "shipments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "provider_metadata_version",
                schema: "fulfillment",
                table: "shipments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
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
