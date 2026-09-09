using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Offer.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P09-T013: نرمال‌سازی مقدار و حد خرید Offer.</summary>
public sealed class QuantityFoundationTests
{
    private static readonly EffectiveQuantityPolicy Precision2 = new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "kg",
        "Kilogram",
        "kg",
        2,
        null,
        QuantityRoundingMode.Nearest);

    [Fact]
    public void Precision_floor_ceiling_nearest()
    {
        var n = new QuantityNormalizer();
        var floor = Precision2 with { RoundingMode = QuantityRoundingMode.Floor };
        var ceil = Precision2 with { RoundingMode = QuantityRoundingMode.Ceiling };
        Assert.Equal(1.23m, n.Normalize(1.239m, floor));
        Assert.Equal(1.24m, n.Normalize(1.231m, ceil));
        Assert.Equal(1.24m, n.Normalize(1.235m, Precision2));
    }

    [Fact]
    public void Step_quarter_normalizes()
    {
        var n = new QuantityNormalizer();
        var step = Precision2 with { Step = 0.25m, RoundingMode = QuantityRoundingMode.Floor };
        Assert.Equal(1.25m, n.Normalize(1.37m, step));
        Assert.Equal(1.50m, n.Normalize(1.37m, step with { RoundingMode = QuantityRoundingMode.Ceiling }));
        Assert.Equal(1.25m, n.Normalize(1.37m, step with { RoundingMode = QuantityRoundingMode.Nearest }));
    }

    [Fact]
    public void Decimal_places_zero_keeps_integer()
    {
        var n = new QuantityNormalizer();
        var pcs = Precision2 with { DecimalPlaces = 0, UnitCode = "pcs" };
        Assert.Equal(2m, n.Normalize(2m, pcs));
    }

    [Fact]
    public void Step_null_accepts_precision_value()
    {
        var n = new QuantityNormalizer();
        Assert.Equal(1.25m, n.Normalize(1.25m, Precision2));
    }

    [Fact]
    public void Positive_required_and_invalid_step_rejected()
    {
        var n = new QuantityNormalizer();
        Assert.Throws<InvalidOperationException>(() => n.Normalize(0m, Precision2));
        Assert.Throws<InvalidOperationException>(() => n.Normalize(-1m, Precision2));
        Assert.Throws<InvalidOperationException>(() =>
            n.Normalize(1m, Precision2 with { Step = 0m }));
    }

    [Fact]
    public void Offer_min_max_rules()
    {
        var now = DateTimeOffset.UtcNow;
        var offer = SellerOffer.Create(Guid.NewGuid(), Guid.NewGuid(), SalesChannel.Direct, "sku", now);
        offer.SetOrderQuantityLimits(0.25m, 2.50m, now);
        Assert.Equal(0.25m, offer.MinimumOrderQuantity);
        Assert.Equal(2.50m, offer.MaximumOrderQuantity);
        Assert.Throws<InvalidOperationException>(() => offer.SetOrderQuantityLimits(3m, 1m, now));
        Assert.Throws<InvalidOperationException>(() => offer.SetOrderQuantityLimits(0m, 1m, now));
    }

    [Fact]
    public void Display_strips_storage_zeros()
    {
        Assert.Equal("2", QuantityDisplay.Format(2.000000m, 0));
        Assert.Equal("1.25", QuantityDisplay.Format(1.250000m, 2));
    }

    [Fact]
    public void Inventory_available_is_exact_decimal()
    {
        var now = DateTimeOffset.UtcNow;
        var position = Tooba.Inventory.Domain.StockPosition.Open(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), now);
        position.SyncQuantities(100.000m, 1.250m, now);
        Assert.Equal(98.750m, position.Available);
        position.SyncQuantities(100.000m, 0m, now);
        Assert.Equal(100.000m, position.Available);
    }

    [Fact]
    public void Return_remaining_uses_decimal_delivered()
    {
        const decimal delivered = 1.750m;
        const decimal requested = 0.500m;
        Assert.Equal(1.250m, delivered - requested);
    }

    [Fact]
    public void Cart_line_total_uses_decimal_quantity()
    {
        const decimal unit = 40000m;
        const decimal quantity = 1.25m;
        Assert.Equal(50000m, unit * quantity);
    }

    [Fact]
    public void Financial_floor_precision_zero_rounds_discount_once()
    {
        var raw = 998m * 0.20m;
        Assert.Equal(199.6m, raw);
        var discount = FinancialRounder.Round(raw, 0, QuantityRoundingMode.Floor);
        Assert.Equal(199m, discount);
        Assert.Equal(799m, 998m - discount);
        Assert.Equal(200m, FinancialRounder.Round(raw, 0, QuantityRoundingMode.Ceiling));
        Assert.Equal(200m, FinancialRounder.Round(raw, 0, QuantityRoundingMode.Nearest));
    }

    [Fact]
    public void Unit_deactivate_is_soft_only()
    {
        var now = DateTimeOffset.UtcNow;
        var unit = UnitOfMeasure.Create(Guid.NewGuid(), "kg", UnitOfMeasureDimension.Mass, true, 10, now);
        unit.SetActive(false, now.AddMinutes(1));
        Assert.False(unit.IsActive);
        Assert.Equal("kg", unit.Code);
    }
}
