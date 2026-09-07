using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Offer.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Offer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OfferDbContext))]
    [Migration("20260907120000_AddOfferReturnPolicy")]
    public partial class AddOfferReturnPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "return_policy_choice",
                schema: "offer",
                table: "offers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Default");

            migrationBuilder.AddColumn<int>(
                name: "custom_return_window_days",
                schema: "offer",
                table: "offers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "custom_return_window_days",
                schema: "offer",
                table: "offers");

            migrationBuilder.DropColumn(
                name: "return_policy_choice",
                schema: "offer",
                table: "offers");
        }
    }
}
