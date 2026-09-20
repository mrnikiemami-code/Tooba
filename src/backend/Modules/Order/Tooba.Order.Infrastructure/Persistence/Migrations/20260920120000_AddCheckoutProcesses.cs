using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>TB-TMAR-CHECKOUT-IMPL-W1 — جدول پایدار فرآیند checkout (idempotency + milestones).</summary>
[DbContext(typeof(OrderDbContext))]
[Migration("20260920120000_AddCheckoutProcesses")]
public partial class AddCheckoutProcesses : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "order".checkout_processes
            (
                process_id uuid NOT NULL,
                submission_idempotency_key character varying(128) NOT NULL,
                cart_id uuid NOT NULL,
                checkout_id uuid NULL,
                status character varying(32) NOT NULL,
                correlation_id character varying(128) NOT NULL,
                started_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                failure_code character varying(128) NULL,
                CONSTRAINT pk_checkout_processes PRIMARY KEY (process_id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_checkout_processes_submission_key
                ON "order".checkout_processes (submission_idempotency_key);

            CREATE INDEX IF NOT EXISTS ix_checkout_processes_cart
                ON "order".checkout_processes (cart_id);

            CREATE INDEX IF NOT EXISTS ix_checkout_processes_checkout
                ON "order".checkout_processes (checkout_id);

            CREATE INDEX IF NOT EXISTS ix_checkout_processes_correlation
                ON "order".checkout_processes (correlation_id);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""DROP TABLE IF EXISTS "order".checkout_processes;""");
    }
}
