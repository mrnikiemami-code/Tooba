using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SelectedCapturedRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "selected_captured_revisions",
                schema: "shipping",
                columns: table => new
                {
                    selection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_kind = table.Column<int>(type: "integer", nullable: false),
                    owner_tenant_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    owner_seller_party_id = table.Column<Guid>(type: "uuid", nullable: true),
                    binding_service_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    binding_option_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    selected_snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    selection_version = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_selected_captured_revisions", x => x.selection_id);
                    table.CheckConstraint("ck_selected_captured_revisions_option_key_canonical", "binding_option_key IS NULL OR (char_length(binding_option_key) > 0 AND binding_option_key = lower(btrim(binding_option_key)))");
                    table.CheckConstraint("ck_selected_captured_revisions_owner_coherent", "((owner_kind = 0 AND owner_tenant_id IS NOT NULL AND owner_seller_party_id IS NULL) OR (owner_kind = 1 AND owner_tenant_id IS NULL AND owner_seller_party_id IS NULL) OR (owner_kind = 2 AND owner_tenant_id IS NULL AND owner_seller_party_id IS NOT NULL))");
                    table.CheckConstraint("ck_selected_captured_revisions_selected_snapshot_not_empty", "selected_snapshot_id IS NULL OR selected_snapshot_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_selected_captured_revisions_service_key_canonical", "char_length(binding_service_key) > 0 AND binding_service_key = lower(btrim(binding_service_key))");
                    table.CheckConstraint("ck_selected_captured_revisions_version_positive", "selection_version > 0");
                });

            migrationBuilder.CreateIndex(
                name: "ux_selected_captured_revisions_owner_binding",
                schema: "shipping",
                table: "selected_captured_revisions",
                columns: new[] { "owner_kind", "owner_tenant_id", "owner_seller_party_id", "binding_service_key", "binding_option_key" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "selected_captured_revisions",
                schema: "shipping");
        }
    }
}
