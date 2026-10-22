using Microsoft.EntityFrameworkCore;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Contracts.Operations;
using Tooba.Shipping.Domain.Aggregates;
using Xunit;

namespace Tooba.Shipping.Tests.Operations;

/// <summary>
/// TB-SHIPPING-FULL-OPERATIONS-076 — ownership and transport shape of the read-only operational
/// evaluation route: it stays under the admin convention, uses the EXISTING <c>fulfillment.view</c>
/// permission, and its declared validation rejects a malformed input as a client error.
/// </summary>
public sealed class ShippingOperationalQuoteEndpointOwnershipTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private static string EndpointsSource() => File.ReadAllText(Path.Combine(
        RepoRoot(),
        "src",
        "backend",
        "Modules",
        "Shipping",
        "Tooba.Shipping.Endpoints",
        "Admin",
        "ShippingOperationalQuoteEndpoints.cs"));

    [Fact]
    public void The_single_operational_route_is_mapped_and_authorized_with_the_existing_view_permission()
    {
        var source = EndpointsSource();

        Assert.Contains(
            "MapPost(\"/shipping-services/{serviceCode}/operational-quote\"",
            source,
            StringComparison.Ordinal);

        // One route, one authorization call: no mapped route may be reachable anonymously.
        Assert.Equal(1, CountOccurrences(source, "group.Map"));
        Assert.Equal(1, CountOccurrences(source, "authorizer.RequireAuthorizedAsync("));
        Assert.Contains("ShippingAdminPermissions.View", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ShippingAdminPermissions.Manage", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowAnonymous", source, StringComparison.Ordinal);

        // No write seam is introduced by this route.
        Assert.DoesNotContain("MapPut", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPatch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete", source, StringComparison.Ordinal);
    }

    [Fact]
    public void The_route_hands_the_route_service_code_to_a_read_only_query()
    {
        var source = EndpointsSource();

        Assert.Contains(
            "new EvaluateShippingOperationalQuoteQuery(serviceCode, body)",
            source,
            StringComparison.Ordinal);
        Assert.Contains("ISender sender", source, StringComparison.Ordinal);
    }

    [Fact]
    public void The_route_is_mapped_by_the_module_under_the_admin_group()
    {
        var module = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "backend",
            "Modules",
            "Shipping",
            "Tooba.Shipping.Endpoints",
            "ShippingEndpointModule.cs"));

        Assert.Contains("ShippingOperationalQuoteEndpoints.Map(admin);", module, StringComparison.Ordinal);
    }

    [Fact]
    public void The_module_registers_the_operational_seam_in_module_owned_di_without_a_host_edit()
    {
        var module = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "backend",
            "Modules",
            "Shipping",
            "Tooba.Shipping.Infrastructure",
            "DependencyInjection",
            "ShippingModule.cs"));

        Assert.Contains(
            "AddScoped<IShippingOperationalQuoteLookup, ShippingOperationalQuoteLookup>",
            module,
            StringComparison.Ordinal);
        Assert.DoesNotContain("AddModuleSchemaMigrator", module, StringComparison.Ordinal);
        Assert.DoesNotContain("Database.MigrateAsync", module, StringComparison.Ordinal);
    }

    [Fact]
    public void The_operational_seam_is_registered_exactly_once_and_scoped()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        new Tooba.Shipping.Infrastructure.DependencyInjection.ShippingModule().AddServices(
            services,
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
            new StubHostEnvironment());

        var descriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(IShippingOperationalQuoteLookup));

        Assert.Equal(
            typeof(Tooba.Shipping.Infrastructure.Operations.ShippingOperationalQuoteLookup),
            descriptor.ImplementationType);
        Assert.Equal(
            Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped,
            descriptor.Lifetime);
    }

    [Fact]
    public void Transport_shape_invalid_weight_count_and_currency_are_denied_with_stable_codes()
    {
        var validator = new ShippingOperationalQuoteEvaluationRequestValidator();

        var negativeWeight = validator.Validate(Request(weight: -1));
        Assert.Contains(
            negativeWeight.Errors,
            e => e.ErrorCode == ShippingOperationalValidationCodes.WeightNegative);

        var oversizedWeight = validator.Validate(Request(weight: ShippingRatePolicy.WeightGramsMaxValue + 1));
        Assert.Contains(
            oversizedWeight.Errors,
            e => e.ErrorCode == ShippingOperationalValidationCodes.WeightOutOfRange);

        // Zero items is an empty shipment, not a measurement: it is refused, never treated as "unmeasured".
        var zeroCount = validator.Validate(Request(itemCount: 0));
        Assert.Contains(
            zeroCount.Errors,
            e => e.ErrorCode == ShippingOperationalValidationCodes.ItemCountOutOfRange);

        var badCurrency = validator.Validate(Request(currency: "IR"));
        Assert.Contains(
            badCurrency.Errors,
            e => e.ErrorCode == ShippingOperationalValidationCodes.CurrencyInvalid);

        var negativeSubtotal = validator.Validate(Request(subtotal: -1m));
        Assert.Contains(
            negativeSubtotal.Errors,
            e => e.ErrorCode == ShippingOperationalValidationCodes.CartSubtotalNegative);

        // The documented happy path stays valid: an omitted measurement and no currency check.
        Assert.True(validator.Validate(Request()).IsValid);
    }

    [Fact]
    public void A_missing_scope_is_a_declared_validation_failure_not_a_null_reference()
    {
        var validator = new ShippingOperationalQuoteEvaluationRequestValidator();

        var missing = validator.Validate(new ShippingOperationalQuoteEvaluationRequest(null!));

        Assert.False(missing.IsValid);
        Assert.Contains(
            missing.Errors,
            e => e.ErrorCode == ShippingOperationalValidationCodes.ScopeRequired);
    }

    [Fact]
    public void A_service_area_without_its_owning_city_is_denied()
    {
        var validator = new ShippingOperationalQuoteEvaluationRequestValidator();

        var incoherent = validator.Validate(new ShippingOperationalQuoteEvaluationRequest(
            new ShippingDestinationScopeWrite(Guid.NewGuid(), null, null, Guid.NewGuid())));

        Assert.Contains(
            incoherent.Errors,
            e => e.ErrorCode == ShippingOperationalValidationCodes.ServiceAreaRequiresCity);
    }

    [Fact]
    public void The_query_validator_rejects_an_empty_service_code_and_an_oversized_option()
    {
        var validator = new EvaluateShippingOperationalQuoteQueryValidator();

        var emptyService = validator.Validate(new Application.Operations.Queries.EvaluateShippingOperationalQuoteQuery(
            string.Empty,
            Request()));
        Assert.Contains(
            emptyService.Errors,
            e => e.ErrorCode == ShippingOperationalValidationCodes.ServiceCodeRequired);

        var longOption = validator.Validate(new Application.Operations.Queries.EvaluateShippingOperationalQuoteQuery(
            "tipax",
            Request(option: new string('x', ShippingRatePolicy.OptionCodeMaxLength + 1))));
        Assert.Contains(
            longOption.Errors,
            e => e.ErrorCode == ShippingOperationalValidationCodes.OptionCodeTooLong);
    }

    [Fact]
    public void The_operational_contract_carries_no_customer_promise_or_payment_transaction_field()
    {
        var quoteProperties = typeof(ShippingOperationalQuote).GetProperties().Select(x => x.Name).ToArray();

        foreach (var forbidden in new[]
                 {
                     "PromisedDeliveryDate", "DeliveryDate", "EstimatedArrival", "PaymentId",
                     "TransactionId", "LedgerEntryId", "ReservationId",
                 })
        {
            Assert.DoesNotContain(forbidden, quoteProperties);
        }

        // The explicit split fields ARE present, so the payment boundary is stated rather than implied.
        Assert.Contains("AmountPayableAtCheckout", quoteProperties);
        Assert.Contains("AmountCollectOnDelivery", quoteProperties);
    }

    private static ShippingOperationalQuoteEvaluationRequest Request(
        long? weight = null,
        int? itemCount = null,
        string? currency = "IRR",
        decimal subtotal = 0m,
        string? option = null)
        => new(
            new ShippingDestinationScopeWrite(Guid.NewGuid()),
            subtotal,
            currency,
            option,
            weight,
            itemCount);

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private sealed class StubHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";

        public string ApplicationName { get; set; } = "Tooba.Host";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
