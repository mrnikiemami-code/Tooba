using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthoringDraftTypeAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "shipping_type_id",
                schema: "shipping",
                table: "authoring_drafts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_authoring_drafts_shipping_type_nonempty",
                schema: "shipping",
                table: "authoring_drafts",
                sql: "shipping_type_id IS NULL OR shipping_type_id <> '00000000-0000-0000-0000-000000000000'::uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_authoring_drafts_shipping_type_nonempty",
                schema: "shipping",
                table: "authoring_drafts");

            migrationBuilder.DropColumn(
                name: "shipping_type_id",
                schema: "shipping",
                table: "authoring_drafts");
        }
    }
}
