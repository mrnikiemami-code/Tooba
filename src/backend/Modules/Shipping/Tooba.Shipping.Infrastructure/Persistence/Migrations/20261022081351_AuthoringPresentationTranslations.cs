using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthoringPresentationTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authoring_draft_translations",
                schema: "shipping",
                columns: table => new
                {
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authoring_draft_translations", x => new { x.draft_id, x.language_id });
                    table.CheckConstraint("ck_authoring_draft_translations_description_length", "description IS NULL OR char_length(description) <= 2000");
                    table.CheckConstraint("ck_authoring_draft_translations_name_length", "char_length(btrim(name)) > 0 AND char_length(name) <= 200");
                    table.ForeignKey(
                        name: "fk_authoring_draft_translations_authoring_drafts_draft_id",
                        column: x => x.draft_id,
                        principalSchema: "shipping",
                        principalTable: "authoring_drafts",
                        principalColumn: "draft_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "authoring_group_titles",
                schema: "shipping",
                columns: table => new
                {
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authoring_group_titles", x => new { x.group_id, x.language_id });
                    table.CheckConstraint("ck_authoring_group_titles_title_length", "char_length(btrim(title)) > 0 AND char_length(title) <= 200");
                    table.ForeignKey(
                        name: "fk_authoring_group_titles_authoring_region_groups_group_id",
                        column: x => x.group_id,
                        principalSchema: "shipping",
                        principalTable: "authoring_region_groups",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authoring_draft_translations",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "authoring_group_titles",
                schema: "shipping");
        }
    }
}
