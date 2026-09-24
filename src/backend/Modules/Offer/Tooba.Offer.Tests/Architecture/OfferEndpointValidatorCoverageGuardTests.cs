using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Offer.Application.Commands.CreateOffer;
using Tooba.Offer.Application.Queries.ListSellerOffers;
using Xunit;

namespace Tooba.Offer.Tests.Architecture;

/// <summary>
/// TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001 — endpoint-reachable Offer validator coverage.
/// Exactly six Offer.Endpoints-reachable MediatR requests; five carry a concrete transport validator
/// and one is explicitly NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY.
/// </summary>
public sealed class OfferEndpointValidatorCoverageGuardTests
{
    /// <summary>
    /// Explicit classification for every Offer.Endpoints-reachable IRequest.
    /// New input-bearing requests must be added here with a concrete validator.
    /// </summary>
    private static readonly (string RequestTypeName, string Classification)[] Manifest =
    [
        ("ListSellerOffersQuery", "NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY"),
        ("GetOfferQuery", "VALIDATOR_REQUIRED"),
        ("CreateOfferCommand", "VALIDATOR_REQUIRED"),
        ("UpdateOfferCommand", "VALIDATOR_REQUIRED"),
        ("SetOfferPriceCommand", "VALIDATOR_REQUIRED"),
        ("SetOfferInventoryCommand", "VALIDATOR_REQUIRED"),
    ];

    [Fact]
    public void Exactly_six_endpoint_reachable_requests_are_manifested_once()
    {
        Assert.Equal(6, Manifest.Length);
        var names = Manifest.Select(x => x.RequestTypeName).ToArray();
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());

        var endpointText = EndpointSource();
        foreach (var (name, _) in Manifest)
        {
            Assert.Contains(name, endpointText, StringComparison.Ordinal);
        }

        var constructed = Regex.Matches(endpointText, @"\bnew\s+([A-Za-z0-9_]+(?:Command|Query))\b")
            .Select(m => m.Groups[1].Value)
            .Where(n => n is not ("Command" or "Query"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var required = Manifest.Select(x => x.RequestTypeName).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(required, constructed);
    }

    [Fact]
    public void Five_required_validators_resolve_via_foundation_DI_and_auth_scoped_query_has_none()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(CreateOfferCommand).Assembly);
        using var sp = services.BuildServiceProvider();

        var appAsm = typeof(CreateOfferCommand).Assembly;

        foreach (var (name, classification) in Manifest.Where(x => x.Classification == "VALIDATOR_REQUIRED"))
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == name);
            var validatorType = typeof(IValidator<>).MakeGenericType(requestType);
            Assert.True(sp.GetService(validatorType) is not null, $"missing IValidator<{name}>");
        }

        foreach (var (name, classification) in Manifest.Where(x => x.Classification == "NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY"))
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == name);
            Assert.True(
                sp.GetService(typeof(IValidator<>).MakeGenericType(requestType)) is null,
                $"{name} is classified {classification} but a validator is registered");
        }
    }

    [Fact]
    public void All_six_requests_are_real_mediatr_requests_and_endpoints_use_ISender()
    {
        var appAsm = typeof(CreateOfferCommand).Assembly;
        foreach (var (name, _) in Manifest)
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == name);
            Assert.Contains(
                requestType.GetInterfaces(),
                i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>));
        }

        var endpointText = EndpointSource();
        Assert.Contains("ISender sender", endpointText, StringComparison.Ordinal);
        Assert.Contains("api.From(", endpointText, StringComparison.Ordinal);
        foreach (var forbidden in new[]
                 {
                     "IOfferStore",
                     "IOfferQueryGateway",
                     "IOfferLookupGateway",
                     "OfferDirectory",
                     ".SetPriceAsync(",
                     ".SetInventoryAsync(",
                     "ISellerOfferPricingGateway",
                     "ISellerOfferInventoryGateway",
                 })
        {
            Assert.DoesNotContain(forbidden, endpointText, StringComparison.Ordinal);
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
    public void Auth_scoped_seller_query_reason_is_explicit()
    {
        // SellerPartyId for ListSellerOffersQuery comes from the trusted seller authorization boundary,
        // not from an arbitrary request payload, so no transport validator is applicable.
        var query = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src", "backend", "Modules", "Offer", "Tooba.Offer.Application",
            "Queries", "ListSellerOffers", "ListSellerOffersQuery.cs"));
        Assert.Contains("public sealed record ListSellerOffersQuery(Guid SellerPartyId)", query, StringComparison.Ordinal);
        Assert.DoesNotContain("SellerPartyId =", query, StringComparison.Ordinal);

        var endpoint = EndpointSource();
        Assert.Contains("ListSellerOffersQuery(sellerId)", endpoint, StringComparison.Ordinal);
        Assert.Contains("RequireAuthorizedAsync", endpoint, StringComparison.Ordinal);
    }

    private static string EndpointSource()
    {
        var root = Path.Combine(RepoRoot(), "src", "backend", "Modules", "Offer", "Tooba.Offer.Endpoints");
        return string.Join(
            "\n",
            Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(File.ReadAllText));
    }

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
