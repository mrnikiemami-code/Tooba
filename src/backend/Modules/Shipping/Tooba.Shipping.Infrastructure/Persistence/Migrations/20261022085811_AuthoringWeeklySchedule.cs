using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthoringWeeklySchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authoring_weekly_schedules",
                schema: "shipping",
                columns: table => new
                {
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authoring_weekly_schedules", x => x.draft_id);
                    table.CheckConstraint("ck_authoring_weekly_schedules_timezone_length", "char_length(btrim(time_zone_id)) > 0 AND char_length(time_zone_id) <= 64");
                    table.ForeignKey(
                        name: "fk_authoring_weekly_schedules_draft",
                        column: x => x.draft_id,
                        principalSchema: "shipping",
                        principalTable: "authoring_drafts",
                        principalColumn: "draft_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "authoring_weekly_schedule_days",
                schema: "shipping",
                columns: table => new
                {
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weekday = table.Column<int>(type: "integer", nullable: false),
                    is_working = table.Column<bool>(type: "boolean", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authoring_weekly_schedule_days", x => new { x.draft_id, x.weekday });
                    table.CheckConstraint("ck_authoring_weekly_schedule_days_hours_paired", "(start_time IS NULL) = (end_time IS NULL)");
                    table.CheckConstraint("ck_authoring_weekly_schedule_days_range", "start_time IS NULL OR end_time > start_time");
                    table.CheckConstraint("ck_authoring_weekly_schedule_days_weekday_range", "weekday BETWEEN 1 AND 7");
                    table.CheckConstraint("ck_authoring_weekly_schedule_days_working_hours", "is_working = FALSE OR (start_time IS NOT NULL AND end_time IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_authoring_weekly_schedule_days_schedule",
                        column: x => x.draft_id,
                        principalSchema: "shipping",
                        principalTable: "authoring_weekly_schedules",
                        principalColumn: "draft_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authoring_weekly_schedule_days",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "authoring_weekly_schedules",
                schema: "shipping");
        }
    }
}
