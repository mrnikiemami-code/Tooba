using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T004-R22-R1 — سیاست هویت مشتری در فرایند خرید.</summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("20260913120000_AddStoreCheckoutIdentitySettings")]
public partial class AddStoreCheckoutIdentitySettings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS catalog.store_checkout_identity_settings
            (
                settings_id uuid NOT NULL,
                policy character varying(32) NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_store_checkout_identity_settings PRIMARY KEY (settings_id)
            );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS catalog.store_checkout_identity_settings;");
    }
}
