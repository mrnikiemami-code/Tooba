using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-264: durable operational deadlines of the accepted dispatch plan (latest preparation start, latest
/// transfer departure and the carrier acceptance deadline), together with the timing policy version that
/// produced them.
/// </summary>
/// <remarks>
/// <para>
/// Additive and nullable only. Historical policy-v1 plans keep their eight dispatch columns and
/// <c>NULL</c> deadlines, which every consumer reads as «بدون موعد آماده‌سازی» — never as an immediately
/// overdue preparation and never as a value recomputed from current settings.
/// </para>
/// <para>
/// The all-or-nothing dispatch constraint stays all-or-nothing: the new columns may be absent (policy v1,
/// eight non-null columns) or present as a complete triple (policy v2+, eleven non-null columns). A partial
/// triple is rejected, so a deadline can never be read beside half a plan. Ordering is enforced
/// <c>preparation &lt; transfer &lt;= handoff</c>; the absolute instants remain the domain's authority.
/// </para>
/// </remarks>
[DbContext(typeof(OrderDbContext))]
[Migration("20261111233000_AddPreparationAwareDispatchDeadlines")]
public partial class AddPreparationAwareDispatchDeadlines : Migration
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
            migrationBuilder.Sql(
                $"ALTER TABLE \"order\".{table} ADD COLUMN IF NOT EXISTS accepted_dispatch_preparation_start_utc timestamp with time zone NULL;");
            migrationBuilder.Sql(
                $"ALTER TABLE \"order\".{table} ADD COLUMN IF NOT EXISTS accepted_dispatch_transfer_departure_utc timestamp with time zone NULL;");
            migrationBuilder.Sql(
                $"ALTER TABLE \"order\".{table} ADD COLUMN IF NOT EXISTS accepted_dispatch_handoff_deadline_utc timestamp with time zone NULL;");
            migrationBuilder.Sql(
                $"ALTER TABLE \"order\".{table} ADD COLUMN IF NOT EXISTS accepted_dispatch_preparation_minutes integer NULL;");
            migrationBuilder.Sql(
                $"ALTER TABLE \"order\".{table} ADD COLUMN IF NOT EXISTS accepted_dispatch_handoff_minutes integer NULL;");

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

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in Tables)
        {
            migrationBuilder.Sql($"ALTER TABLE \"order\".{table} DROP CONSTRAINT IF EXISTS ck_{table}_accepted_dispatch_plan;");
            migrationBuilder.Sql(
                $"""
                ALTER TABLE "order".{table}
                ADD CONSTRAINT ck_{table}_accepted_dispatch_plan CHECK (
                    num_nonnulls({BaseColumns}) IN (0, 8)
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
            migrationBuilder.Sql(
                $"""
                ALTER TABLE "order".{table}
                DROP COLUMN IF EXISTS accepted_dispatch_preparation_start_utc,
                DROP COLUMN IF EXISTS accepted_dispatch_transfer_departure_utc,
                DROP COLUMN IF EXISTS accepted_dispatch_handoff_deadline_utc,
                DROP COLUMN IF EXISTS accepted_dispatch_preparation_minutes,
                DROP COLUMN IF EXISTS accepted_dispatch_handoff_minutes;
                """);
        }
    }
}
