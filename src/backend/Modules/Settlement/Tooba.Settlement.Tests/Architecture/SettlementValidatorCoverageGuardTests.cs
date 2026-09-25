using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Settlement.Application.Commands.ProcessAdminPayout;
using Tooba.Settlement.Application.Commands.RequestSellerPayout;
using Tooba.Settlement.Application.Commands.RetryAdminPayout;
using Tooba.Settlement.Application.Queries.GetSellerSettlementBalance;
using Tooba.Settlement.Application.Queries.ListAdminPayoutQueue;
using Tooba.Settlement.Application.Queries.ListAdminSettlementBalances;
using Tooba.Settlement.Application.Queries.ListSellerPayoutRequests;
using Tooba.Settlement.Application.Queries.ListSellerSettlementEntries;
using Tooba.Settlement.Application.Queries.ListSellerSettlementStatements;
using Tooba.Settlement.Application.Queries.QueryAdminPayoutGrid;
using Tooba.Settlement.Application.Validators.Admin;
using Tooba.Settlement.Application.Validators.Seller;
using Xunit;

namespace Tooba.Settlement.Tests.Architecture;

/// <summary>
/// TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001 — complete Settlement
/// endpoint-reachable validator coverage. Exactly 10 endpoint-reachable MediatR requests:
/// 4 carry a concrete transport validator and 6 are explicitly NO_VALIDATOR_REQUIRED
/// (4 AUTH_SCOPED_QUERY + 2 NO_INPUT). Settlement structure certification remains PENDING.
/// </summary>
public sealed class SettlementValidatorCoverageGuardTests
{
    private const string ValidatorRequired = "VALIDATOR_REQUIRED_PRESENT";
    private const string AuthScoped = "NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY";
    private const string NoInput = "NO_VALIDATOR_REQUIRED_NO_INPUT";

    private static readonly (string RequestTypeName, string Surface, string Classification)[] Manifest =
    [
        ("RequestSellerPayoutCommand", "Seller", ValidatorRequired),
        ("GetSellerSettlementBalanceQuery", "Seller", AuthScoped),
        ("ListSellerSettlementEntriesQuery", "Seller", AuthScoped),
        ("ListSellerSettlementStatementsQuery", "Seller", AuthScoped),
        ("ListSellerPayoutRequestsQuery", "Seller", AuthScoped),
        ("ProcessAdminPayoutCommand", "Admin", ValidatorRequired),
        ("RetryAdminPayoutCommand", "Admin", ValidatorRequired),
        ("ListAdminSettlementBalancesQuery", "Admin", NoInput),
        ("ListAdminPayoutQueueQuery", "Admin", NoInput),
        ("QueryAdminPayoutGridQuery", "Admin", ValidatorRequired),
    ];

    private static readonly (string Request, Type Validator, string Surface)[] RequiredValidators =
    [
        ("RequestSellerPayoutCommand", typeof(RequestSellerPayoutCommandValidator), "Seller"),
        ("ProcessAdminPayoutCommand", typeof(ProcessAdminPayoutCommandValidator), "Admin"),
        ("RetryAdminPayoutCommand", typeof(RetryAdminPayoutCommandValidator), "Admin"),
        ("QueryAdminPayoutGridQuery", typeof(QueryAdminPayoutGridQueryValidator), "Admin"),
    ];

