using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Story.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Story.Infrastructure.Persistence.Migrations;

/// <summary>
/// کلید اصلی رخداد فرمان استوری را به سطح tenant محدود می‌کند تا همان کلید رخداد
/// در tenantهای مختلف بدون برخورد سراسری قابل استفاده باشد.
/// </summary>
[DbContext(typeof(StoryDbContext))]
[Migration("20261119020000_Tb281R3TenantScopedCommandOccurrences")]
public partial class Tb281R3TenantScopedCommandOccurrences : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "pk_command_occurrences",
            schema: "story",
            table: "command_occurrences");

        migrationBuilder.AddPrimaryKey(
            name: "pk_command_occurrences",
            schema: "story",
            table: "command_occurrences",
            columns: new[] { "tenant_id", "occurrence_key" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "pk_command_occurrences",
            schema: "story",
            table: "command_occurrences");

        migrationBuilder.AddPrimaryKey(
            name: "pk_command_occurrences",
            schema: "story",
            table: "command_occurrences",
            column: "occurrence_key");
    }
}
