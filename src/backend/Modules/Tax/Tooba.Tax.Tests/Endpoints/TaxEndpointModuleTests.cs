using Xunit;
using Tooba.Tax.Endpoints;

namespace Tooba.Tax.Tests.Endpoints;

public sealed class TaxEndpointModuleTests
{
    [Fact]
    public void MapTaxModule_method_exists()
    {
        var method = typeof(TaxEndpointModule).GetMethod(nameof(TaxEndpointModule.MapTaxModule));
        Assert.NotNull(method);
        Assert.True(method!.IsStatic);
    }
}
