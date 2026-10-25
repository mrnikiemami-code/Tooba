using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthoringRevisionSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authoring_revision_snapshots",
                schema: "shipping",
                columns: table => new
                {
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_kind = table.Column<int>(type: "integer", nullable: false),
                    owner_tenant_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    owner_seller_party_id = table.Column<Guid>(type: "uuid", nullable: true),
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_revision = table.Column<long>(type: "bigint", nullable: false),
                    format_version = table.Column<int>(type: "integer", nullable: false),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authoring_revision_snapshots", x => x.snapshot_id);
                    table.CheckConstraint("ck_authoring_revision_snapshots_format_version_known", "format_version = 1");
                    table.CheckConstraint("ck_authoring_revision_snapshots_owner_coherent", "((owner_kind = 0 AND owner_tenant_id IS NOT NULL AND owner_seller_party_id IS NULL) OR (owner_kind = 1 AND owner_tenant_id IS NULL AND owner_seller_party_id IS NULL) OR (owner_kind = 2 AND owner_tenant_id IS NULL AND owner_seller_party_id IS NOT NULL))");
                    table.CheckConstraint("ck_authoring_revision_snapshots_source_revision_positive", "source_revision > 0");
                    table.CheckConstraint("ck_authoring_revision_snapshots_tenant_not_blank", "owner_tenant_id IS NULL OR char_length(owner_tenant_id) > 0");
                });

            migrationBuilder.CreateIndex(
                name: "ux_authoring_revision_snapshots_owner_revision",
                schema: "shipping",
                table: "authoring_revision_snapshots",
                columns: new[] { "owner_kind", "owner_tenant_id", "owner_seller_party_id", "draft_id", "source_revision" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authoring_revision_snapshots",
                schema: "shipping");
        }
    }
}
