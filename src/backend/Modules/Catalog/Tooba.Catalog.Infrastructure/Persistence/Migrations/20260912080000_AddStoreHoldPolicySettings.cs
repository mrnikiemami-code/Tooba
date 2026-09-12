using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T004-R10 — override فروشگاه برای ماندگاری سبد و مهلت پرداخت.</summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("20260912080000_AddStoreHoldPolicySettings")]
public partial class AddStoreHoldPolicySettings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "store_hold_policy_settings",
            schema: "catalog",
            columns: table => new
            {
                settings_id = table.Column<Guid>(type: "uuid", nullable: false),
                cart_persistence_hours = table.Column<int>(type: "integer", nullable: true),
                online_payment_hold_hours = table.Column<int>(type: "integer", nullable: true),
                manual_payment_initial_hold_hours = table.Column<int>(type: "integer", nullable: true),
                manual_payment_review_hold_hours = table.Column<int>(type: "integer", nullable: true),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_store_hold_policy_settings", x => x.settings_id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "store_hold_policy_settings", schema: "catalog");
    }
}
