using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Commands.CreateShippingService;
using Tooba.Fulfillment.Application.Commands.DeactivateShippingService;
using Tooba.Fulfillment.Application.Commands.ExecuteAdminFulfillmentBulk;
using Tooba.Fulfillment.Application.Commands.SellerMutateFulfillment;
using Tooba.Fulfillment.Application.Commands.UpdateShippingService;
using Tooba.Fulfillment.Application.Queries.GetAdminFulfillment;
using Tooba.Fulfillment.Application.Queries.GetSellerFulfillment;
using Tooba.Fulfillment.Application.Queries.GetShippingService;
using Tooba.Fulfillment.Application.Queries.ListCustomerCheckoutFulfillments;
using Tooba.Fulfillment.Application.Queries.QueryAdminFulfillmentWorkQueue;
using Tooba.Fulfillment.Application.Validators.Admin;
using Tooba.Fulfillment.Application.Validators.Customer;
using Tooba.Fulfillment.Application.Validators.Seller;
using Tooba.Fulfillment.Application.Validators.Shipping;
using Xunit;

namespace Tooba.Fulfillment.Tests.Architecture;

/// <summary>
/// TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001 — exhaustive Fulfillment
/// endpoint-reachable validator coverage. Exactly 15 endpoint-reachable MediatR requests:
/// 10 carry a concrete transport validator and 5 are explicitly NO_VALIDATOR_REQUIRED
/// (2 NO_INPUT + 1 AUTH_SCOPED_QUERY + 2 OPTIONAL_PRESENTATION_LOCALE).
/// Fulfillment structure certification remains PENDING.
/// </summary>
public sealed class FulfillmentValidatorCoverageGuardTests
{
    private const string ValidatorRequired = "VALIDATOR_REQUIRED_PRESENT";
    private const string AuthScoped = "NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY";
    private const string NoInput = "NO_VALIDATOR_REQUIRED_NO_INPUT";
    private const string OptionalLocale = "NO_VALIDATOR_REQUIRED_OPTIONAL_PRESENTATION_LOCALE";

    private static readonly (string RequestTypeName, string Surface, string Classification)[] Manifest =
    [
        ("SellerMutateFulfillmentCommand", "Seller", ValidatorRequired),
        ("GetSellerFulfillmentQuery", "Seller", ValidatorRequired),
        ("ListSellerFulfillmentsQuery", "Seller", AuthScoped),
        ("ListCustomerCheckoutFulfillmentsQuery", "Customer", ValidatorRequired),
        ("GetAdminFulfillmentQuery", "Admin", ValidatorRequired),
        ("ExecuteAdminFulfillmentBulkCommand", "Admin", ValidatorRequired),
        ("QueryAdminFulfillmentWorkQueueQuery", "Admin", ValidatorRequired),
        ("ListAdminFulfillmentsQuery", "Admin", NoInput),
        ("CreateShippingServiceCommand", "Shipping", ValidatorRequired),
        ("UpdateShippingServiceCommand", "Shipping", ValidatorRequired),
        ("DeactivateShippingServiceCommand", "Shipping", ValidatorRequired),
        ("GetShippingServiceQuery", "Shipping", ValidatorRequired),
        ("ListShippingServicesQuery", "Shipping", OptionalLocale),
        ("EnsureShippingCatalogSeedCommand", "Shipping", NoInput),
        ("ListEnabledShippingMethodsTreeQuery", "Shipping", OptionalLocale),
    ];

