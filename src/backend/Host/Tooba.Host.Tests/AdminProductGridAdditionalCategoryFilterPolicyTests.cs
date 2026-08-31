using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Grid;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class AdminProductGridAdditionalCategoryFilterPolicyTests
{
    [Fact]
    public void Normalize_accepts_additionalCategoryNames_text_filter()
    {
        var normalized = AdminProductGridQueryPolicy.Normalize(new GridQueryRequest(
            1,
            20,
            null,
            [],
            [new GridFilterRequest("additionalCategoryNames", "contains", "گوشی", null, null)],
            null));

        Assert.Single(normalized.Filters);
        Assert.Equal("additionalCategoryNames", normalized.Filters[0].Field);
        Assert.Equal("contains", normalized.Filters[0].Operator);
    }

    [Fact]
    public void Normalize_rejects_additionalCategoryNames_as_sort_field()
    {
        var normalized = AdminProductGridQueryPolicy.Normalize(new GridQueryRequest(
            1,
            20,
            null,
            [new GridSortRequest("additionalCategoryNames", "asc")],
            [],
            null));

        Assert.DoesNotContain(normalized.Sort, s => s.Field == "additionalCategoryNames");
        Assert.Equal("updatedAt", normalized.Sort[0].Field);
    }
}
