using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Grid;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class AdminProductGridAdvancedFilterTests
{
    [Fact]
    public void EvaluateLeftToRight_AndBOrC_MatchesDocumentedSemantics()
    {
        var a = Set(1, 2, 3);
        var b = Set(2);
        var c = Set(3, 4);

        var result = GridAdvancedFilterEvaluator.EvaluateLeftToRight(
            [a, b, c],
            ["and", "or"]);

        Assert.Equal(Set(2, 3, 4), result);
    }

    [Fact]
    public void EvaluateLeftToRight_OrBAndC_MatchesDocumentedSemantics()
    {
        var a = Set(1);
        var b = Set(1, 2);
        var c = Set(2);

        var result = GridAdvancedFilterEvaluator.EvaluateLeftToRight(
            [a, b, c],
            ["or", "and"]);

        Assert.Equal(Set(2), result);
    }

    [Fact]
    public void NormalizeAdvancedFilter_RejectsInvalidConnector()
    {
        var expression = new GridAdvancedFilterExpression(
            [
                new GridAdvancedFilterCondition("1", "status", "equals", "Published", null, null),
                new GridAdvancedFilterCondition("2", "title", "contains", "phone", null, null),
            ],
            ["xor"]);

        var ex = Assert.Throws<PlatformHttpException>(() => AdminProductGridQueryPolicy.Normalize(
            new GridQueryRequest(1, 20, null, [], [], expression)));

        Assert.Equal("grid.advancedFilter.connector.invalid", ex.ErrorCode);
    }

    [Fact]
    public void NormalizeAdvancedFilter_RejectsConnectorCountMismatch()
    {
        var expression = new GridAdvancedFilterExpression(
            [new GridAdvancedFilterCondition("1", "status", "equals", "Published", null, null)],
            ["and"]);

        var ex = Assert.Throws<PlatformHttpException>(() => AdminProductGridQueryPolicy.Normalize(
            new GridQueryRequest(1, 20, null, [], [], expression)));

        Assert.Equal("grid.advancedFilter.connector.count", ex.ErrorCode);
    }

    private static HashSet<Guid> Set(params int[] ids) => ids.Select(i => Guid.Parse($"00000000-0000-0000-0000-{i:D012}")).ToHashSet();
}
