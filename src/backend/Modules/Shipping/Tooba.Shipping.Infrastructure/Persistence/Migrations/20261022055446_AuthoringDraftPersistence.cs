using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthoringDraftPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authoring_drafts",
                schema: "shipping",
                columns: table => new
                {
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_kind = table.Column<int>(type: "integer", nullable: false),
                    owner_tenant_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    owner_seller_party_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    option_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    revision = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authoring_drafts", x => x.draft_id);
                    table.CheckConstraint("ck_authoring_drafts_option_code_canonical", "option_code IS NULL OR (char_length(option_code) > 0 AND option_code = btrim(option_code))");
                    table.CheckConstraint("ck_authoring_drafts_owner_coherent", "((owner_kind = 0 AND owner_tenant_id IS NOT NULL AND owner_seller_party_id IS NULL) OR (owner_kind = 1 AND owner_tenant_id IS NULL AND owner_seller_party_id IS NULL) OR (owner_kind = 2 AND owner_tenant_id IS NULL AND owner_seller_party_id IS NOT NULL))");
                    table.CheckConstraint("ck_authoring_drafts_revision_positive", "revision > 0");
                    table.CheckConstraint("ck_authoring_drafts_service_code_canonical", "char_length(service_code) > 0 AND service_code = btrim(service_code)");
                    table.CheckConstraint("ck_authoring_drafts_tenant_not_blank", "owner_tenant_id IS NULL OR char_length(owner_tenant_id) > 0");
                });

            migrationBuilder.CreateTable(
                name: "authoring_region_groups",
                schema: "shipping",
                columns: table => new
                {
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authoring_region_groups", x => x.group_id);
                    table.CheckConstraint("ck_authoring_region_groups_order_non_negative", "group_order >= 0");
                    table.ForeignKey(
                        name: "fk_authoring_region_groups_authoring_drafts_draft_id",
                        column: x => x.draft_id,
                        principalSchema: "shipping",
                        principalTable: "authoring_drafts",
                        principalColumn: "draft_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "authoring_selected_areas",
                schema: "shipping",
                columns: table => new
                {
                    selected_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_kind = table.Column<int>(type: "integer", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authoring_selected_areas", x => x.selected_area_id);
                    table.CheckConstraint("ck_authoring_selected_areas_node_kind_known", "node_kind IN (0, 1, 2, 3)");
                    table.ForeignKey(
                        name: "fk_authoring_selected_areas_authoring_region_groups_group_id",
                        column: x => x.group_id,
                        principalSchema: "shipping",
                        principalTable: "authoring_region_groups",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_authoring_drafts_owner",
                schema: "shipping",
                table: "authoring_drafts",
                columns: new[] { "owner_kind", "owner_tenant_id", "owner_seller_party_id" });

            migrationBuilder.CreateIndex(
                name: "ux_authoring_drafts_owner_binding",
                schema: "shipping",
                table: "authoring_drafts",
                columns: new[] { "owner_kind", "owner_tenant_id", "owner_seller_party_id", "service_code", "option_code" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ux_authoring_region_groups_draft_order",
                schema: "shipping",
                table: "authoring_region_groups",
                columns: new[] { "draft_id", "group_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_authoring_selected_areas_group_id",
                schema: "shipping",
                table: "authoring_selected_areas",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ux_authoring_selected_areas_draft_node",
                schema: "shipping",
                table: "authoring_selected_areas",
                columns: new[] { "draft_id", "node_kind", "node_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authoring_selected_areas",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "authoring_region_groups",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "authoring_drafts",
                schema: "shipping");
        }
    }
}
