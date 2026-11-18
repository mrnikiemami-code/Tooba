using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Story.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tb281CreatedByAndOccurrences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by_actor_user_id",
                schema: "story",
                table: "stories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "command_occurrences",
                schema: "story",
                columns: table => new
                {
                    occurrence_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_party_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    intent_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    story_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_command_occurrences", x => x.occurrence_key);
                });

            migrationBuilder.CreateIndex(
                name: "ix_command_occurrences_tenant_id_seller_party_id_created_at",
                schema: "story",
                table: "command_occurrences",
                columns: new[] { "tenant_id", "seller_party_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "command_occurrences",
                schema: "story");

            migrationBuilder.DropColumn(
                name: "created_by_actor_user_id",
                schema: "story",
                table: "stories");
        }
    }
}
