using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T004-R23 — قفل مشتری، رویداد سهمیه Cycle #1 و ممیزی مسدود.</summary>
[DbContext(typeof(OrderDbContext))]
[Migration("20260913140000_AddCheckoutAbuseLimits")]
public partial class AddCheckoutAbuseLimits : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "order".checkout_abuse_customer_locks
            (
                customer_id uuid NOT NULL,
                touched_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_checkout_abuse_customer_locks PRIMARY KEY (customer_id)
            );

            CREATE TABLE IF NOT EXISTS "order".checkout_reservation_commits
            (
                event_id uuid NOT NULL,
                store_id uuid NOT NULL,
                customer_id uuid NOT NULL,
                order_id uuid NOT NULL,
                checkout_id uuid NOT NULL,
                reservation_cycle_number integer NOT NULL,
                occurred_at timestamp with time zone NOT NULL,
                source character varying(64) NOT NULL,
                CONSTRAINT pk_checkout_reservation_commits PRIMARY KEY (event_id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_checkout_reservation_commits_checkout
                ON "order".checkout_reservation_commits (checkout_id);

            CREATE INDEX IF NOT EXISTS ix_checkout_reservation_commits_customer_occurred
                ON "order".checkout_reservation_commits (customer_id, occurred_at);

            CREATE TABLE IF NOT EXISTS "order".checkout_abuse_block_events
            (
                event_id uuid NOT NULL,
                store_id uuid NOT NULL,
                customer_id uuid NOT NULL,
                kind character varying(32) NOT NULL,
                current_count integer NOT NULL,
                max_count integer NOT NULL,
                occurred_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_checkout_abuse_block_events PRIMARY KEY (event_id)
            );

            CREATE INDEX IF NOT EXISTS ix_checkout_abuse_block_events_customer_occurred
                ON "order".checkout_abuse_block_events (customer_id, occurred_at);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS "order".checkout_abuse_block_events;
            DROP TABLE IF EXISTS "order".checkout_reservation_commits;
            DROP TABLE IF EXISTS "order".checkout_abuse_customer_locks;
            """);
    }
}
