using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentAuthors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authors",
                schema: "content",
                columns: table => new
                {
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    profile_image_media_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cover_image_media_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    short_bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    full_bio = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    website_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    instagram_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    twitter_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    linked_in_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("pk_authors", x => x.author_id));

            migrationBuilder.CreateIndex(
                name: "ix_authors_is_active_display_name",
                schema: "content",
                table: "authors",
                columns: new[] { "is_active", "display_name" });

            migrationBuilder.CreateIndex(
                name: "ix_authors_slug",
                schema: "content",
                table: "authors",
                column: "slug",
                unique: true);

            migrationBuilder.AddColumn<Guid>(
                name: "author_id",
                schema: "content",
                table: "articles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_articles_author_id",
                schema: "content",
                table: "articles",
                column: "author_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ix_articles_author_id", schema: "content", table: "articles");
            migrationBuilder.DropColumn(name: "author_id", schema: "content", table: "articles");
            migrationBuilder.DropTable(name: "authors", schema: "content");
        }
    }
}
