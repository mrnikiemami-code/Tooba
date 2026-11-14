using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-265-R1: durable Order-owned closure fact for the remaining customer delivery obligation of one order line when
/// an approved courier-origin Returns case covers that exact quantity.
/// </summary>
/// <remarks>
/// Additive only. The row is written BEFORE any refund or stock effect, so a retried approval replays the recorded
/// closure instead of recomputing (and potentially re-closing) the obligation. The unique
/// (order_line_id, return_request_id, source_claim_id) guard makes double closure of the same case line impossible,
/// and old orders stay readable without any backfill.
/// </remarks>
[DbContext(typeof(OrderDbContext))]
[Migration("20261114120000_AddOrderLineObligationClosures")]
public partial class AddOrderLineObligationClosures : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "order".order_line_obligation_closures
            (
                closure_id uuid NOT NULL,
                seller_order_id uuid NOT NULL,
                checkout_id uuid NOT NULL,
                order_line_id uuid NOT NULL,
                return_request_id uuid NOT NULL,
                source_claim_id uuid NOT NULL,
                quantity numeric(18,6) NOT NULL,
                actor_user_id uuid NOT NULL,
                closed_at_utc timestamp with time zone NOT NULL,
                CONSTRAINT pk_order_line_obligation_closures PRIMARY KEY (closure_id),
                CONSTRAINT ck_order_line_obligation_closures_quantity CHECK (quantity > 0),
                CONSTRAINT fk_order_line_obligation_closures_line FOREIGN KEY (order_line_id)
                    REFERENCES "order".order_lines (line_id) ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_order_line_obligation_closures_line_case_claim
                ON "order".order_line_obligation_closures (order_line_id, return_request_id, source_claim_id);

            CREATE INDEX IF NOT EXISTS ix_order_line_obligation_closures_line
                ON "order".order_line_obligation_closures (order_line_id);

            CREATE INDEX IF NOT EXISTS ix_order_line_obligation_closures_seller_order
                ON "order".order_line_obligation_closures (seller_order_id);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS "order".order_line_obligation_closures;
            """);
    }
}
