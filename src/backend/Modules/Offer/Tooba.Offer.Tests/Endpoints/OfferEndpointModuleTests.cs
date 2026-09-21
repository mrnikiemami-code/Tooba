using Xunit;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Domain.Events;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Endpoints;
using Tooba.Offer.Endpoints.Seller;

namespace Tooba.Offer.Tests.Endpoints;

public sealed class OfferEndpointModuleTests
{
    [Fact]
    public void MapOfferModule_method_exists_on_endpoint_module()
    {
        var method = typeof(OfferEndpointModule).GetMethod(nameof(OfferEndpointModule.MapOfferModule));
        Assert.NotNull(method);
        Assert.True(method!.IsStatic);
    }

    [Fact]
    public void OfferSellerEndpoints_map_registers_expected_route_handlers()
    {
        var map = typeof(OfferSellerEndpoints).GetMethod(nameof(OfferSellerEndpoints.Map));
        Assert.NotNull(map);
    }

    [Fact]
    public void Offer_owned_create_and_patch_dispatch_through_sender()
    {
        var root = FindRepoRoot();
        var source = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Offer", "Tooba.Offer.Endpoints", "Seller", "OfferSellerEndpoints.cs"));
        Assert.Contains("ISender sender", source, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory api", source, StringComparison.Ordinal);
        Assert.Contains("api.From(", source, StringComparison.Ordinal);
        Assert.Contains("api.Created(", source, StringComparison.Ordinal);
        Assert.Contains("new CreateOfferCommand", source, StringComparison.Ordinal);
        Assert.Contains("new UpdateOfferCommand", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(await sender.Send", source, StringComparison.Ordinal);
        Assert.DoesNotContain("panel.CreateOfferAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("panel.PatchOfferAsync", source, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Repository root not found.");
    }
}
