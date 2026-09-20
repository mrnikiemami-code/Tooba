using Xunit;
using Tooba.Pricing.Endpoints;

namespace Tooba.Pricing.Tests.Endpoints;

public sealed class PricingEndpointModuleTests
{
    [Fact]
    public void MapPricingModule_method_exists()
    {
        var method = typeof(PricingEndpointModule).GetMethod(nameof(PricingEndpointModule.MapPricingModule));
        Assert.NotNull(method);
        Assert.True(method!.IsStatic);
    }
}
