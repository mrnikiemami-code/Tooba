using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CatalogDbContext))]
    [Migration("20260909130000_AddQuantityFoundation")]
    public partial class AddQuantityFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "units_of_measure",
                schema: "catalog",
                columns: table => new
                {
                    unit_of_measure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dimension = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_units_of_measure", x => x.unit_of_measure_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_units_of_measure_code",
                schema: "catalog",
                table: "units_of_measure",
                column: "code",
                unique: true);

            migrationBuilder.CreateTable(
                name: "unit_of_measure_translations",
                schema: "catalog",
                columns: table => new
                {
                    translation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_of_measure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    short_name = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unit_of_measure_translations", x => x.translation_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_unit_of_measure_translations_unit_language",
                schema: "catalog",
                table: "unit_of_measure_translations",
                columns: new[] { "unit_of_measure_id", "language_id" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "store_quantity_settings",
                schema: "catalog",
                columns: table => new
                {
                    settings_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rounding_mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_store_quantity_settings", x => x.settings_id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "unit_of_measure_id",
                schema: "catalog",
                table: "products",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Parse("01900000-0000-7000-8000-000000000001"));

            migrationBuilder.AddColumn<int>(
                name: "quantity_decimal_places",
                schema: "catalog",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_step",
                schema: "catalog",
                table: "products",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO catalog.units_of_measure (unit_of_measure_id, code, dimension, is_active, sort_order, created_at, updated_at)
                VALUES
                  ('01900000-0000-7000-8000-000000000001', 'pcs', 'Count', TRUE, 1, NOW(), NOW()),
                  ('01900000-0000-7000-8000-000000000002', 'kg', 'Mass', TRUE, 2, NOW(), NOW()),
                  ('01900000-0000-7000-8000-000000000003', 'g', 'Mass', TRUE, 3, NOW(), NOW())
                ON CONFLICT (unit_of_measure_id) DO NOTHING;

                INSERT INTO catalog.store_quantity_settings (settings_id, rounding_mode, updated_at)
                VALUES ('01900000-0000-7000-8000-00000000aa01', 'Nearest', NOW())
                ON CONFLICT (settings_id) DO NOTHING;

                DO $seed$
                BEGIN
                  IF to_regclass('localization.languages') IS NOT NULL THEN
                    INSERT INTO catalog.unit_of_measure_translations (translation_id, unit_of_measure_id, language_id, name, short_name)
                    SELECT gen_random_uuid(), u.unit_of_measure_id, l.language_id, t.name, t.short_name
                    FROM catalog.units_of_measure u
                    JOIN localization.languages l ON lower(l.code) IN ('fa', 'fa-ir', 'en', 'en-us')
                    JOIN (VALUES
                      ('pcs', 'fa', 'عدد', 'عدد'),
                      ('pcs', 'en', 'Piece', 'pcs'),
                      ('kg', 'fa', 'کیلوگرم', 'کیلو'),
                      ('kg', 'en', 'Kilogram', 'kg'),
                      ('g', 'fa', 'گرم', 'گرم'),
                      ('g', 'en', 'Gram', 'g')
                    ) AS t(code, lang, name, short_name)
                      ON u.code = t.code
                     AND (lower(l.code) = t.lang OR lower(l.code) LIKE t.lang || '-%')
                    ON CONFLICT DO NOTHING;
                  END IF;
                END
                $seed$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "quantity_step", schema: "catalog", table: "products");
            migrationBuilder.DropColumn(name: "quantity_decimal_places", schema: "catalog", table: "products");
            migrationBuilder.DropColumn(name: "unit_of_measure_id", schema: "catalog", table: "products");
            migrationBuilder.DropTable(name: "unit_of_measure_translations", schema: "catalog");
            migrationBuilder.DropTable(name: "store_quantity_settings", schema: "catalog");
            migrationBuilder.DropTable(name: "units_of_measure", schema: "catalog");
        }
    }
}
