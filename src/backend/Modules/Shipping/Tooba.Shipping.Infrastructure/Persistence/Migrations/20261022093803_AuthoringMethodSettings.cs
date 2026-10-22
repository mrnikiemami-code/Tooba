using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthoringMethodSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authoring_method_settings",
                schema: "shipping",
                columns: table => new
                {
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    freight_payment_mode = table.Column<int>(type: "integer", nullable: false),
                    calculation_mode = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authoring_method_settings", x => x.draft_id);
                    table.CheckConstraint("ck_authoring_method_settings_calculation_mode", "calculation_mode IN (0, 1, 2)");
                    table.CheckConstraint("ck_authoring_method_settings_freight_mode", "freight_payment_mode IN (0, 1)");
                    table.ForeignKey(
                        name: "fk_authoring_method_settings_draft",
                        column: x => x.draft_id,
                        principalSchema: "shipping",
                        principalTable: "authoring_drafts",
                        principalColumn: "draft_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authoring_method_settings",
                schema: "shipping");
        }
    }
}
