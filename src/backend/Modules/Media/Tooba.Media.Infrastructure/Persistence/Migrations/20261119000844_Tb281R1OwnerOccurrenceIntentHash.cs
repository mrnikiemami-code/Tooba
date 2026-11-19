using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Media.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tb281R1OwnerOccurrenceIntentHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_asset_owner_occurrences",
                schema: "media",
                table: "asset_owner_occurrences");

            migrationBuilder.AddColumn<string>(
                name: "intent_hash",
                schema: "media",
                table: "asset_owner_occurrences",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asset_owner_occurrences",
                schema: "media",
                table: "asset_owner_occurrences",
                columns: new[] { "tenant_id", "seller_party_id", "occurrence_key" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_asset_owner_occurrences",
                schema: "media",
                table: "asset_owner_occurrences");

            migrationBuilder.DropColumn(
                name: "intent_hash",
                schema: "media",
                table: "asset_owner_occurrences");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asset_owner_occurrences",
                schema: "media",
                table: "asset_owner_occurrences",
                column: "occurrence_key");
        }
    }
}
