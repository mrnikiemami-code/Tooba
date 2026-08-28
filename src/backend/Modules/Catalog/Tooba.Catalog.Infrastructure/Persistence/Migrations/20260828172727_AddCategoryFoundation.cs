using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_categories_parent_category_id",
                schema: "catalog",
                table: "categories");

            migrationBuilder.AddColumn<Guid>(
                name: "icon_media_asset_id",
                schema: "catalog",
                table: "categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "image_media_asset_id",
                schema: "catalog",
                table: "categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_visible",
                schema: "catalog",
                table: "categories",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                schema: "catalog",
                table: "categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "category_slug_histories",
                schema: "catalog",
                columns: table => new
                {
                    history_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    old_slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category_slug_histories", x => x.history_id);
                    table.ForeignKey(
                        name: "fk_category_slug_histories_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "category_translations",
                schema: "catalog",
                columns: table => new
                {
                    translation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    short_description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    seo_title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    seo_description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    meta_keywords = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category_translations", x => x.translation_id);
                    table.ForeignKey(
                        name: "fk_category_translations_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_category_id_sort_order",
                schema: "catalog",
                table: "categories",
                columns: new[] { "parent_category_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_category_slug_histories_category_id",
                schema: "catalog",
                table: "category_slug_histories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_category_slug_histories_locale_old_slug",
                schema: "catalog",
                table: "category_slug_histories",
                columns: new[] { "locale", "old_slug" });

            migrationBuilder.CreateIndex(
                name: "ix_category_translations_category_id_locale",
                schema: "catalog",
                table: "category_translations",
                columns: new[] { "category_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_category_translations_locale_slug",
                schema: "catalog",
                table: "category_translations",
                columns: new[] { "locale", "slug" },
                unique: true);

            // Backfill: Category LocalizedText name → translations (slug = normalized name + short id for uniqueness).
            migrationBuilder.Sql(
                """
                INSERT INTO catalog.category_translations (
                    translation_id,
                    category_id,
                    locale,
                    name,
                    slug,
                    short_description,
                    description,
                    seo_title,
                    seo_description,
                    meta_keywords,
                    updated_at)
                SELECT
                    gen_random_uuid(),
                    lt.owner_id,
                    btrim(lt.locale),
                    btrim(lt.value),
                    lower(regexp_replace(btrim(lt.value), '[[:space:]_/\\]+', '-', 'g'))
                        || '-' || substr(replace(lt.owner_id::text, '-', ''), 1, 8),
                    NULL,
                    NULL,
                    NULL,
                    NULL,
                    NULL,
                    NOW()
                FROM catalog.localized_texts lt
                WHERE lt.owner_kind = 'Category'
                  AND lt.field_key = 'name'
                  AND btrim(lt.value) <> ''
                  AND NOT EXISTS (
                      SELECT 1
                      FROM catalog.category_translations ct
                      WHERE ct.category_id = lt.owner_id
                        AND ct.locale = btrim(lt.locale));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_slug_histories",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "category_translations",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "ix_categories_parent_category_id_sort_order",
                schema: "catalog",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "icon_media_asset_id",
                schema: "catalog",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "image_media_asset_id",
                schema: "catalog",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "is_visible",
                schema: "catalog",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "sort_order",
                schema: "catalog",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_category_id",
                schema: "catalog",
                table: "categories",
                column: "parent_category_id");
        }
    }
}
