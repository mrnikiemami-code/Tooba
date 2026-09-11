using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Payment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Payment.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-P10-T004-R2 — شماره پیگیری و مدرک کارت‌به‌کارت روی تلاش پرداخت + جدول اتصال دارایی.
/// </summary>
[DbContext(typeof(PaymentDbContext))]
[Migration("20260911080000_AddManualPaymentEvidence")]
public partial class AddManualPaymentEvidence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "customer_transfer_reference",
            schema: "payment",
            table: "attempts",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "proof_media_asset_id",
            schema: "payment",
            table: "attempts",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "evidence_submitted_at",
            schema: "payment",
            table: "attempts",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "proof_assets",
            schema: "payment",
            columns: table => new
            {
                proof_asset_row_id = table.Column<Guid>(type: "uuid", nullable: false),
                payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                media_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_proof_assets", x => x.proof_asset_row_id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_proof_assets_media_asset_id",
            schema: "payment",
            table: "proof_assets",
            column: "media_asset_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_proof_assets_payment_id",
            schema: "payment",
            table: "proof_assets",
            column: "payment_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "proof_assets", schema: "payment");
        migrationBuilder.DropColumn(name: "customer_transfer_reference", schema: "payment", table: "attempts");
        migrationBuilder.DropColumn(name: "proof_media_asset_id", schema: "payment", table: "attempts");
        migrationBuilder.DropColumn(name: "evidence_submitted_at", schema: "payment", table: "attempts");
    }
}
