using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Media.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Media.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(MediaDbContext))]
    [Migration("20260916053000_AddMediaFocalPoint")]
    public partial class AddMediaFocalPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "focal_point_x",
                schema: "media",
                table: "assets",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "focal_point_y",
                schema: "media",
                table: "assets",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "focal_point_x",
                schema: "media",
                table: "assets");

            migrationBuilder.DropColumn(
                name: "focal_point_y",
                schema: "media",
                table: "assets");
        }
    }
}
