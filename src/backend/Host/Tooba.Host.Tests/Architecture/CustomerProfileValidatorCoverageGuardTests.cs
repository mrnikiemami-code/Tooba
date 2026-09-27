using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.CustomerProfile.Application.Commands.UpsertCustomerProfile;
using Tooba.CustomerProfile.Application.Queries.GetCustomerAccountDashboard;
using Tooba.CustomerProfile.Application.Queries.GetCustomerProfilePage;
using Tooba.CustomerProfile.Application.Validators.UpsertCustomerProfile;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001 — endpoint-reachable CustomerProfile request/validator coverage.
/// 3 HTTP-owned MediatR requests: 2 NO_VALIDATOR_REQUIRED (server actor only) + 1 VALIDATOR_REQUIRED (profile write body).
/// </summary>
public sealed class CustomerProfileValidatorCoverageGuardTests
{
    private const string ValidatorRequired = "VALIDATOR_REQUIRED_PRESENT";
    private const string NoValidatorRequired = "NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT";

    private static readonly (string RequestTypeName, string Route, string Classification)[] Manifest =
    [
        ("GetCustomerAccountDashboardQuery", "GET /v1/customer/dashboard", NoValidatorRequired),
        ("GetCustomerProfilePageQuery", "GET /v1/customer/profile", NoValidatorRequired),
        ("UpsertCustomerProfileCommand", "PUT /v1/customer/profile", ValidatorRequired),
    ];

    [Fact]
    public void Manifest_is_exactly_3_with_1_required_and_2_no_validator()
    {
        Assert.Equal(3, Manifest.Length);
        Assert.Equal(1, Manifest.Count(x => x.Classification == ValidatorRequired));
        Assert.Equal(2, Manifest.Count(x => x.Classification == NoValidatorRequired));
    }

    [Fact]
    public void Endpoint_constructions_match_manifest()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "backend", "Modules", "CustomerProfile", "Tooba.CustomerProfile.Endpoints", "Customer", "CustomerProfileEndpoints.cs"),
            Path.Combine(root, "src", "backend", "Modules", "CustomerProfile", "Tooba.CustomerProfile.Endpoints", "CustomerDashboard", "CustomerAccountDashboardEndpoints.cs"),
        };
        var constructed = files.SelectMany(f => ConstructedRequests(File.ReadAllText(f)))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        var manifested = Manifest.Select(x => x.RequestTypeName).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(manifested, constructed);
    }

    [Fact]
    public void Validators_resolve_via_foundation_DI()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(GetCustomerProfilePageQuery).Assembly);
        using var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetService<IValidator<UpsertCustomerProfileCommand>>());
        Assert.Equal(
            typeof(UpsertCustomerProfileCommandValidator),
            sp.GetRequiredService<IValidator<UpsertCustomerProfileCommand>>().GetType());
        Assert.Null(sp.GetService<IValidator<GetCustomerProfilePageQuery>>());
        Assert.Null(sp.GetService<IValidator<GetCustomerAccountDashboardQuery>>());
    }

    [Fact]
    public void Requests_are_mediatr_and_endpoints_use_ISender()
    {
        foreach (var (name, _, _) in Manifest)
        {
            var type = typeof(GetCustomerProfilePageQuery).Assembly.GetTypes().Single(t => t.Name == name);
            Assert.True(type.IsAssignableTo(typeof(MediatR.IBaseRequest)));
        }

        var root = FindRepoRoot();
        foreach (var relative in new[]
                 {
                     Path.Combine("Customer", "CustomerProfileEndpoints.cs"),
                     Path.Combine("CustomerDashboard", "CustomerAccountDashboardEndpoints.cs"),
                 })
        {
            var text = File.ReadAllText(Path.Combine(
                root, "src", "backend", "Modules", "CustomerProfile", "Tooba.CustomerProfile.Endpoints", relative));
            Assert.Contains("ISender", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ICustomerProfileDirectory", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContextOptions", text, StringComparison.Ordinal);
        }
    }

    private static IEnumerable<string> ConstructedRequests(string source)
    {
        foreach (Match match in Regex.Matches(source, @"new\s+(\w+)\s*\("))
        {
            var name = match.Groups[1].Value;
            if (name.EndsWith("Query", StringComparison.Ordinal) || name.EndsWith("Command", StringComparison.Ordinal))
            {
                yield return name;
            }
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
