using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Payment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Payment.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-P10-T003-R1 — ستون target_kind برای تفکیک تخصیص ارسال Store از SellerOrder.
/// </summary>
[DbContext(typeof(PaymentDbContext))]
[Migration("20260910180000_AddPaymentAllocationTargetKind")]
public partial class AddPaymentAllocationTargetKind : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "target_kind",
            schema: "payment",
            table: "allocations",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "SellerOrder");

        migrationBuilder.CreateIndex(
            name: "ix_allocations_payment_id_target_kind_seller_order_id",
            schema: "payment",
            table: "allocations",
            columns: new[] { "payment_id", "target_kind", "seller_order_id" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_allocations_payment_id_target_kind_seller_order_id",
            schema: "payment",
            table: "allocations");

        migrationBuilder.DropColumn(
            name: "target_kind",
            schema: "payment",
            table: "allocations");
    }
}
