using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.UserPreference.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUiPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ui_preferences",
                schema: "user_preference",
                columns: table => new
                {
                    preference_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    json_payload = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ui_preferences", x => x.preference_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ui_preferences_actor_user_id_key",
                schema: "user_preference",
                table: "ui_preferences",
                columns: new[] { "actor_user_id", "key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ui_preferences",
                schema: "user_preference");
        }
    }
}
