using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// جبران migration خالی قبلی: ستون را فقط در صورت نبود اضافه می‌کند.
    /// </remarks>
    public partial class EnsureBrandLogoMediaAssetIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE catalog.brands
                ADD COLUMN IF NOT EXISTS logo_media_asset_id uuid NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE catalog.brands
                DROP COLUMN IF EXISTS logo_media_asset_id;
                """);
        }
    }
}
