using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Offer.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Offer.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// TB-287: additive durable immutable creation-intent fingerprint on an Offer.
    /// Nullable by design — historical rows stay <c>null</c> and are never backfilled or inferred.
    /// </summary>
    [DbContext(typeof(OfferDbContext))]
    [Migration("20261123120000_AddOfferCreationIntentFingerprint")]
    public partial class AddOfferCreationIntentFingerprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "creation_intent_fingerprint",
                schema: "offer",
                table: "offers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "creation_intent_fingerprint", schema: "offer", table: "offers");
        }
    }
}
