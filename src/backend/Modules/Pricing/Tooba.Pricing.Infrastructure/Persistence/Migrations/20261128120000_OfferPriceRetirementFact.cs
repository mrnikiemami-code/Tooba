using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tooba.Pricing.Infrastructure.Persistence.Migrations;

/// <summary>Distinguishes a retired authored price from a new amount without rewriting historical facts.</summary>
[DbContext(typeof(PricingDbContext))]
[Migration("20261128120000_OfferPriceRetirementFact")]
public sealed class OfferPriceRetirementFact : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<bool>("retired", "offer_price_changes", schema: "pricing", type: "boolean", nullable: false, defaultValue: false);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("retired", "offer_price_changes", schema: "pricing");
}
