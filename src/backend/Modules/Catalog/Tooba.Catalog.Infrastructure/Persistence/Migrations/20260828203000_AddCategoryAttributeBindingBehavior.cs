using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryAttributeBindingBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_required",
                schema: "catalog",
                table: "category_attribute_bindings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_filterable",
                schema: "catalog",
                table: "category_attribute_bindings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_variant_axis",
                schema: "catalog",
                table: "category_attribute_bindings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_comparable",
                schema: "catalog",
                table: "category_attribute_bindings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE catalog.category_attribute_bindings AS b
                SET
                    is_required = COALESCE(b.is_required_override, d.is_required, false),
                    is_filterable = COALESCE(d.is_filterable, false),
                    is_comparable = COALESCE(d.is_comparable, false),
                    is_variant_axis = COALESCE(d.is_variant_axis, false)
                FROM catalog.attribute_definitions AS d
                WHERE b.definition_id = d.definition_id;
                """);

            migrationBuilder.DropColumn(
                name: "is_required_override",
                schema: "catalog",
                table: "category_attribute_bindings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_required_override",
                schema: "catalog",
                table: "category_attribute_bindings",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE catalog.category_attribute_bindings AS b
                SET is_required_override = b.is_required;
                """);

            migrationBuilder.DropColumn(
                name: "is_required",
                schema: "catalog",
                table: "category_attribute_bindings");

            migrationBuilder.DropColumn(
                name: "is_filterable",
                schema: "catalog",
                table: "category_attribute_bindings");

            migrationBuilder.DropColumn(
                name: "is_variant_axis",
                schema: "catalog",
                table: "category_attribute_bindings");

            migrationBuilder.DropColumn(
                name: "is_comparable",
                schema: "catalog",
                table: "category_attribute_bindings");
        }
    }
}
