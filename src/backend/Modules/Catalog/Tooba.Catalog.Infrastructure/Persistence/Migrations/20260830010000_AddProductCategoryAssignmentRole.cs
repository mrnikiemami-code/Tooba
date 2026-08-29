using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CatalogDbContext))]
    [Migration("20260830010000_AddProductCategoryAssignmentRole")]
    public partial class AddProductCategoryAssignmentRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "role",
                schema: "catalog",
                table: "product_categories",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.Sql(
                """
                UPDATE catalog.product_categories SET role = 0;
                """);

            migrationBuilder.Sql(
                """
                WITH ranked AS (
                  SELECT assignment_id,
                         ROW_NUMBER() OVER (PARTITION BY product_id ORDER BY assignment_id) AS rn
                  FROM catalog.product_categories
                )
                UPDATE catalog.product_categories pc
                SET role = CASE WHEN ranked.rn = 1 THEN 0 ELSE 1 END
                FROM ranked
                WHERE pc.assignment_id = ranked.assignment_id;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_product_categories_category_id_role",
                schema: "catalog",
                table: "product_categories",
                columns: new[] { "category_id", "role" });

            migrationBuilder.CreateIndex(
                name: "ix_product_categories_one_primary_per_product",
                schema: "catalog",
                table: "product_categories",
                column: "product_id",
                unique: true,
                filter: "\"role\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_product_categories_one_primary_per_product",
                schema: "catalog",
                table: "product_categories");

            migrationBuilder.DropIndex(
                name: "ix_product_categories_category_id_role",
                schema: "catalog",
                table: "product_categories");

            migrationBuilder.DropColumn(
                name: "role",
                schema: "catalog",
                table: "product_categories");
        }
    }
}
