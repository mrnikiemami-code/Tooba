using System.Text.Json;
using Xunit;
using Tooba.Tax.Contracts;
using Tooba.Tax.Domain;

namespace Tooba.Tax.Tests.Contracts;

public sealed class TaxCalculationShapeTests
{
    [Fact]
    public void TaxCalculationResult_roundtrips_json()
    {
        var original = new TaxCalculationResult(
            TaxOutcome.Taxable,
            100m,
            0.09m,
            9m,
            109m,
            "IRR",
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            DateTimeOffset.Parse("2026-01-01Z"));
        var json = JsonSerializer.Serialize(original);
        var copy = JsonSerializer.Deserialize<TaxCalculationResult>(json);
        Assert.NotNull(copy);
        Assert.Equal(original, copy);
    }
}
