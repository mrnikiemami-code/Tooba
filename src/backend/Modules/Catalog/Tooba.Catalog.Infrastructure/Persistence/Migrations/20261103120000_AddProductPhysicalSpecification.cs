using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CatalogDbContext))]
    [Migration("20261103120000_AddProductPhysicalSpecification")]
    public partial class AddProductPhysicalSpecification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "weight_grams",
                schema: "catalog",
                table: "products",
                type: "numeric(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "length_centimeters",
                schema: "catalog",
                table: "products",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "width_centimeters",
                schema: "catalog",
                table: "products",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "height_centimeters",
                schema: "catalog",
                table: "products",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "weight_grams",
                schema: "catalog",
                table: "variants",
                type: "numeric(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "length_centimeters",
                schema: "catalog",
                table: "variants",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "width_centimeters",
                schema: "catalog",
                table: "variants",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "height_centimeters",
                schema: "catalog",
                table: "variants",
                type: "numeric(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "weight_grams",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "length_centimeters",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "width_centimeters",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "height_centimeters",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "weight_grams",
                schema: "catalog",
                table: "variants");

            migrationBuilder.DropColumn(
                name: "length_centimeters",
                schema: "catalog",
                table: "variants");

            migrationBuilder.DropColumn(
                name: "width_centimeters",
                schema: "catalog",
                table: "variants");

            migrationBuilder.DropColumn(
                name: "height_centimeters",
                schema: "catalog",
                table: "variants");
        }
    }
}
