using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T004-R15 — سیاست چرخه رزرو فروشگاه و overrideهای Offer/Category.</summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("20260913033100_AddReservationCyclePolicy")]
public partial class AddReservationCyclePolicy : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE catalog.store_hold_policy_settings
                ADD COLUMN IF NOT EXISTS initial_reservation_hold_minutes integer NULL,
                ADD COLUMN IF NOT EXISTS retry_reservation_hold_minutes integer NULL,
                ADD COLUMN IF NOT EXISTS max_reservation_cycles integer NULL;

            CREATE TABLE IF NOT EXISTS catalog.reservation_cycle_policy_overrides
            (
                override_id uuid NOT NULL,
                scope_kind character varying(16) NOT NULL,
                scope_id uuid NOT NULL,
                initial_reservation_hold_minutes integer NULL,
                retry_reservation_hold_minutes integer NULL,
                max_reservation_cycles integer NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_reservation_cycle_policy_overrides PRIMARY KEY (override_id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_reservation_cycle_policy_scope
                ON catalog.reservation_cycle_policy_overrides (scope_kind, scope_id);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS catalog.reservation_cycle_policy_overrides;
            ALTER TABLE catalog.store_hold_policy_settings
                DROP COLUMN IF EXISTS initial_reservation_hold_minutes,
                DROP COLUMN IF EXISTS retry_reservation_hold_minutes,
                DROP COLUMN IF EXISTS max_reservation_cycles;
            """);
    }
}
