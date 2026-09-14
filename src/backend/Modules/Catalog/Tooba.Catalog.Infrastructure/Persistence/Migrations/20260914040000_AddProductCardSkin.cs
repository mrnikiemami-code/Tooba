using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T009 — پوستهٔ کنترل‌شدهٔ کارت کالا.</summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("20260914040000_AddProductCardSkin")]
public partial class AddProductCardSkin : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE catalog.store_appearance_settings
            ADD COLUMN IF NOT EXISTS product_card_skin character varying(16) NOT NULL DEFAULT 'classic';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE catalog.store_appearance_settings
            DROP COLUMN IF EXISTS product_card_skin;
            """);
    }
}
