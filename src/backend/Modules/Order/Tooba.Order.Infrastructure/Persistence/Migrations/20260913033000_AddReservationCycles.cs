using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T004-R15 — چرخه رزرو سطح سفارش و تاریخچهٔ تغییرناپذیر.</summary>
[DbContext(typeof(OrderDbContext))]
[Migration("20260913033000_AddReservationCycles")]
public partial class AddReservationCycles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "order".reservation_cycles
            (
                cycle_id uuid NOT NULL,
                checkout_id uuid NOT NULL,
                cycle_number integer NOT NULL,
                reason character varying(32) NOT NULL,
                status character varying(32) NOT NULL,
                started_at timestamp with time zone NOT NULL,
                expires_at timestamp with time zone NOT NULL,
                ended_at timestamp with time zone NULL,
                actor character varying(128) NULL,
                correlation_id character varying(128) NULL,
                payment_attempt_id uuid NULL,
                effective_hold_minutes integer NOT NULL,
                effective_max_cycles integer NOT NULL,
                policy_source character varying(64) NOT NULL,
                reservation_ids character varying(2048) NOT NULL,
                CONSTRAINT pk_reservation_cycles PRIMARY KEY (cycle_id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_reservation_cycles_checkout_number
                ON "order".reservation_cycles (checkout_id, cycle_number);

            CREATE UNIQUE INDEX IF NOT EXISTS ux_reservation_cycles_one_active
                ON "order".reservation_cycles (checkout_id)
                WHERE status = 'Active';

            CREATE INDEX IF NOT EXISTS ix_reservation_cycles_correlation
                ON "order".reservation_cycles (correlation_id);

            CREATE TABLE IF NOT EXISTS "order".reservation_cycle_events
            (
                event_id uuid NOT NULL,
                checkout_id uuid NOT NULL,
                cycle_id uuid NULL,
                kind character varying(48) NOT NULL,
                occurred_at timestamp with time zone NOT NULL,
                detail character varying(256) NOT NULL,
                CONSTRAINT pk_reservation_cycle_events PRIMARY KEY (event_id)
            );

            CREATE INDEX IF NOT EXISTS ix_reservation_cycle_events_checkout_at
                ON "order".reservation_cycle_events (checkout_id, occurred_at);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS "order".reservation_cycle_events;
            DROP TABLE IF EXISTS "order".reservation_cycles;
            """);
    }
}
