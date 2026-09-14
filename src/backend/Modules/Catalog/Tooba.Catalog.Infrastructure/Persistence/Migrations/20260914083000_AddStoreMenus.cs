using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T013 — منو و آیتم منو + ارجاع هدر.</summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("20260914083000_AddStoreMenus")]
public partial class AddStoreMenus : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE catalog.store_appearance_settings
                ADD COLUMN IF NOT EXISTS header_menu_id uuid NULL;

            CREATE TABLE IF NOT EXISTS catalog.store_menus (
                menu_id uuid NOT NULL,
                title character varying(200) NOT NULL,
                locale character varying(16) NOT NULL,
                menu_key character varying(64) NOT NULL,
                is_enabled boolean NOT NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_store_menus PRIMARY KEY (menu_id)
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_store_menus_locale_menu_key
                ON catalog.store_menus (locale, menu_key);

            CREATE TABLE IF NOT EXISTS catalog.store_menu_items (
                menu_item_id uuid NOT NULL,
                menu_id uuid NOT NULL,
                parent_menu_item_id uuid NULL,
                label character varying(120) NOT NULL,
                link_type character varying(32) NOT NULL,
                target_id uuid NULL,
                external_url character varying(500) NULL,
                sort_order integer NOT NULL,
                is_enabled boolean NOT NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_store_menu_items PRIMARY KEY (menu_item_id)
            );
            CREATE INDEX IF NOT EXISTS ix_store_menu_items_menu_parent_sort
                ON catalog.store_menu_items (menu_id, parent_menu_item_id, sort_order);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS catalog.store_menu_items;
            DROP TABLE IF EXISTS catalog.store_menus;
            ALTER TABLE catalog.store_appearance_settings DROP COLUMN IF EXISTS header_menu_id;
            """);
    }
}
