using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Tooba.PlatformProbe.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// مهاجرت دستی اولیهٔ schema <c>platform_probe</c> و جدول <c>probe_records</c>.
    /// snapshot/designer تولیدشده را فقط برای توضیح فارسی ویرایش نکنید.
    /// </summary>
    public partial class InitialPlatformProbe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "platform_probe");

            migrationBuilder.CreateTable(
                name: "probe_records",
                schema: "platform_probe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    external_reference = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_probe_records", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "probe_records",
                schema: "platform_probe");
        }
    }
}
