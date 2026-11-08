using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-253-R4: durable immutable Marketplace operational-group occurrence envelope and its exact
/// per-member constituent allocation links.
/// </summary>
/// <remarks>
/// Additive only. The occurrence row is claimed before the first Fulfillment allocation, so the exact
/// reviewed request survives response loss and a changed request under the same occurrence can be
/// refused as a typed intent conflict instead of silently re-allocating.
/// </remarks>
[DbContext(typeof(OrderDbContext))]
[Migration("20261108153000_AddOperationalGroupOccurrences")]
public partial class AddOperationalGroupOccurrences : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "order".order_operational_group_occurrences
            (
                occurrence_id uuid NOT NULL,
                checkout_id uuid NOT NULL,
                actor_user_id uuid NOT NULL,
                operational_group_key character varying(1024) NOT NULL,
                envelope_fingerprint character varying(64) NOT NULL,
                envelope_json text NOT NULL,
                state character varying(32) NOT NULL,
                consolidated_package_id uuid NULL,
                package_number character varying(64) NULL,
                shipping_method_code character varying(128) NULL,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL,
                CONSTRAINT pk_order_operational_group_occurrences PRIMARY KEY (occurrence_id),
                CONSTRAINT fk_order_operational_group_occurrences_checkout FOREIGN KEY (checkout_id)
                    REFERENCES "order".checkouts (checkout_id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS ix_order_operational_group_occurrences_checkout
                ON "order".order_operational_group_occurrences (checkout_id);

            CREATE TABLE IF NOT EXISTS "order".order_operational_group_occurrence_members
            (
                occurrence_id uuid NOT NULL,
                shipping_group_id uuid NOT NULL,
                shipment_ids_json text NOT NULL,
                reused_shipment_ids_json text NOT NULL,
                created boolean NOT NULL,
                recorded_at_utc timestamp with time zone NOT NULL,
                CONSTRAINT pk_order_operational_group_occurrence_members
                    PRIMARY KEY (occurrence_id, shipping_group_id),
                CONSTRAINT fk_order_operational_group_occurrence_members_occurrence
                    FOREIGN KEY (occurrence_id)
                    REFERENCES "order".order_operational_group_occurrences (occurrence_id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS ix_order_operational_group_occurrence_members_occurrence
                ON "order".order_operational_group_occurrence_members (occurrence_id);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS "order".order_operational_group_occurrence_members;
            DROP TABLE IF EXISTS "order".order_operational_group_occurrences;
            """);
    }
}
