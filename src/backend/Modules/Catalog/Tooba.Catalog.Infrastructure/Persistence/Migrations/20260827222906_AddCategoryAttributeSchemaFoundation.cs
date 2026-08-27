using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryAttributeSchemaFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "display_order",
                schema: "catalog",
                table: "attribute_options",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "catalog",
                table: "attribute_options",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "display_order",
                schema: "catalog",
                table: "attribute_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "catalog",
                table: "attribute_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_comparable",
                schema: "catalog",
                table: "attribute_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_filterable",
                schema: "catalog",
                table: "attribute_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_multivalue",
                schema: "catalog",
                table: "attribute_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_required",
                schema: "catalog",
                table: "attribute_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "unit",
                schema: "catalog",
                table: "attribute_definitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "validation_max",
                schema: "catalog",
                table: "attribute_definitions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "validation_max_length",
                schema: "catalog",
                table: "attribute_definitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "validation_min",
                schema: "catalog",
                table: "attribute_definitions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "category_attribute_bindings",
                schema: "catalog",
                columns: table => new
                {
                    binding_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_required_override = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category_attribute_bindings", x => x.binding_id);
                    table.ForeignKey(
                        name: "fk_category_attribute_bindings_attribute_definitions_definitio",
                        column: x => x.definition_id,
                        principalSchema: "catalog",
                        principalTable: "attribute_definitions",
                        principalColumn: "definition_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_category_attribute_bindings_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_variant_axes",
                schema: "catalog",
                columns: table => new
                {
                    axis_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_variant_axes", x => x.axis_id);
                    table.ForeignKey(
                        name: "fk_product_variant_axes_attribute_definitions_definition_id",
                        column: x => x.definition_id,
                        principalSchema: "catalog",
                        principalTable: "attribute_definitions",
                        principalColumn: "definition_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_variant_axes_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_category_attribute_bindings_category_id_definition_id",
                schema: "catalog",
                table: "category_attribute_bindings",
                columns: new[] { "category_id", "definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_category_attribute_bindings_definition_id",
                schema: "catalog",
                table: "category_attribute_bindings",
                column: "definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_variant_axes_definition_id",
                schema: "catalog",
                table: "product_variant_axes",
                column: "definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_variant_axes_product_id_definition_id",
                schema: "catalog",
                table: "product_variant_axes",
                columns: new[] { "product_id", "definition_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_attribute_bindings",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "product_variant_axes",
                schema: "catalog");

            migrationBuilder.DropColumn(
                name: "display_order",
                schema: "catalog",
                table: "attribute_options");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "catalog",
                table: "attribute_options");

            migrationBuilder.DropColumn(
                name: "display_order",
                schema: "catalog",
                table: "attribute_definitions");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "catalog",
                table: "attribute_definitions");

            migrationBuilder.DropColumn(
                name: "is_comparable",
                schema: "catalog",
                table: "attribute_definitions");

            migrationBuilder.DropColumn(
                name: "is_filterable",
                schema: "catalog",
                table: "attribute_definitions");

            migrationBuilder.DropColumn(
                name: "is_multivalue",
                schema: "catalog",
                table: "attribute_definitions");

            migrationBuilder.DropColumn(
                name: "is_required",
                schema: "catalog",
                table: "attribute_definitions");

            migrationBuilder.DropColumn(
                name: "unit",
                schema: "catalog",
                table: "attribute_definitions");

            migrationBuilder.DropColumn(
                name: "validation_max",
                schema: "catalog",
                table: "attribute_definitions");

            migrationBuilder.DropColumn(
                name: "validation_max_length",
                schema: "catalog",
                table: "attribute_definitions");

            migrationBuilder.DropColumn(
                name: "validation_min",
                schema: "catalog",
                table: "attribute_definitions");
        }
    }
}
