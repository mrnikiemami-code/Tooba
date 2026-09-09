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
            // Replay-safe: local tooba_alpha already has these T006 columns while
            // offer.__ef_migrations_history omitted this row. Fresh DBs add them.
            migrationBuilder.Sql(
                """
                ALTER TABLE offer.offers
                    ADD COLUMN IF NOT EXISTS return_policy_choice character varying(32) NOT NULL DEFAULT 'Default';
                ALTER TABLE offer.offers
                    ADD COLUMN IF NOT EXISTS custom_return_window_days integer NULL;
                """);
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
