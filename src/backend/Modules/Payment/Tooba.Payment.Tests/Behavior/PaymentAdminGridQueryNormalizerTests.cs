using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Payment.Endpoints.Admin;
using Xunit;

namespace Tooba.Payment.Tests.Behavior;

/// <summary>
/// Focused Payment-owned admin grid normalizer tests
/// (TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001-R1).
/// Proves every grid structural validation error surfaces as a stable semantic code
/// instead of escaping as <see cref="GridQueryValidationException"/> (which would map to 500).
/// </summary>
public sealed class PaymentAdminGridQueryNormalizerTests
{
    private static readonly string[] CanonicalFields =
        ["reference", "customer", "amount", "status", "supply", "reservation", "provider", "created", "completed"];

    [Fact]
    public void Accepts_canonical_fields_in_sort_and_filter()
    {
        var normalizer = new PaymentAdminGridQueryNormalizer();
        var request = new GridQueryRequest(
            1,
            20,
            null,
            CanonicalFields.Select(f => new GridSortRequest(f, "asc")).ToArray(),
            [new GridFilterRequest("status", "equals", "Paid", null, null)],
            null);

        var normalized = normalizer.Normalize(request);

        Assert.Equal(GridQueryPolicyBase.DefaultMaxSortCount, normalized.Sort.Count);
        Assert.All(normalized.Sort, s => Assert.Contains(s.Field, CanonicalFields));
        Assert.Single(normalized.Filters);
    }

    [Fact]
    public void Rejects_unknown_filter_field_as_semantic_field_error()
    {
        var normalizer = new PaymentAdminGridQueryNormalizer();
        var request = new GridQueryRequest(
            1,
            20,
            null,
            [],
            [new GridFilterRequest("unknown", "contains", "x", null, null)],
            null);

        AssertSemantic(normalizer, request, "grid.filter.field.invalid");
    }

    [Fact]
    public void Rejects_invalid_operator_as_semantic_operator_error()
    {
        var normalizer = new PaymentAdminGridQueryNormalizer();
        var request = new GridQueryRequest(
            1,
            20,
            null,
            [],
            [new GridFilterRequest("reference", "between", "1", "2", null)],
            null);

        AssertSemantic(normalizer, request, "grid.filter.operator.invalid");
    }

    [Fact]
    public void Rejects_advanced_filter_field_as_semantic_advanced_field_error()
    {
        var normalizer = new PaymentAdminGridQueryNormalizer();
        var request = new GridQueryRequest(
            1,
            20,
            null,
            [],
            [],
            Advanced(
                [Condition("unknown", "equals", "x"), Condition("status", "equals", "Paid")],
                ["and"]));

        AssertSemantic(normalizer, request, "grid.advancedFilter.field.invalid");
    }

    [Fact]
    public void Rejects_advanced_connector_count_as_semantic_connector_count_error()
    {
        var normalizer = new PaymentAdminGridQueryNormalizer();
        var request = new GridQueryRequest(
            1,
            20,
            null,
            [],
            [],
            Advanced(
                [Condition("status", "equals", "Paid"), Condition("provider", "equals", "zarinpal")],
                []));

        AssertSemantic(normalizer, request, "grid.advancedFilter.connector.count");
    }

    [Fact]
    public void Rejects_advanced_connector_value_as_semantic_connector_invalid_error()
    {
        var normalizer = new PaymentAdminGridQueryNormalizer();
        var request = new GridQueryRequest(
            1,
            20,
            null,
            [],
            [],
            Advanced(
                [Condition("status", "equals", "Paid"), Condition("provider", "equals", "zarinpal")],
                ["xor"]));

        AssertSemantic(normalizer, request, "grid.advancedFilter.connector.invalid");
    }

    [Fact]
    public void No_grid_query_validation_exception_escapes_the_normalizer()
    {
        var normalizer = new PaymentAdminGridQueryNormalizer();
        var requests = new[]
        {
            new GridQueryRequest(1, 20, null, [], [new GridFilterRequest("unknown", "contains", "x", null, null)], null),
            new GridQueryRequest(1, 20, null, [], [new GridFilterRequest("reference", "between", "1", "2", null)], null),
            new GridQueryRequest(1, 20, null, [], [], Advanced([Condition("unknown", "equals", "x"), Condition("status", "equals", "Paid")], ["and"])),
            new GridQueryRequest(1, 20, null, [], [], Advanced([Condition("status", "equals", "Paid"), Condition("provider", "equals", "zarinpal")], [])),
            new GridQueryRequest(1, 20, null, [], [], Advanced([Condition("status", "equals", "Paid"), Condition("provider", "equals", "zarinpal")], ["xor"])),
        };

        foreach (var request in requests)
        {
            var thrown = Record.Exception(() => normalizer.Normalize(request));
            Assert.IsType<SemanticException>(thrown);
        }
    }

    [Fact]
    public void Semantic_grid_codes_map_to_http_400_through_safe_error_mapper()
    {
        var mapper = new SafeErrorMapper(new ErrorDefinitionCatalog([]));
        var codes = new[]
        {
            "grid.filter.field.invalid",
            "grid.filter.operator.invalid",
            "grid.advancedFilter.field.invalid",
            "grid.advancedFilter.connector.count",
            "grid.advancedFilter.connector.invalid",
        };

        foreach (var code in codes)
        {
            var mapped = mapper.Map(new SemanticException(new SemanticError(code)));
            Assert.Equal(400, mapped.StatusCode);
            Assert.Equal(code, mapped.ErrorCode);
        }
    }

    [Fact]
    public void Preserves_default_sort_paging_and_valid_advanced_expression()
    {
        var normalizer = new PaymentAdminGridQueryNormalizer();

        var fallback = normalizer.Normalize(new GridQueryRequest(0, 0, null, [], [], null));
        Assert.Equal(1, fallback.Page);
        Assert.Equal(20, fallback.PageSize);
        var sort = Assert.Single(fallback.Sort);
        Assert.Equal("created", sort.Field);
        Assert.Equal("desc", sort.Direction);

        var advanced = normalizer.Normalize(new GridQueryRequest(
            1,
            20,
            null,
            [],
            [],
            Advanced([Condition("status", "equals", "Paid"), Condition("provider", "equals", "zarinpal")], ["and"])));
        Assert.NotNull(advanced.AdvancedFilter);
        Assert.Equal(2, advanced.AdvancedFilter!.Conditions.Count);
        Assert.Equal(["and"], advanced.AdvancedFilter.Connectors);
    }

    private static void AssertSemantic(IPaymentAdminGridQueryNormalizer normalizer, GridQueryRequest request, string expectedCode)
    {
        var ex = Assert.Throws<SemanticException>(() => normalizer.Normalize(request));
        Assert.Equal(expectedCode, ex.Error.Code);
    }

    private static GridAdvancedFilterCondition Condition(string field, string op, string value) =>
        new($"adv-{field}", field, op, value, null, null);

    private static GridAdvancedFilterExpression Advanced(
        IReadOnlyList<GridAdvancedFilterCondition> conditions,
        IReadOnlyList<string> connectors) =>
        new(conditions, connectors);
}
