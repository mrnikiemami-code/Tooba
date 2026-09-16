using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T022-R9 — Store Page Type + production SEO fields.</summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("20260916093000_AddStorePageTypeAndSeo")]
public partial class AddStorePageTypeAndSeo : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE catalog.store_landing_pages
                ADD COLUMN IF NOT EXISTS page_type character varying(16) NOT NULL DEFAULT 'Landing',
                ADD COLUMN IF NOT EXISTS robots_index boolean NOT NULL DEFAULT TRUE,
                ADD COLUMN IF NOT EXISTS robots_follow boolean NOT NULL DEFAULT TRUE,
                ADD COLUMN IF NOT EXISTS canonical_url character varying(500) NULL,
                ADD COLUMN IF NOT EXISTS og_title character varying(200) NULL,
                ADD COLUMN IF NOT EXISTS og_description character varying(500) NULL,
                ADD COLUMN IF NOT EXISTS og_image_url character varying(500) NULL,
                ADD COLUMN IF NOT EXISTS primary_h1 character varying(200) NULL;

            UPDATE catalog.store_landing_pages
            SET page_type = 'Landing'
            WHERE page_type IS NULL OR page_type = '' OR page_type NOT IN ('Home', 'Landing');

            UPDATE catalog.store_landing_pages p
            SET page_type = 'Home'
            FROM catalog.store_appearance_settings s
            WHERE s.home_page_id IS NOT NULL
              AND p.page_id = s.home_page_id;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE catalog.store_landing_pages
                DROP COLUMN IF EXISTS page_type,
                DROP COLUMN IF EXISTS robots_index,
                DROP COLUMN IF EXISTS robots_follow,
                DROP COLUMN IF EXISTS canonical_url,
                DROP COLUMN IF EXISTS og_title,
                DROP COLUMN IF EXISTS og_description,
                DROP COLUMN IF EXISTS og_image_url,
                DROP COLUMN IF EXISTS primary_h1;
            """);
    }
}
