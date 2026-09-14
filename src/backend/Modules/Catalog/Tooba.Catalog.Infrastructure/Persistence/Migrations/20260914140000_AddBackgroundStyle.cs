using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T015 — پس‌زمینهٔ کنترل‌شدهٔ فروشگاه.</summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("20260914140000_AddBackgroundStyle")]
public partial class AddBackgroundStyle : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE catalog.store_appearance_settings
            ADD COLUMN IF NOT EXISTS background_style character varying(16) NOT NULL DEFAULT 'Neutral';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE catalog.store_appearance_settings
            DROP COLUMN IF EXISTS background_style;
            """);
    }
}
