using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OperatingSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "operating_schedules",
                schema: "shipping",
                columns: table => new
                {
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    option_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    time_zone_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operating_schedules", x => x.schedule_id);
                    table.CheckConstraint("ck_operating_schedules_option_code_canonical", "option_code IS NULL OR (char_length(option_code) > 0 AND option_code = btrim(option_code))");
                    table.CheckConstraint("ck_operating_schedules_service_code_canonical", "char_length(service_code) > 0 AND service_code = btrim(service_code)");
                    table.CheckConstraint("ck_operating_schedules_timezone_canonical", "char_length(time_zone_id) > 0 AND time_zone_id = btrim(time_zone_id)");
                });

            migrationBuilder.CreateTable(
                name: "operating_windows",
                schema: "shipping",
                columns: table => new
                {
                    window_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weekday = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operating_windows", x => x.window_id);
                    table.CheckConstraint("ck_operating_windows_capacity_positive", "capacity IS NULL OR (capacity > 0 AND capacity <= 1000000)");
                    table.CheckConstraint("ck_operating_windows_range_half_open", "end_time > start_time");
                    table.CheckConstraint("ck_operating_windows_weekday_iso_range", "weekday >= 1 AND weekday <= 7");
                    table.ForeignKey(
                        name: "fk_operating_windows_operating_schedules_schedule_id",
                        column: x => x.schedule_id,
                        principalSchema: "shipping",
                        principalTable: "operating_schedules",
                        principalColumn: "schedule_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_operating_schedules_service_option",
                schema: "shipping",
                table: "operating_schedules",
                columns: new[] { "service_code", "option_code" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_operating_windows_schedule_weekday",
                schema: "shipping",
                table: "operating_windows",
                columns: new[] { "schedule_id", "weekday" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operating_windows",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "operating_schedules",
                schema: "shipping");
        }
    }
}
