using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                schema: "content",
                columns: table => new
                {
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    parent_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    short_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    seo_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    seo_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    image_media_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("pk_categories", x => x.category_id));

            migrationBuilder.CreateIndex(
                name: "ix_categories_language_code_parent_category_id_sort_order",
                schema: "content",
                table: "categories",
                columns: new[] { "language_code", "parent_category_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_categories_language_code_slug",
                schema: "content",
                table: "categories",
                columns: new[] { "language_code", "slug" },
                unique: true);

            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                schema: "content",
                table: "articles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_articles_category_id",
                schema: "content",
                table: "articles",
                column: "category_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ix_articles_category_id", schema: "content", table: "articles");
            migrationBuilder.DropColumn(name: "category_id", schema: "content", table: "articles");
            migrationBuilder.DropTable(name: "categories", schema: "content");
        }
    }
}
