using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// TB-241: the accepted shipping evidence keeps EXACTLY the same 22 scalar columns and the same
    /// constraint names; only the known capture format whitelist widens from <c>1</c> to <c>1..2</c> so a
    /// slot-configured capture (format 2) can be persisted and read. Version 1 (full-day window) stays
    /// first-class and no stored row, amount, owner or interval is rewritten.
    /// </summary>
    /// <remarks>
    /// The predicate below is the EXACT body already stored by migration 231/236 with one substitution in
    /// <c>accepted_format_version</c>; nothing else about ownership, money, ticks or delivery days moves.
    /// </remarks>
    [DbContext(typeof(OrderDbContext))]
    [Migration("20261028200100_AcceptSlotDurationFormat")]
    public partial class AcceptSlotDurationFormat : Migration
    {
        private static readonly string[] Tables = ["cart_shipping_groups", "order_shipping_groups"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"ALTER TABLE \"order\".{table} DROP CONSTRAINT IF EXISTS ck_{table}_accepted_evidence_shape;");
                migrationBuilder.Sql(Build(table, "accepted_format_version = ANY (ARRAY[1, 2])"));
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"ALTER TABLE \"order\".{table} DROP CONSTRAINT IF EXISTS ck_{table}_accepted_evidence_shape;");
                migrationBuilder.Sql(Build(table, "accepted_format_version = 1"));
            }
        }

        private static string Build(string table, string formatPredicate)
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
}
