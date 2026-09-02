using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ArticleSlugLocaleUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_articles_slug",
                schema: "content",
                table: "articles");

            migrationBuilder.CreateIndex(
                name: "ix_articles_slug_locale",
                schema: "content",
                table: "articles",
                columns: new[] { "slug", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_articles_locale_slug",
                schema: "content",
                table: "articles",
                columns: new[] { "locale", "slug" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_articles_locale_slug",
                schema: "content",
                table: "articles");

            migrationBuilder.DropIndex(
                name: "ix_articles_slug_locale",
                schema: "content",
                table: "articles");

            migrationBuilder.CreateIndex(
                name: "ix_articles_slug",
                schema: "content",
                table: "articles",
                column: "slug",
                unique: true);
        }
    }
}
