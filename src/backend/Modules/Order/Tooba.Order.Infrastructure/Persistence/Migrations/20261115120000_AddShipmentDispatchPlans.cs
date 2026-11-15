using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-270-R2: durable Order-owned confirmed dispatch plan for ONE existing physical parcel (returned-source resend
/// or healthy-stock replacement).
/// </summary>
/// <remarks>
/// Additive only. The plan is written by the plan command, never by a parcel/hold/decision creation, and it stores
/// the exact captured binding plus the readiness/selection facts the existing scheduling authority consumed. The
/// unique index on <c>shipment_id</c> is the database half of "one immutable plan per physical parcel"; old rows
/// stay readable with no backfill and no massaging of original accepted evidence.
/// </remarks>
[DbContext(typeof(OrderDbContext))]
[Migration("20261115120000_AddShipmentDispatchPlans")]
public partial class AddShipmentDispatchPlans : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "order".shipment_dispatch_plans
            (
                plan_id uuid NOT NULL,
                shipment_id uuid NOT NULL,
                checkout_id uuid NOT NULL,
                seller_order_id uuid NOT NULL,
                fulfillment_id uuid NOT NULL,
                shipping_group_id uuid NOT NULL,
                attempt_kind text NOT NULL,
                source_shipment_id uuid NULL,
                damaged_resolution_id uuid NULL,
                replacement_reservation_id uuid NULL,
                source_fingerprint text NOT NULL,
                owner_kind integer NOT NULL,
                owner_tenant_key text NULL,
                owner_seller_party_id uuid NULL,
                method_code text NOT NULL,
                method_label text NOT NULL,
                service_code text NOT NULL,
                option_code text NULL,
                snapshot_id uuid NOT NULL,
                draft_id uuid NOT NULL,
                source_revision bigint NOT NULL,
                format_version integer NOT NULL,
                selection_version bigint NOT NULL,
                area_group_id uuid NOT NULL,
                rate_row_id uuid NOT NULL,
                customer_receiving_date date NOT NULL,
                customer_receiving_open_ticks bigint NOT NULL,
                customer_receiving_close_ticks bigint NOT NULL,
                customer_receiving_time_zone text NOT NULL,
                handoff_date date NOT NULL,
                handoff_open_ticks bigint NOT NULL,
                handoff_close_ticks bigint NOT NULL,
                handoff_time_zone text NOT NULL,
                preparation_start_utc timestamp with time zone NOT NULL,
                transfer_departure_utc timestamp with time zone NOT NULL,
                handoff_deadline_utc timestamp with time zone NOT NULL,
                preparation_minutes integer NOT NULL,
                handoff_minutes integer NOT NULL,
                transport_working_days integer NOT NULL,
                policy_version integer NOT NULL,
                readiness_source text NOT NULL,
                readiness_at_utc timestamp with time zone NOT NULL,
                occurrence_key text NOT NULL,
                confirmed_by_user_id uuid NOT NULL,
                confirmed_at_utc timestamp with time zone NOT NULL,
                CONSTRAINT pk_shipment_dispatch_plans PRIMARY KEY (plan_id),
                CONSTRAINT ck_shipment_dispatch_plans_attempt_kind
                    CHECK (attempt_kind IN ('resend', 'replacement')),
                CONSTRAINT ck_shipment_dispatch_plans_source
                    CHECK ((attempt_kind = 'resend' AND damaged_resolution_id IS NULL)
                        OR (attempt_kind = 'replacement' AND damaged_resolution_id IS NOT NULL)),
                CONSTRAINT ck_shipment_dispatch_plans_owner_kind CHECK (owner_kind BETWEEN 0 AND 2),
                CONSTRAINT ck_shipment_dispatch_plans_ticks
                    CHECK (customer_receiving_open_ticks >= 0 AND customer_receiving_close_ticks > customer_receiving_open_ticks
                        AND handoff_open_ticks >= 0 AND handoff_close_ticks > handoff_open_ticks),
                CONSTRAINT ck_shipment_dispatch_plans_handoff_not_after_receiving
                    CHECK (handoff_date <= customer_receiving_date),
                CONSTRAINT ck_shipment_dispatch_plans_minutes
                    CHECK (preparation_minutes >= 0 AND handoff_minutes >= 0 AND transport_working_days >= 0),
                CONSTRAINT ck_shipment_dispatch_plans_policy CHECK (policy_version >= 1),
                CONSTRAINT ck_shipment_dispatch_plans_deadline_order
                    CHECK (preparation_start_utc <= transfer_departure_utc
                        AND transfer_departure_utc <= handoff_deadline_utc)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_shipment_dispatch_plans_shipment
                ON "order".shipment_dispatch_plans (shipment_id);

            CREATE INDEX IF NOT EXISTS ix_shipment_dispatch_plans_seller_group
                ON "order".shipment_dispatch_plans (seller_order_id, shipping_group_id);

            CREATE INDEX IF NOT EXISTS ix_shipment_dispatch_plans_checkout
                ON "order".shipment_dispatch_plans (checkout_id);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS "order".shipment_dispatch_plans;
            """);
    }
}
