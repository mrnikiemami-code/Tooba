using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CatalogDbContext))]
    [Migration("20260830090000_AddCatalogTagFoundation")]
    public partial class AddCatalogTagFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tags",
                schema: "catalog",
                columns: table => new
                {
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    slug_seam = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.tag_id);
                });

            migrationBuilder.CreateTable(
                name: "category_tag_assignments",
                schema: "catalog",
                columns: table => new
                {
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category_tag_assignments", x => x.assignment_id);
                    table.ForeignKey(
                        name: "fk_category_tag_assignments_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_category_tag_assignments_tags_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "catalog",
                        principalTable: "tags",
                        principalColumn: "tag_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_tag_assignments",
                schema: "catalog",
                columns: table => new
                {
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_tag_assignments", x => x.assignment_id);
                    table.ForeignKey(
                        name: "fk_product_tag_assignments_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_tag_assignments_tags_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "catalog",
                        principalTable: "tags",
                        principalColumn: "tag_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tags_code",
                schema: "catalog",
                table: "tags",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_category_tag_assignments_category_id_tag_id",
                schema: "catalog",
                table: "category_tag_assignments",
                columns: new[] { "category_id", "tag_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_category_tag_assignments_tag_id",
                schema: "catalog",
                table: "category_tag_assignments",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_tag_assignments_product_id_tag_id",
                schema: "catalog",
                table: "product_tag_assignments",
                columns: new[] { "product_id", "tag_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_tag_assignments_tag_id",
                schema: "catalog",
                table: "product_tag_assignments",
                column: "tag_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_tag_assignments",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "product_tag_assignments",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "tags",
                schema: "catalog");
        }
    }
}
