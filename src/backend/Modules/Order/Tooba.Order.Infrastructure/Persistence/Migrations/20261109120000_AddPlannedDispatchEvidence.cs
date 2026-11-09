using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-256: durable <b>planned dispatch</b> (carrier handoff) on accepted shipping group evidence.
/// </summary>
/// <remarks>
/// <para>
/// Additive and nullable only: historical groups keep <c>NULL</c> dispatch columns, which the domain reads
/// as "unspecified" (never an overdue guess and never a recomputation from current settings).
/// </para>
/// <para>
/// The capture format whitelist widens from <c>1..2</c> to <c>1..3</c> so a carrier-acceptance capture can be
/// stored and read; versions 1 and 2 stay first-class and no stored row, amount, owner, date or tick is
/// rewritten.
/// </para>
/// </remarks>
[DbContext(typeof(OrderDbContext))]
[Migration("20261109120000_AddPlannedDispatchEvidence")]
public partial class AddPlannedDispatchEvidence : Migration
{
    private static readonly string[] Tables = ["cart_shipping_groups", "order_shipping_groups"];

    private static readonly string[] Columns =
    [
        "accepted_dispatch_handoff_date date NULL",
        "accepted_dispatch_open_ticks bigint NULL",
        "accepted_dispatch_close_ticks bigint NULL",
        "accepted_dispatch_time_zone character varying(64) NULL",
        "accepted_dispatch_transport_days integer NULL",
        "accepted_dispatch_policy_version integer NULL",
        "accepted_dispatch_snapshot_id uuid NULL",
        "accepted_dispatch_source_revision bigint NULL",
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var table in Tables)
        {
            foreach (var column in Columns)
            {
                migrationBuilder.Sql($"ALTER TABLE \"order\".{table} ADD COLUMN IF NOT EXISTS {column};");
            }

            // All-or-nothing: a partially written plan is not a plan, it can only exist beside accepted
            // evidence under the carrier-acceptance format, and a handoff may never be AFTER the accepted
            // customer receiving date.
            migrationBuilder.Sql($"ALTER TABLE \"order\".{table} DROP CONSTRAINT IF EXISTS ck_{table}_accepted_dispatch_plan;");
            migrationBuilder.Sql(
                $"""
                ALTER TABLE "order".{table}
                ADD CONSTRAINT ck_{table}_accepted_dispatch_plan CHECK (
                    num_nonnulls(
                        accepted_dispatch_handoff_date,
                        accepted_dispatch_open_ticks,
                        accepted_dispatch_close_ticks,
                        accepted_dispatch_time_zone,
                        accepted_dispatch_transport_days,
                        accepted_dispatch_policy_version,
                        accepted_dispatch_snapshot_id,
                        accepted_dispatch_source_revision) IN (0, 8)
                    AND (
                        accepted_dispatch_handoff_date IS NULL
                        OR (
                            accepted_snapshot_id IS NOT NULL
                            AND accepted_format_version = 3
                            AND accepted_dispatch_handoff_date <= accepted_receiving_date
                            AND accepted_dispatch_open_ticks >= 0
                            AND accepted_dispatch_open_ticks < accepted_dispatch_close_ticks
                            AND accepted_dispatch_close_ticks <= 863999999999
                            AND char_length(btrim(accepted_dispatch_time_zone)) > 0
                            AND accepted_dispatch_transport_days >= 0
                            AND accepted_dispatch_transport_days <= 365
                            AND accepted_dispatch_policy_version >= 1
                            AND accepted_dispatch_snapshot_id <> '00000000-0000-0000-0000-000000000000'::uuid
                            AND accepted_dispatch_source_revision > 0
                        )
                    )
                );
                """);

            migrationBuilder.Sql($"ALTER TABLE \"order\".{table} DROP CONSTRAINT IF EXISTS ck_{table}_accepted_evidence_shape;");
            migrationBuilder.Sql(BuildEvidenceShape(table, "accepted_format_version = ANY (ARRAY[1, 2, 3])"));
        }
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in Tables)
        {
            migrationBuilder.Sql($"ALTER TABLE \"order\".{table} DROP CONSTRAINT IF EXISTS ck_{table}_accepted_dispatch_plan;");
            migrationBuilder.Sql($"ALTER TABLE \"order\".{table} DROP CONSTRAINT IF EXISTS ck_{table}_accepted_evidence_shape;");
            migrationBuilder.Sql(BuildEvidenceShape(table, "accepted_format_version = ANY (ARRAY[1, 2])"));
            migrationBuilder.Sql(
                $"""
                ALTER TABLE "order".{table}
                DROP COLUMN IF EXISTS accepted_dispatch_handoff_date,
                DROP COLUMN IF EXISTS accepted_dispatch_open_ticks,
                DROP COLUMN IF EXISTS accepted_dispatch_close_ticks,
                DROP COLUMN IF EXISTS accepted_dispatch_time_zone,
                DROP COLUMN IF EXISTS accepted_dispatch_transport_days,
                DROP COLUMN IF EXISTS accepted_dispatch_policy_version,
                DROP COLUMN IF EXISTS accepted_dispatch_snapshot_id,
                DROP COLUMN IF EXISTS accepted_dispatch_source_revision;
                """);
        }
    }

    /// <summary>
    /// The exact predicate stored by 231/236/241 with one substitution in <c>accepted_format_version</c>;
    /// nothing else about ownership, money, ticks, delivery days or dispatch moves.
    /// </summary>
    private static string BuildEvidenceShape(string table, string formatPredicate)
    {
        var template =
            """
            ALTER TABLE "order".__TABLE__ ADD CONSTRAINT ck___TABLE___accepted_evidence_shape
            CHECK (
                accepted_snapshot_id IS NULL
                OR (
                    accepted_snapshot_id <> '00000000-0000-0000-0000-000000000000'::uuid
                    AND accepted_draft_id <> '00000000-0000-0000-0000-000000000000'::uuid
                    AND accepted_area_group_id <> '00000000-0000-0000-0000-000000000000'::uuid
                    AND accepted_rate_row_id <> '00000000-0000-0000-0000-000000000000'::uuid
                    AND (accepted_shipping_type_id IS NULL
                         OR accepted_shipping_type_id <> '00000000-0000-0000-0000-000000000000'::uuid)
                    AND accepted_source_revision > 0
                    AND __FORMAT__
                    AND accepted_selection_version > 0
                    AND accepted_owner_kind = ANY (ARRAY[0, 1, 2])
                    AND accepted_currency ~ '^[A-Z]{3}$'
                    AND accepted_freight_amount >= 0
                    AND accepted_packaging_amount >= 0
                    AND accepted_payable_at_checkout_amount >= 0
                    AND accepted_carrier_collect_amount >= 0
                    AND accepted_freight_amount + accepted_packaging_amount
                        = accepted_payable_at_checkout_amount + accepted_carrier_collect_amount
                    AND accepted_receiving_open_ticks >= 0
                    AND accepted_receiving_open_ticks < accepted_receiving_close_ticks
                    AND accepted_receiving_close_ticks <= 863999999999
                    AND char_length(accepted_receiving_time_zone) > 0
                    AND (
                        (accepted_minimum_delivery_days IS NULL AND accepted_maximum_delivery_days IS NULL)
                        OR (
                            accepted_minimum_delivery_days IS NOT NULL
                            AND accepted_maximum_delivery_days IS NOT NULL
                            AND accepted_minimum_delivery_days >= 0
                            AND accepted_maximum_delivery_days >= accepted_minimum_delivery_days
                            AND accepted_maximum_delivery_days <= 365
                        )
                    )
                    AND (
                        (
                            accepted_owner_kind = 2
                            AND accepted_owner_seller_party_id IS NOT NULL
                            AND accepted_owner_seller_party_id <> '00000000-0000-0000-0000-000000000000'::uuid
                            AND accepted_owner_tenant_key IS NULL
                        )
                        OR (
                            accepted_owner_kind = 0
                            AND accepted_owner_tenant_key IS NOT NULL
                            AND btrim(accepted_owner_tenant_key) <> ''
                            AND accepted_owner_tenant_key = btrim(accepted_owner_tenant_key)
                            AND accepted_owner_seller_party_id IS NULL
                        )
                        OR (
                            accepted_owner_kind = 1
                            AND accepted_owner_tenant_key IS NULL
                            AND accepted_owner_seller_party_id IS NULL
                        )
                    )
                )
            );
            """;

        return template
            .Replace("__TABLE__", table)
            .Replace("__FORMAT__", formatPredicate);
    }
}
