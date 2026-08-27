using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Party.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrganizationProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address_line",
                schema: "party",
                table: "parties",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "party",
                table: "parties",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "support_email",
                schema: "party",
                table: "parties",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "support_phone",
                schema: "party",
                table: "parties",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "address_line",
                schema: "party",
                table: "parties");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "party",
                table: "parties");

            migrationBuilder.DropColumn(
                name: "support_email",
                schema: "party",
                table: "parties");

            migrationBuilder.DropColumn(
                name: "support_phone",
                schema: "party",
                table: "parties");
        }
    }
}
