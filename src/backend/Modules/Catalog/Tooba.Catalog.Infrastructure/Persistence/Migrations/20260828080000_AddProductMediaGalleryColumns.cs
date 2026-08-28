using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductMediaGalleryColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "alt_text",
                schema: "catalog",
                table: "product_media_references",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "display_order",
                schema: "catalog",
                table: "product_media_references",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_primary",
                schema: "catalog",
                table: "product_media_references",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_product_media_references_product_id_display_order",
                schema: "catalog",
                table: "product_media_references",
                columns: new[] { "product_id", "display_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_product_media_references_product_id_display_order",
                schema: "catalog",
                table: "product_media_references");

            migrationBuilder.DropColumn(
                name: "alt_text",
                schema: "catalog",
                table: "product_media_references");

            migrationBuilder.DropColumn(
                name: "display_order",
                schema: "catalog",
                table: "product_media_references");

            migrationBuilder.DropColumn(
                name: "is_primary",
                schema: "catalog",
                table: "product_media_references");
        }
    }
}
