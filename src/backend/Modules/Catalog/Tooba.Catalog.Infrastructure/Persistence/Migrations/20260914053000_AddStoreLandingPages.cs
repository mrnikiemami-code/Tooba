using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T010 — صفحات Landing و ارجاع HomePage.</summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("20260914053000_AddStoreLandingPages")]
public partial class AddStoreLandingPages : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS catalog.store_landing_pages (
                page_id uuid NOT NULL,
                locale character varying(16) NOT NULL,
                slug character varying(128) NOT NULL,
                title character varying(200) NOT NULL,
                seo_title character varying(200) NULL,
                seo_description character varying(500) NULL,
                template_key character varying(32) NOT NULL DEFAULT 'default',
                status character varying(16) NOT NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_store_landing_pages PRIMARY KEY (page_id)
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ux_store_landing_pages_locale_slug
                ON catalog.store_landing_pages (locale, slug);
            ALTER TABLE catalog.store_appearance_settings
            ADD COLUMN IF NOT EXISTS home_page_id uuid NULL;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE catalog.store_appearance_settings DROP COLUMN IF EXISTS home_page_id;
            DROP TABLE IF EXISTS catalog.store_landing_pages;
            """);
    }
}