    private static readonly (string Request, Type Validator, string Surface)[] RequiredValidators =
    [
        ("SellerMutateFulfillmentCommand", typeof(SellerMutateFulfillmentCommandValidator), "Seller"),
        ("GetSellerFulfillmentQuery", typeof(GetSellerFulfillmentQueryValidator), "Seller"),
        ("ListCustomerCheckoutFulfillmentsQuery", typeof(ListCustomerCheckoutFulfillmentsQueryValidator), "Customer"),
        ("GetAdminFulfillmentQuery", typeof(GetAdminFulfillmentQueryValidator), "Admin"),
        ("ExecuteAdminFulfillmentBulkCommand", typeof(ExecuteAdminFulfillmentBulkCommandValidator), "Admin"),
        ("QueryAdminFulfillmentWorkQueueQuery", typeof(QueryAdminFulfillmentWorkQueueQueryValidator), "Admin"),
        ("CreateShippingServiceCommand", typeof(CreateShippingServiceCommandValidator), "Shipping"),
        ("UpdateShippingServiceCommand", typeof(UpdateShippingServiceCommandValidator), "Shipping"),
        ("DeactivateShippingServiceCommand", typeof(DeactivateShippingServiceCommandValidator), "Shipping"),
        ("GetShippingServiceQuery", typeof(GetShippingServiceQueryValidator), "Shipping"),
    ];

    [Fact]
    public void Complete_endpoint_inventory_is_exactly_15_with_10_required_and_5_no_validator()
    {
        Assert.Equal(15, Manifest.Length);
        Assert.Equal(10, Manifest.Count(x => x.Classification == ValidatorRequired));
        Assert.Equal(5, Manifest.Count(x => x.Classification != ValidatorRequired));
        Assert.Equal(2, Manifest.Count(x => x.Classification == NoInput));
        Assert.Equal(1, Manifest.Count(x => x.Classification == AuthScoped));
        Assert.Equal(2, Manifest.Count(x => x.Classification == OptionalLocale));

        var names = Manifest.Select(x => x.RequestTypeName).ToArray();
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());

