using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Shipping.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-264-R1: the durable pieces the physical readiness policy needs on the authoring side — the explicit
/// preparation/transfer minute pair that TB-264 introduced, the explicit processing meaning, and the
/// independent <b>warehouse processing week</b>.
/// </summary>
/// <remarks>
/// <para>
/// Additive only. Absent columns/rows are real states: a draft with no timing pair is «تنظیم‌نشده» (never
/// instant-ready), a draft with no processing week is «پیکربندی‌نشده» (no hours are invented), and a
/// seven-closed week is seven real rows. No existing draft is backfilled, no customer/carrier hour is
/// copied into the processing week, and no historical revision becomes invalid.
/// </para>
/// <para>
/// The authoring revision payload whitelist widens from <c>1..3</c> to <c>1..4</c> so a revision that
/// carries the processing week can be persisted; versions 1–3 keep decoding unchanged.
/// </para>
/// </remarks>
[DbContext(typeof(ShippingDbContext))]
[Migration("20261112120000_AddProcessingHoursAndTimingPolicy")]
public partial class AddProcessingHoursAndTimingPolicy : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE shipping.authoring_method_settings
                ADD COLUMN IF NOT EXISTS preparation_minutes integer NULL,
                ADD COLUMN IF NOT EXISTS handoff_minutes integer NULL,
                ADD COLUMN IF NOT EXISTS preparation_mode integer NOT NULL DEFAULT 0;

            ALTER TABLE shipping.authoring_method_settings
                ALTER COLUMN preparation_mode DROP DEFAULT;

            ALTER TABLE shipping.authoring_method_settings
                DROP CONSTRAINT IF EXISTS ck_authoring_method_settings_preparation_pair;
            ALTER TABLE shipping.authoring_method_settings
                ADD CONSTRAINT ck_authoring_method_settings_preparation_pair
                CHECK ((preparation_minutes IS NULL) = (handoff_minutes IS NULL));

            ALTER TABLE shipping.authoring_method_settings
                DROP CONSTRAINT IF EXISTS ck_authoring_method_settings_preparation_minutes_range;
            ALTER TABLE shipping.authoring_method_settings
                ADD CONSTRAINT ck_authoring_method_settings_preparation_minutes_range
                CHECK (preparation_minutes IS NULL OR preparation_minutes BETWEEN 0 AND 10080);

            ALTER TABLE shipping.authoring_method_settings
                DROP CONSTRAINT IF EXISTS ck_authoring_method_settings_handoff_minutes_range;
            ALTER TABLE shipping.authoring_method_settings
                ADD CONSTRAINT ck_authoring_method_settings_handoff_minutes_range
                CHECK (handoff_minutes IS NULL OR handoff_minutes BETWEEN 0 AND 1440);

            ALTER TABLE shipping.authoring_method_settings
                DROP CONSTRAINT IF EXISTS ck_authoring_method_settings_preparation_mode;
            ALTER TABLE shipping.authoring_method_settings
                ADD CONSTRAINT ck_authoring_method_settings_preparation_mode
                CHECK (preparation_mode IN (0, 1, 2));

            ALTER TABLE shipping.authoring_method_settings
                DROP CONSTRAINT IF EXISTS ck_authoring_method_settings_preparation_mode_paired;
            ALTER TABLE shipping.authoring_method_settings
                ADD CONSTRAINT ck_authoring_method_settings_preparation_mode_paired
                CHECK ((preparation_minutes IS NULL) = (preparation_mode = 0));
            """);

        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS shipping.authoring_processing_schedules (
                draft_id uuid NOT NULL,
                time_zone_id character varying(64) NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_authoring_processing_schedules PRIMARY KEY (draft_id),
                CONSTRAINT fk_authoring_processing_schedules_draft FOREIGN KEY (draft_id)
                    REFERENCES shipping.authoring_drafts (draft_id) ON DELETE CASCADE,
                CONSTRAINT ck_authoring_processing_schedules_timezone_length
                    CHECK (char_length(btrim(time_zone_id)) > 0 AND char_length(time_zone_id) <= 64)
            );

            CREATE TABLE IF NOT EXISTS shipping.authoring_processing_schedule_days (
                draft_id uuid NOT NULL,
                weekday integer NOT NULL,
                is_working boolean NOT NULL,
                opens_at time without time zone,
                closes_at time without time zone,
                CONSTRAINT pk_authoring_processing_schedule_days PRIMARY KEY (draft_id, weekday),
                CONSTRAINT fk_authoring_processing_schedule_days_schedule FOREIGN KEY (draft_id)
                    REFERENCES shipping.authoring_processing_schedules (draft_id) ON DELETE CASCADE,
                CONSTRAINT ck_authoring_processing_schedule_days_weekday_range
                    CHECK (weekday BETWEEN 1 AND 7),
                CONSTRAINT ck_authoring_processing_schedule_days_hours_paired
                    CHECK ((opens_at IS NULL) = (closes_at IS NULL)),
                CONSTRAINT ck_authoring_processing_schedule_days_range
                    CHECK (opens_at IS NULL OR closes_at > opens_at),
                CONSTRAINT ck_authoring_processing_schedule_days_working_hours
                    CHECK (is_working = FALSE OR (opens_at IS NOT NULL AND closes_at IS NOT NULL))
            );
            """);

        migrationBuilder.Sql(
            """
            ALTER TABLE shipping.authoring_revision_snapshots
            DROP CONSTRAINT IF EXISTS ck_authoring_revision_snapshots_format_version_known;
            ALTER TABLE shipping.authoring_revision_snapshots
            ADD CONSTRAINT ck_authoring_revision_snapshots_format_version_known
            CHECK (format_version IN (1, 2, 3, 4));
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
            CHECK (format_version IN (1, 2, 3));
            """);

        migrationBuilder.Sql("DROP TABLE IF EXISTS shipping.authoring_processing_schedule_days;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS shipping.authoring_processing_schedules;");

        migrationBuilder.Sql(
            """
            ALTER TABLE shipping.authoring_method_settings
                DROP CONSTRAINT IF EXISTS ck_authoring_method_settings_preparation_mode_paired,
                DROP CONSTRAINT IF EXISTS ck_authoring_method_settings_preparation_mode,
                DROP CONSTRAINT IF EXISTS ck_authoring_method_settings_handoff_minutes_range,
                DROP CONSTRAINT IF EXISTS ck_authoring_method_settings_preparation_minutes_range,
                DROP CONSTRAINT IF EXISTS ck_authoring_method_settings_preparation_pair,
                DROP COLUMN IF EXISTS preparation_mode,
                DROP COLUMN IF EXISTS preparation_minutes,
                DROP COLUMN IF EXISTS handoff_minutes;
            """);
    }
}
