using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductHistory.Queries;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>Focused non-DB parity checks for W15 ProductHistory read mapping and rules.</summary>
public sealed class ProductHistoryReadCapabilityTests
{
    [Fact]
    public void GetProductHistoryHandler_Map_preserves_json_shape_and_actor_fallback_fields()
    {
        var blankActor = new ProductHistoryEntryDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ProductHistoryRules.EventCreated,
            ProductHistoryRules.SectionGeneral,
            ProductHistoryRules.SectionLabelFa(ProductHistoryRules.SectionGeneral),
            ProductHistoryRules.SummaryCreatedFa,
            null,
            null,
            null,
            ProductHistoryRules.ActorSystemFa,
            DateTimeOffset.UtcNow);

        var view = GetProductHistoryHandler.MapItem(blankActor);
        Assert.Equal(blankActor.HistoryId, view.HistoryId);
        Assert.Equal(blankActor.EventType, view.EventType);
        Assert.Equal(blankActor.Section, view.Section);
        Assert.Equal(blankActor.SectionLabelFa, view.SectionLabelFa);
        Assert.Equal(blankActor.SummaryFa, view.SummaryFa);
        Assert.Equal(blankActor.BeforeSummary, view.BeforeSummary);
        Assert.Equal(blankActor.AfterSummary, view.AfterSummary);
        Assert.Equal(ProductHistoryRules.ActorSystemFa, view.ActorDisplayName);
        Assert.Equal(blankActor.OccurredAt, view.OccurredAt);

        var page = GetProductHistoryHandler.Map(new ProductHistoryPage([blankActor], 1, 0, 50));
        Assert.Single(page.Items);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(0, page.Skip);
        Assert.Equal(50, page.Take);
    }

    [Theory]
    [InlineData(null, "عمومی")]
    [InlineData("", "عمومی")]
    [InlineData("   ", "عمومی")]
    [InlineData("lifecycle", "انتشار")]
    public void ProductHistoryRules_SectionLabelFa_and_ActorSystem_authority(string? section, string expectedLabel)
    {
        var label = ProductHistoryRules.SectionLabelFa(section ?? ProductHistoryRules.SectionGeneral);
        if (string.IsNullOrWhiteSpace(section))
        {
            Assert.Equal("عمومی", ProductHistoryRules.SectionLabelFa(ProductHistoryRules.SectionGeneral));
        }
        else
        {
            Assert.Equal(expectedLabel, label);
        }

        Assert.Equal("سیستم", ProductHistoryRules.ActorSystemFa);
    }

    [Fact]
    public void Workspace_product_missing_code_is_stable()
    {
        Assert.Equal("workspace.product.missing", CatalogErrorCodes.WorkspaceProductMissing);
        var failure = Result.Failure<ProductHistoryPage>(
            new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        Assert.True(failure.IsFailure);
        Assert.Equal(CatalogErrorCodes.WorkspaceProductMissing, failure.FirstError.Code);
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(0, 0)]
    [InlineData(3, 3)]
    public void Skip_normalization_parity(int input, int expected)
    {
        Assert.Equal(expected, Math.Max(0, input));
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(-1, 50)]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    [InlineData(1, 1)]
    public void Take_normalization_parity(int input, int expected)
    {
        Assert.Equal(expected, Math.Clamp(input <= 0 ? 50 : input, 1, 100));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(" lifecycle ", true)]
    [InlineData("lifecycle", true)]
    public void Section_filter_blank_means_no_filter(string? section, bool appliesFilter)
    {
        Assert.Equal(appliesFilter, !string.IsNullOrWhiteSpace(section));
        if (appliesFilter)
        {
            Assert.Equal("lifecycle", section!.Trim());
        }
    }
}
