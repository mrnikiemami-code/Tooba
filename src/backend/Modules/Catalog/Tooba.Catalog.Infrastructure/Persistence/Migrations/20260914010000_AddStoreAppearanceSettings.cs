using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T005 — تنظیم ظاهری فروشگاه (پالت curated و حالت تم).</summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("20260914010000_AddStoreAppearanceSettings")]
public partial class AddStoreAppearanceSettings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS catalog.store_appearance_settings
            (
                settings_id uuid NOT NULL,
                palette_key character varying(32) NOT NULL,
                theme_mode character varying(16) NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_store_appearance_settings PRIMARY KEY (settings_id)
            );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS catalog.store_appearance_settings;");
    }
}
