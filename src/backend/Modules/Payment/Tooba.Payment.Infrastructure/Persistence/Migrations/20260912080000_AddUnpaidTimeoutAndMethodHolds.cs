using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Payment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Payment.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T004-R10 — مهلت unpaid روی پرداخت + override روش پرداخت.</summary>
[DbContext(typeof(PaymentDbContext))]
[Migration("20260912080000_AddUnpaidTimeoutAndMethodHolds")]
public partial class AddUnpaidTimeoutAndMethodHolds : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "unpaid_timeout_at",
            schema: "payment",
            table: "payments",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_payments_status_unpaid_timeout_at",
            schema: "payment",
            table: "payments",
            columns: new[] { "status", "unpaid_timeout_at" });

        migrationBuilder.CreateTable(
            name: "payment_method_hold_overrides",
            schema: "payment",
            columns: table => new
            {
                provider_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                online_payment_hold_hours = table.Column<int>(type: "integer", nullable: true),
                manual_payment_initial_hold_hours = table.Column<int>(type: "integer", nullable: true),
                manual_payment_review_hold_hours = table.Column<int>(type: "integer", nullable: true),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_payment_method_hold_overrides", x => x.provider_code);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "payment_method_hold_overrides", schema: "payment");
        migrationBuilder.DropIndex(
            name: "ix_payments_status_unpaid_timeout_at",
            schema: "payment",
            table: "payments");
        migrationBuilder.DropColumn(name: "unpaid_timeout_at", schema: "payment", table: "payments");
    }
}
