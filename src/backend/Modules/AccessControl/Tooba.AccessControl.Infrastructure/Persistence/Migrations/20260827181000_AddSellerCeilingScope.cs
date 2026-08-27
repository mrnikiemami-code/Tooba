using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.AccessControl.Infrastructure.Persistence;

#nullable disable

namespace Tooba.AccessControl.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AccessControlDbContext))]
    [Migration("20260827181000_AddSellerCeilingScope")]
    public partial class AddSellerCeilingScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_platform_seller_ceilings_seller_party_id_permission_id",
                schema: "access_control",
                table: "platform_seller_ceilings");

            migrationBuilder.AddColumn<int>(
                name: "scope_kind",
                schema: "access_control",
                table: "platform_seller_ceilings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "scope_resource_id",
                schema: "access_control",
                table: "platform_seller_ceilings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_platform_seller_ceilings_seller_party_id_permission_id_scop",
                schema: "access_control",
                table: "platform_seller_ceilings",
                columns: new[] { "seller_party_id", "permission_id", "scope_kind", "scope_resource_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_platform_seller_ceilings_seller_party_id_permission_id_scop",
                schema: "access_control",
                table: "platform_seller_ceilings");

            migrationBuilder.DropColumn(
                name: "scope_kind",
                schema: "access_control",
                table: "platform_seller_ceilings");

            migrationBuilder.DropColumn(
                name: "scope_resource_id",
                schema: "access_control",
                table: "platform_seller_ceilings");

            migrationBuilder.CreateIndex(
                name: "ix_platform_seller_ceilings_seller_party_id_permission_id",
                schema: "access_control",
                table: "platform_seller_ceilings",
                columns: new[] { "seller_party_id", "permission_id" },
                unique: true);
        }
    }
}
