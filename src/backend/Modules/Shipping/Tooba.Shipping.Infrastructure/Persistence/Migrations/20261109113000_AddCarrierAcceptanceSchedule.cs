using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Shipping.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-256: independent <b>carrier acceptance</b> weekly calendar per shipping method draft.
/// </summary>
/// <remarks>
/// <para>
/// Additive only. The header row is absent for existing drafts, which is the explicit
/// "acceptance unconfigured" state; no customer hour is copied and no default interval is invented.
/// </para>
/// <para>
/// The authoring revision payload whitelist widens from <c>1..2</c> to <c>1..3</c> so a carrier-configured
/// revision can be persisted; versions 1 and 2 keep decoding unchanged.
/// </para>
/// </remarks>
[DbContext(typeof(ShippingDbContext))]
[Migration("20261109113000_AddCarrierAcceptanceSchedule")]
public partial class AddCarrierAcceptanceSchedule : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS shipping.authoring_carrier_schedules (
                draft_id uuid NOT NULL,
                time_zone_id character varying(64) NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_authoring_carrier_schedules PRIMARY KEY (draft_id),
                CONSTRAINT fk_authoring_carrier_schedules_draft FOREIGN KEY (draft_id)
                    REFERENCES shipping.authoring_drafts (draft_id) ON DELETE CASCADE,
                CONSTRAINT ck_authoring_carrier_schedules_timezone_length
                    CHECK (char_length(btrim(time_zone_id)) > 0 AND char_length(time_zone_id) <= 64)
            );

            CREATE TABLE IF NOT EXISTS shipping.authoring_carrier_schedule_days (
                draft_id uuid NOT NULL,
                weekday integer NOT NULL,
                is_working boolean NOT NULL,
                opens_at time without time zone,
                closes_at time without time zone,
                CONSTRAINT pk_authoring_carrier_schedule_days PRIMARY KEY (draft_id, weekday),
                CONSTRAINT fk_authoring_carrier_schedule_days_schedule FOREIGN KEY (draft_id)
                    REFERENCES shipping.authoring_carrier_schedules (draft_id) ON DELETE CASCADE,
                CONSTRAINT ck_authoring_carrier_schedule_days_weekday_range
                    CHECK (weekday BETWEEN 1 AND 7),
                CONSTRAINT ck_authoring_carrier_schedule_days_hours_paired
                    CHECK ((opens_at IS NULL) = (closes_at IS NULL)),
                CONSTRAINT ck_authoring_carrier_schedule_days_range
                    CHECK (opens_at IS NULL OR closes_at > opens_at),
                CONSTRAINT ck_authoring_carrier_schedule_days_working_hours
                    CHECK (is_working = FALSE OR (opens_at IS NOT NULL AND closes_at IS NOT NULL))
            );
            """);

        migrationBuilder.Sql(
            """
            ALTER TABLE shipping.authoring_revision_snapshots
            DROP CONSTRAINT IF EXISTS ck_authoring_revision_snapshots_format_version_known;
            ALTER TABLE shipping.authoring_revision_snapshots
            ADD CONSTRAINT ck_authoring_revision_snapshots_format_version_known
            CHECK (format_version IN (1, 2, 3));
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE shipping.authoring_revision_snapshots
            DROP CONSTRAINT IF EXISTS ck_authoring_revision_snapshots_format_version_known;
            ALTER TABLE shipping.authoring_revision_snapshots
            ADD CONSTRAINT ck_authoring_revision_snapshots_format_version_known
            CHECK (format_version IN (1, 2));
            """);

        migrationBuilder.Sql("DROP TABLE IF EXISTS shipping.authoring_carrier_schedule_days;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS shipping.authoring_carrier_schedules;");
    }
}
