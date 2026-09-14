using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T011 — بخش‌های Landing.</summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("20260914060000_AddStoreLandingPageSections")]
public partial class AddStoreLandingPageSections : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS catalog.store_landing_page_sections (
                page_section_id uuid NOT NULL,
                page_id uuid NOT NULL,
                section_type character varying(64) NOT NULL,
                sort_order integer NOT NULL,
                is_enabled boolean NOT NULL,
                configuration_json character varying(4000) NOT NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_store_landing_page_sections PRIMARY KEY (page_section_id)
            );
            CREATE INDEX IF NOT EXISTS ix_store_landing_page_sections_page_id_sort
                ON catalog.store_landing_page_sections (page_id, sort_order);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS catalog.store_landing_page_sections;");
    }
}