        var required = Manifest
            .Where(x => x.Classification == ValidatorRequired)
            .Select(x => x.RequestTypeName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        var expected = RequiredValidators
            .Select(x => x.Request)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expected, required);
    }

    [Fact]
    public void Endpoint_inventory_matches_endpoint_constructions_exactly()
    {
        var constructed = EndpointRequestConstructions();
        var manifested = Manifest
            .Select(x => x.RequestTypeName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(manifested, constructed);
    }

    [Fact]
    public void Ten_required_validators_resolve_via_foundation_DI_and_five_have_none()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(SellerMutateFulfillmentCommand).Assembly);
        using var sp = services.BuildServiceProvider();

        var appAsm = typeof(SellerMutateFulfillmentCommand).Assembly;

        foreach (var (request, validatorType, _) in RequiredValidators)
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == request);
            var registered = sp.GetService(typeof(IValidator<>).MakeGenericType(requestType));
            Assert.NotNull(registered);
            Assert.Equal(validatorType, registered!.GetType());

            Assert.Contains(
                validatorType.GetInterfaces(),
                i => i.IsGenericType
                    && i.GetGenericTypeDefinition() == typeof(IValidator<>)
                    && i.GetGenericArguments()[0] == requestType);
        }

        foreach (var (name, _, classification) in Manifest.Where(x => x.Classification != ValidatorRequired))
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == name);
            Assert.True(
                sp.GetService(typeof(IValidator<>).MakeGenericType(requestType)) is null,
                $"{name} is {classification} but a validator is registered");
        }
    }

    [Fact]
    public void All_fifteen_requests_are_real_mediatr_requests_and_endpoints_use_ISender()
    {
        var appAsm = typeof(SellerMutateFulfillmentCommand).Assembly;
        foreach (var (name, _, _) in Manifest)
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == name);
            Assert.True(requestType.IsAssignableTo(typeof(MediatR.IBaseRequest)));
        }

        foreach (var file in EndpointFiles())
        {
            var text = File.ReadAllText(file);
            Assert.Contains("ISender sender", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IValidator", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ValidateAsync", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Validator", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void No_fulfillment_handler_directly_invokes_validators()
    {
        foreach (var (request, _, _) in RequiredValidators)
        {
            var handlerText = HandlerSourceFor(request);
            Assert.DoesNotContain("IValidator", handlerText, StringComparison.Ordinal);
            Assert.DoesNotContain("ValidateAsync", handlerText, StringComparison.Ordinal);
            Assert.DoesNotContain("Validator", handlerText, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Validator_folder_layout_is_exactly_seller_customer_admin_shipping()
    {
        var validatorsRoot = Path.Combine(ApplicationRoot(), "Validators");
        var folders = Directory.GetDirectories(validatorsRoot)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "Admin", "Customer", "Seller", "Shipping" }, folders);

        Assert.Equal(2, Directory.GetFiles(Path.Combine(validatorsRoot, "Seller"), "*Validator.cs").Length);
        Assert.Single(Directory.GetFiles(Path.Combine(validatorsRoot, "Customer"), "*Validator.cs"));
        Assert.Equal(3, Directory.GetFiles(Path.Combine(validatorsRoot, "Admin"), "*Validator.cs").Length);
        Assert.Equal(4, Directory.GetFiles(Path.Combine(validatorsRoot, "Shipping"), "*Validator.cs").Length);

        foreach (var path in Directory.EnumerateDirectories(validatorsRoot))
        {
            var name = Path.GetFileName(path);
            Assert.DoesNotContain(name, new[] { "Common", "Helpers", "Utils", "Managers" });
        }
    }

    [Fact]
    public void MediatR_package_version_remains_12_5_0()
    {
        var csproj = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "BuildingBlocks", "Tooba.BuildingBlocks", "Tooba.BuildingBlocks.csproj"));
        Assert.Contains("Include=\"MediatR\" Version=\"12.5.0\"", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Dead_host_alias_residue_is_absent_and_fulfillment_remains_uncertified()
    {
        var hostAlias = Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "FulfillmentReturnsGridAliases.cs");
        Assert.False(File.Exists(hostAlias), "dead Host alias residue must stay removed");

        var state = File.ReadAllText(Path.Combine(
            RepoRoot(), "docs", "architecture", "tmar-current-state.json"));
        using var stateDoc = System.Text.Json.JsonDocument.Parse(state);
        var certified = stateDoc.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()!).ToArray();
        Assert.DoesNotContain("Fulfillment", certified);

        var manifests = File.ReadAllText(Path.Combine(
            RepoRoot(), "docs", "architecture", "tmar-module-structure-manifests.json"));
        var at = manifests.IndexOf("\"uncertifiedHttpOwningModules\"", StringComparison.Ordinal);
        Assert.True(at >= 0, "uncertifiedHttpOwningModules missing");
        var window = manifests[at..Math.Min(manifests.Length, at + 3000)];
        Assert.Contains("Fulfillment", window, StringComparison.Ordinal);
    }

    private static string[] EndpointRequestConstructions() =>
        EndpointFiles()
            .SelectMany(file => Regex.Matches(
                    File.ReadAllText(file),
                    @"sender\.Send\(\s*new\s+([A-Za-z0-9_]+(?:Command|Query))\b")
                .Select(m => m.Groups[1].Value))
            .Where(n => n is not ("Command" or "Query"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<string> EndpointFiles() =>
    [
        Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Endpoints", "Seller", "FulfillmentSellerEndpoints.cs"),
        Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Endpoints", "Admin", "FulfillmentAdminEndpoints.cs"),
        Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Endpoints", "Customer", "FulfillmentCustomerEndpoints.cs"),
        Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Endpoints", "Shipping", "ShippingServiceEndpoints.cs"),
        Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Endpoints", "Shipping", "ShippingMethodsEndpoints.cs"),
    ];

    private static string HandlerSourceFor(string requestTypeName)
    {
        var file = Directory.EnumerateFiles(ApplicationRoot(), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(p => p, StringComparer.Ordinal)
            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == requestTypeName);
        Assert.True(file is not null, $"no handler file found for {requestTypeName}");
        return File.ReadAllText(file!);
    }

    private static string ModuleRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Fulfillment");

    private static string ApplicationRoot() =>
        Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Application");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
