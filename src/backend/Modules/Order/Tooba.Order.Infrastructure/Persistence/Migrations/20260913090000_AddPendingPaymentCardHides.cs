using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T004-R21-R1 — ترجیح پنهان‌کردن کارت در انتظار پرداخت.</summary>
[DbContext(typeof(OrderDbContext))]
[Migration("20260913090000_AddPendingPaymentCardHides")]
public partial class AddPendingPaymentCardHides : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "order".pending_payment_card_hides
            (
                hide_id uuid NOT NULL,
                checkout_id uuid NOT NULL,
                owner_user_id uuid NULL,
                guest_cart_id uuid NULL,
                hidden_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_pending_payment_card_hides PRIMARY KEY (hide_id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_pending_hides_owner_checkout
                ON "order".pending_payment_card_hides (owner_user_id, checkout_id)
                WHERE owner_user_id IS NOT NULL;

            CREATE UNIQUE INDEX IF NOT EXISTS ux_pending_hides_guest_checkout
                ON "order".pending_payment_card_hides (guest_cart_id, checkout_id)
                WHERE guest_cart_id IS NOT NULL;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""DROP TABLE IF EXISTS "order".pending_payment_card_hides;""");
    }
}
