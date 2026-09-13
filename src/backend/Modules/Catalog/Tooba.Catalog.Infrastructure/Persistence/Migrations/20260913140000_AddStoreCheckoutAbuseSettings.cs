using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T004-R23 — سقف سفارش باز و سهمیه شروع رزرو.</summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("20260913140000_AddStoreCheckoutAbuseSettings")]
public partial class AddStoreCheckoutAbuseSettings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS catalog.store_checkout_abuse_settings
            (
                settings_id uuid NOT NULL,
                max_open_unpaid_orders_per_customer integer NOT NULL,
                reservation_commit_window_minutes integer NOT NULL,
                max_checkout_commits_per_customer_in_window integer NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_store_checkout_abuse_settings PRIMARY KEY (settings_id)
            );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS catalog.store_checkout_abuse_settings;");
    }
}
