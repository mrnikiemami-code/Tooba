using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "seo_image_media_asset_id",
                schema: "content",
                table: "articles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "article_media",
                schema: "content",
                columns: table => new
                {
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    alt_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_media", x => new { x.article_id, x.media_asset_id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_article_media_article_id_display_order",
                schema: "content",
                table: "article_media",
                columns: new[] { "article_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_article_media_media_asset_id",
                schema: "content",
                table: "article_media",
                column: "media_asset_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "article_media", schema: "content");
            migrationBuilder.DropColumn(name: "seo_image_media_asset_id", schema: "content", table: "articles");
        }
    }
}
