using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Story.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryReviewOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "origin",
                schema: "story",
                table: "stories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                schema: "story",
                table: "stories",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "review_status",
                schema: "story",
                table: "stories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reviewed_at",
                schema: "story",
                table: "stories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reviewed_by_actor_user_id",
                schema: "story",
                table: "stories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "seller_party_id",
                schema: "story",
                table: "stories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "submitted_at",
                schema: "story",
                table: "stories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "submitted_by_actor_user_id",
                schema: "story",
                table: "stories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_stories_tenant_id_review_status",
                schema: "story",
                table: "stories",
                columns: new[] { "tenant_id", "review_status" });

            migrationBuilder.CreateIndex(
                name: "ix_stories_tenant_id_seller_party_id",
                schema: "story",
                table: "stories",
                columns: new[] { "tenant_id", "seller_party_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_stories_tenant_id_review_status",
                schema: "story",
                table: "stories");

            migrationBuilder.DropIndex(
                name: "ix_stories_tenant_id_seller_party_id",
                schema: "story",
                table: "stories");

            migrationBuilder.DropColumn(
                name: "origin",
                schema: "story",
                table: "stories");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                schema: "story",
                table: "stories");

            migrationBuilder.DropColumn(
                name: "review_status",
                schema: "story",
                table: "stories");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                schema: "story",
                table: "stories");

            migrationBuilder.DropColumn(
                name: "reviewed_by_actor_user_id",
                schema: "story",
                table: "stories");

            migrationBuilder.DropColumn(
                name: "seller_party_id",
                schema: "story",
                table: "stories");

            migrationBuilder.DropColumn(
                name: "submitted_at",
                schema: "story",
                table: "stories");

            migrationBuilder.DropColumn(
                name: "submitted_by_actor_user_id",
                schema: "story",
                table: "stories");
        }
    }
}
