using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// TB-241: ONE additive column on the EXISTING weekly schedule (method-level receiving slot duration in
    /// minutes) plus the two shape constraints that already guard this schema — the optional duration is a real
    /// whole number of minutes (1..1440) and a captured revision body may only use the known format versions
    /// 1 (no duration = one full-day window) and 2 (configured hourly slots).
    /// </summary>
    /// <remarks>
    /// No backfill: every existing schedule stays <c>NULL</c>, which keeps the legacy full-day window and is
    /// never inferred as 60 minutes. No history row is rewritten and no business value is invented.
    /// </remarks>
    public partial class ReceivingSlotDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_authoring_revision_snapshots_format_version_known",
                schema: "shipping",
                table: "authoring_revision_snapshots");

            migrationBuilder.AddColumn<int>(
                name: "slot_duration_minutes",
                schema: "shipping",
                table: "authoring_weekly_schedules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_authoring_weekly_schedules_slot_duration",
                schema: "shipping",
                table: "authoring_weekly_schedules",
                sql: "slot_duration_minutes IS NULL OR (slot_duration_minutes >= 1 AND slot_duration_minutes <= 1440)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_authoring_revision_snapshots_format_version_known",
                schema: "shipping",
                table: "authoring_revision_snapshots",
                sql: "format_version IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_authoring_weekly_schedules_slot_duration",
                schema: "shipping",
                table: "authoring_weekly_schedules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_authoring_revision_snapshots_format_version_known",
                schema: "shipping",
                table: "authoring_revision_snapshots");

            migrationBuilder.DropColumn(
                name: "slot_duration_minutes",
                schema: "shipping",
                table: "authoring_weekly_schedules");

            migrationBuilder.AddCheckConstraint(
                name: "ck_authoring_revision_snapshots_format_version_known",
                schema: "shipping",
                table: "authoring_revision_snapshots",
                sql: "format_version = 1");
        }
    }
}
