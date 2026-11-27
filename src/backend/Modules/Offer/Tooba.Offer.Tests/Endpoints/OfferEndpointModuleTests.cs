using System.Text.Json;
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

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"amount\":null}")]
    public void Missing_price_amount_remains_missing_at_json_boundary(string json)
    {
        var request = JsonSerializer.Deserialize<SellerOfferPriceWriteRequest>(json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(request);
        Assert.Null(request!.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100000)]
    public void Explicit_price_amount_is_preserved_at_json_boundary(int amount)
    {
        var json = JsonSerializer.Serialize(new { amount });
        var request = JsonSerializer.Deserialize<SellerOfferPriceWriteRequest>(json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(request);
        Assert.Equal((decimal?)amount, request!.Amount);
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
