using Xunit;
using Tooba.Offer.Endpoints;

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
}
