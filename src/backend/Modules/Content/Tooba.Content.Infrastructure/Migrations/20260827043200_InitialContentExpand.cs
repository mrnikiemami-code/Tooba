using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialContentExpand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "body",
                schema: "content",
                table: "articles",
                type: "character varying(50000)",
                maxLength: 50000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "category",
                schema: "content",
                table: "articles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locale",
                schema: "content",
                table: "articles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "fa-IR");

            migrationBuilder.AddColumn<string>(
                name: "seo_description",
                schema: "content",
                table: "articles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_title",
                schema: "content",
                table: "articles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_articles_status_category_publish_date",
                schema: "content",
                table: "articles",
                columns: new[] { "status", "category", "publish_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_articles_status_category_publish_date",
                schema: "content",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "body",
                schema: "content",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "category",
                schema: "content",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "locale",
                schema: "content",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "seo_description",
                schema: "content",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "seo_title",
                schema: "content",
                table: "articles");
        }
    }
}
