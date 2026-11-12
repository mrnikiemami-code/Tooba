using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-264-R1 (gap A): the durable dispatch-plan constraint must accept the EXPLICIT zero allowances the
/// authority and the domain already accept, and must accept the format version that carries the processing
/// week.
/// </summary>
/// <remarks>
/// <para>
/// The previous constraint required <c>preparation_start &lt; transfer_departure</c> and pinned
/// <c>accepted_format_version = 3</c>. Both were true for the TB-264 policy, but a method with
/// <c>PreparationMinutes = 0</c> (equality is the correct arithmetic, not a fake one-second duration) or a
/// revision that carries the warehouse processing week (format version 4) was refused by storage even
/// though the plan was lawful.
/// </para>
/// <para>
/// This is a NEW additive migration; the applied TB-264 migration stays immutable. Non-decreasing ordering
/// is still enforced, the deadline triple is still all-or-nothing, the carrier window stays half-open
/// (<c>open &lt; close</c>), and equality of conceptual boundaries never authorizes arrival at the carrier's
/// close instant — that rule is the scheduling authority's, not a storage default.
/// </para>
/// </remarks>
[DbContext(typeof(OrderDbContext))]
[Migration("20261112130000_RelaxPreparationDispatchZeroAllowances")]
public partial class RelaxPreparationDispatchZeroAllowances : Migration
{
    private static readonly string[] Tables = ["cart_shipping_groups", "order_shipping_groups"];

    private const string BaseColumns =
        """
        accepted_dispatch_handoff_date,
        accepted_dispatch_open_ticks,
        accepted_dispatch_close_ticks,
        accepted_dispatch_time_zone,
        accepted_dispatch_transport_days,
        accepted_dispatch_policy_version,
        accepted_dispatch_snapshot_id,
        accepted_dispatch_source_revision
        """;

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var table in Tables)
        {
            // Gap C: a revision that carries the warehouse processing week is format version 4, so the
            // accepted-evidence shape constraint must accept it. Nothing else about ownership, money,
            // ticks, delivery days or the receiving window moves.
            migrationBuilder.Sql($"ALTER TABLE \"order\".{table} DROP CONSTRAINT IF EXISTS ck_{table}_accepted_evidence_shape;");
            migrationBuilder.Sql(BuildEvidenceShape(table, "accepted_format_version = ANY (ARRAY[1, 2, 3, 4])"));

            migrationBuilder.Sql($"ALTER TABLE \"order\".{table} DROP CONSTRAINT IF EXISTS ck_{table}_accepted_dispatch_plan;");
            migrationBuilder.Sql(
                $"""
                ALTER TABLE "order".{table}
                ADD CONSTRAINT ck_{table}_accepted_dispatch_plan CHECK (
                    num_nonnulls(
                        {BaseColumns},
                        accepted_dispatch_preparation_start_utc,
                        accepted_dispatch_transfer_departure_utc,
                        accepted_dispatch_handoff_deadline_utc,
                        accepted_dispatch_preparation_minutes,
                        accepted_dispatch_handoff_minutes) IN (0, 8, 13)
                    AND (
                        accepted_dispatch_handoff_date IS NULL
                        OR (
                            accepted_snapshot_id IS NOT NULL
                            AND accepted_format_version IN (3, 4)
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
                    AND (
                        accepted_dispatch_preparation_start_utc IS NULL
                        OR (
                            accepted_dispatch_policy_version >= 2
                            AND accepted_dispatch_preparation_start_utc <= accepted_dispatch_transfer_departure_utc
                            AND accepted_dispatch_transfer_departure_utc <= accepted_dispatch_handoff_deadline_utc
                            AND accepted_dispatch_preparation_minutes BETWEEN 0 AND 10080
                            AND accepted_dispatch_handoff_minutes BETWEEN 0 AND 1440
                            -- A zero allowance collapses exactly the boundary it measures; a positive one
                            -- keeps the two instants strictly ordered.
                            AND (
                                accepted_dispatch_preparation_minutes > 0
                                OR accepted_dispatch_preparation_start_utc = accepted_dispatch_transfer_departure_utc
                            )
                            AND (
                                accepted_dispatch_handoff_minutes > 0
                                OR accepted_dispatch_transfer_departure_utc = accepted_dispatch_handoff_deadline_utc
                            )
                        )
                    )
                );
                """);
        }
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in Tables)
        {
            migrationBuilder.Sql($"ALTER TABLE \"order\".{table} DROP CONSTRAINT IF EXISTS ck_{table}_accepted_evidence_shape;");
            migrationBuilder.Sql(BuildEvidenceShape(table, "accepted_format_version = ANY (ARRAY[1, 2, 3])"));

            migrationBuilder.Sql($"ALTER TABLE \"order\".{table} DROP CONSTRAINT IF EXISTS ck_{table}_accepted_dispatch_plan;");
            migrationBuilder.Sql(
                $"""
                ALTER TABLE "order".{table}
                ADD CONSTRAINT ck_{table}_accepted_dispatch_plan CHECK (
                    num_nonnulls(
                        {BaseColumns},
                        accepted_dispatch_preparation_start_utc,
                        accepted_dispatch_transfer_departure_utc,
                        accepted_dispatch_handoff_deadline_utc,
                        accepted_dispatch_preparation_minutes,
                        accepted_dispatch_handoff_minutes) IN (0, 8, 13)
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
                    AND (
                        accepted_dispatch_preparation_start_utc IS NULL
                        OR (
                            accepted_dispatch_policy_version >= 2
                            AND accepted_dispatch_preparation_start_utc < accepted_dispatch_transfer_departure_utc
                            AND accepted_dispatch_transfer_departure_utc <= accepted_dispatch_handoff_deadline_utc
                            AND accepted_dispatch_preparation_minutes BETWEEN 0 AND 10080
                            AND accepted_dispatch_handoff_minutes BETWEEN 0 AND 1440
                        )
                    )
                );
                """);
        }
    }

    /// <summary>
    /// The predicate stored by TB-253 with one substitution in <c>accepted_format_version</c>; nothing else
    /// about ownership, money, ticks, delivery days or ownership shape moves.
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
