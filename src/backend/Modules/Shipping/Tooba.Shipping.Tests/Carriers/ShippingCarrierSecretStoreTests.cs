using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Shipping.Infrastructure.Carriers;
using Tooba.Shipping.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Shipping.Tests.Carriers;

public sealed class ShippingCarrierSecretStoreTests
{
    [Fact]
    public async Task Put_then_get_roundtrips_and_version_mismatch_returns_null()
    {
        using var context = CreateContext();
        var store = new ShippingCarrierSecretStore(context, Protector());

        await store.PutAsync("shipping.carrier.demo", "user-a", "pass-a", 2, CancellationToken.None);

        var match = await store.TryGetAsync("shipping.carrier.demo", 2, CancellationToken.None);
        Assert.NotNull(match);
        Assert.Equal("user-a", match!.Username);
        Assert.Equal("pass-a", match.Password);
        Assert.Equal(2, match.ConfigurationVersion);

        Assert.Null(await store.TryGetAsync("shipping.carrier.demo", 1, CancellationToken.None));
    }

    [Fact]
    public async Task Ciphertext_is_not_the_plaintext_password()
    {
        using var context = CreateContext();
        var store = new ShippingCarrierSecretStore(context, Protector());
        await store.PutAsync("shipping.carrier.cipher", "merchant", "s3cret", 1, CancellationToken.None);

        var row = Assert.Single(context.CarrierSecretMaterials);
        var text = System.Text.Encoding.UTF8.GetString(row.Ciphertext);
        Assert.DoesNotContain("s3cret", text, StringComparison.Ordinal);
        Assert.DoesNotContain("merchant", text, StringComparison.Ordinal);
    }

    private static ShippingDbContext CreateContext()
        => new(new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"tb248-secret-{Guid.NewGuid():N}")
            .Options);

    private static IDataProtectionProvider Protector()
        => new ServiceCollection()
            .AddDataProtection()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IDataProtectionProvider>();
}
