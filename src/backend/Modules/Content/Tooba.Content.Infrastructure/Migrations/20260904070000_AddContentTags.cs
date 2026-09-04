using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tags",
                schema: "content",
                columns: table => new
                {
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("pk_tags", x => x.tag_id));

            migrationBuilder.CreateIndex(
                name: "ix_tags_language_code_is_active_name",
                schema: "content",
                table: "tags",
                columns: new[] { "language_code", "is_active", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_tags_language_code_normalized_name",
                schema: "content",
                table: "tags",
                columns: new[] { "language_code", "normalized_name" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "article_tags",
                schema: "content",
                columns: table => new
                {
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_tags", x => new { x.article_id, x.tag_id });
                    table.ForeignKey(
                        name: "fk_article_tags_articles_article_id",
                        column: x => x.article_id,
                        principalSchema: "content",
                        principalTable: "articles",
                        principalColumn: "article_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_article_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "content",
                        principalTable: "tags",
                        principalColumn: "tag_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_article_tags_tag_id",
                schema: "content",
                table: "article_tags",
                column: "tag_id");

            // Safe TagsCsv → ContentTag + ArticleTag migration (trim/dedupe per article language).
            migrationBuilder.Sql("""
                WITH exploded AS (
                  SELECT
                    a.article_id,
                    a.locale AS language_code,
                    trim(both FROM t.tag_raw) AS name
                  FROM content.articles a
                  CROSS JOIN LATERAL unnest(string_to_array(COALESCE(a.tags_csv, ''), ',')) AS t(tag_raw)
                  WHERE COALESCE(a.tags_csv, '') <> ''
                    AND trim(both FROM t.tag_raw) <> ''
                ),
                normalized AS (
                  SELECT
                    article_id,
                    language_code,
                    name,
                    lower(regexp_replace(trim(both FROM name), '\s+', ' ', 'g')) AS normalized_name
                  FROM exploded
                ),
                distinct_tags AS (
                  SELECT DISTINCT ON (language_code, normalized_name)
                    language_code,
                    name,
                    normalized_name
                  FROM normalized
                  ORDER BY language_code, normalized_name, name
                ),
                inserted AS (
                  INSERT INTO content.tags (
                    tag_id, language_code, name, normalized_name, slug, is_active, created_at, updated_at
                  )
                  SELECT
                    gen_random_uuid(),
                    language_code,
                    name,
                    normalized_name,
                    NULL,
                    TRUE,
                    NOW(),
                    NOW()
                  FROM distinct_tags
                  ON CONFLICT DO NOTHING
                  RETURNING tag_id, language_code, normalized_name
                ),
                all_tags AS (
                  SELECT tag_id, language_code, normalized_name FROM content.tags
                )
                INSERT INTO content.article_tags (article_id, tag_id, assigned_at)
                SELECT DISTINCT n.article_id, t.tag_id, NOW()
                FROM normalized n
                INNER JOIN all_tags t
                  ON t.language_code = n.language_code
                 AND t.normalized_name = n.normalized_name
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "article_tags", schema: "content");
            migrationBuilder.DropTable(name: "tags", schema: "content");
        }
    }
}