    [Fact]
    public void Complete_endpoint_inventory_is_exactly_10_with_4_required_and_6_no_validator()
    {
        Assert.Equal(10, Manifest.Length);
        Assert.Equal(4, Manifest.Count(x => x.Classification == ValidatorRequired));
        Assert.Equal(4, Manifest.Count(x => x.Classification == AuthScoped));
        Assert.Equal(2, Manifest.Count(x => x.Classification == NoInput));
        Assert.Equal(6, Manifest.Count(x => x.Classification != ValidatorRequired));
        Assert.Equal(5, Manifest.Count(x => x.Surface == "Seller"));
        Assert.Equal(5, Manifest.Count(x => x.Surface == "Admin"));

        var names = Manifest.Select(x => x.RequestTypeName).ToArray();
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Seller_inventory_matches_endpoint_constructions_exactly()
    {
        var constructed = ConstructedRequests(Path.Combine(
            SettlementRoot(), "Tooba.Settlement.Endpoints", "Seller", "SettlementSellerEndpoints.cs"));
        var manifested = Manifest
            .Where(x => x.Surface == "Seller")
            .Select(x => x.RequestTypeName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(manifested, constructed);
    }

    [Fact]
    public void Admin_inventory_matches_endpoint_constructions_exactly()
    {
        var constructed = ConstructedRequests(Path.Combine(
            SettlementRoot(), "Tooba.Settlement.Endpoints", "Admin", "SettlementAdminEndpoints.cs"));
        var manifested = Manifest
            .Where(x => x.Surface == "Admin")
            .Select(x => x.RequestTypeName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(manifested, constructed);
    }

    [Fact]
    public void Four_required_validators_resolve_via_foundation_DI_and_six_have_none()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(RequestSellerPayoutCommand).Assembly);
        using var sp = services.BuildServiceProvider();

        var appAsm = typeof(RequestSellerPayoutCommand).Assembly;

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
    public void All_ten_requests_are_real_mediatr_requests_and_endpoints_use_ISender()
    {
        var appAsm = typeof(RequestSellerPayoutCommand).Assembly;
        foreach (var (name, _, _) in Manifest)
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == name);
            Assert.True(requestType.IsAssignableTo(typeof(MediatR.IBaseRequest)));
        }

        foreach (var file in EndpointFiles())
        {
            var text = File.ReadAllText(file);
            Assert.Contains("ISender sender", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void No_endpoint_or_relevant_handler_directly_invokes_validators()
    {
        foreach (var file in EndpointFiles())
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("IValidator", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ValidateAsync", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Validator", text, StringComparison.Ordinal);
        }

        foreach (var (request, _, _) in RequiredValidators)
        {
            var handlerText = HandlerSourceFor(request);
            Assert.DoesNotContain("IValidator", handlerText, StringComparison.Ordinal);
            Assert.DoesNotContain("ValidateAsync", handlerText, StringComparison.Ordinal);
            Assert.DoesNotContain("Validator", handlerText, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Validator_folder_layout_is_exactly_seller_and_admin()
    {
        var validatorsRoot = Path.Combine(SettlementRoot(), "Tooba.Settlement.Application", "Validators");
        var folders = Directory.GetDirectories(validatorsRoot)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "Admin", "Seller" }, folders);

        Assert.Equal(1, Directory.GetFiles(Path.Combine(validatorsRoot, "Seller"), "*Validator.cs").Length);
        Assert.Equal(3, Directory.GetFiles(Path.Combine(validatorsRoot, "Admin"), "*Validator.cs").Length);
    }

    [Fact]
    public void MediatR_package_version_remains_12_5_0()
    {
        var csproj = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "BuildingBlocks", "Tooba.BuildingBlocks", "Tooba.BuildingBlocks.csproj"));
        Assert.Contains("Include=\"MediatR\" Version=\"12.5.0\"", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Settlement_is_structure_certified_under_arch_complete_002()
    {
        var state = File.ReadAllText(Path.Combine(
            RepoRoot(), "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("COMPLETE_4_OF_4_REQUIRED_PRESENT_6_NO_VALIDATOR_REQUIRED", state, StringComparison.Ordinal);

        var manifests = File.ReadAllText(Path.Combine(
            RepoRoot(), "docs", "architecture", "tmar-module-structure-manifests.json"));
        var at = manifests.IndexOf("\"uncertifiedHttpOwningModules\"", StringComparison.Ordinal);
        Assert.True(at >= 0, "uncertifiedHttpOwningModules missing");
        var window = manifests[at..Math.Min(manifests.Length, at + 2000)];
        Assert.DoesNotContain("Settlement", window, StringComparison.Ordinal);

        using var stateDoc = System.Text.Json.JsonDocument.Parse(state);
        var certified = stateDoc.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()!).ToArray();
        Assert.Contains("Settlement", certified);
    }

    private static IReadOnlyList<string> EndpointFiles() =>
    [
        Path.Combine(SettlementRoot(), "Tooba.Settlement.Endpoints", "Seller", "SettlementSellerEndpoints.cs"),
        Path.Combine(SettlementRoot(), "Tooba.Settlement.Endpoints", "Admin", "SettlementAdminEndpoints.cs"),
    ];

    private static string[] ConstructedRequests(string endpointFile) =>
        Regex.Matches(File.ReadAllText(endpointFile), @"\bnew\s+([A-Za-z0-9_]+(?:Command|Query))\b")
            .Select(m => m.Groups[1].Value)
            .Where(n => n is not ("Command" or "Query"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

    private static string HandlerSourceFor(string requestTypeName)
    {
        var applicationRoot = Path.Combine(SettlementRoot(), "Tooba.Settlement.Application");
        var file = Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(p => p, StringComparer.Ordinal)
            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == requestTypeName);
        Assert.True(file is not null, $"no handler file found for {requestTypeName}");
        return File.ReadAllText(file!);
    }

    private static string SettlementRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Settlement");

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
