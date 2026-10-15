using Microsoft.Extensions.Options;
using Npgsql;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-100 — درز برچسب فروشگاه یک‌سطحی: فقط برچسب، fail-closed.
/// </summary>
public sealed class ShippingOneLevelStorefrontLabelReadTests
{
    private static readonly Guid Fa = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid En = Guid.Parse("22222222-2222-4222-8222-222222222222");

    [Fact]
    public async Task Disabled_gate_returns_no_label_and_reads_no_catalog()
    {
        var reader = new HandwrittenReader(One("post:standard", Fa, "یک‌سطحی"));
        var seam = new ShippingOneLevelStorefrontLabelRead(Options(enabled: false), reader);

        var names = await seam.ResolveDisplayNamesAsync(["post:standard"], Fa, CancellationToken.None);

        Assert.Empty(names);
        Assert.Equal(0, reader.ListCalls);
    }

    [Fact]
    public async Task Enabled_gate_returns_exact_language_translation_only()
    {
        var reader = new HandwrittenReader(
            One("post:standard", Fa, "پست استاندارد یک‌سطحی"),
            One("post:express", En, "One level express"));
        var seam = new ShippingOneLevelStorefrontLabelRead(Options(enabled: true), reader);

        var faNames = await seam.ResolveDisplayNamesAsync(["post:standard", "post:express"], Fa, CancellationToken.None);
        Assert.Equal("پست استاندارد یک‌سطحی", faNames["post:standard"]);
        Assert.False(faNames.ContainsKey("post:express"));

        var enNames = await seam.ResolveDisplayNamesAsync(["post:standard", "post:express"], En, CancellationToken.None);
        Assert.Equal("One level express", enNames["post:express"]);
        Assert.False(enNames.ContainsKey("post:standard"));
    }

    [Fact]
    public async Task Unknown_blank_and_unknown_language_never_produce_a_label()
    {
        var reader = new HandwrittenReader(One("post:standard", Fa, "پست استاندارد یک‌سطحی"));
        var seam = new ShippingOneLevelStorefrontLabelRead(Options(enabled: true), reader);

        // کد معتبر اما ناشناخته: یک خواندن کاتالوگ و بدون برچسب (هیچ نامی ساخته نمی‌شود).
        Assert.Empty(await seam.ResolveDisplayNamesAsync(["post:economic"], Fa, CancellationToken.None));
        Assert.Equal(1, reader.ListCalls);

        // کد سفید و زبان نامعلوم: پیش از هر خواندنِ دیتابیس خالی برمی‌گردند.
        Assert.Empty(await seam.ResolveDisplayNamesAsync(["  "], Fa, CancellationToken.None));
        Assert.Empty(await seam.ResolveDisplayNamesAsync(["post:standard"], Guid.Empty, CancellationToken.None));
        Assert.Equal(1, reader.ListCalls);
    }

    [Fact]
    public async Task Missing_table_fails_closed_to_legacy_label()
    {
        var seam = new ShippingOneLevelStorefrontLabelRead(
            Options(enabled: true),
            new ThrowingReader(new PostgresException("relation does not exist", "ERROR", "ERROR", PostgresErrorCodes.UndefinedTable)));

        var names = await seam.ResolveDisplayNamesAsync(["post:standard"], Fa, CancellationToken.None);

        Assert.Empty(names);
    }

    private static IOptions<ShippingOneLevelCatalogOptions> Options(bool enabled) =>
        Microsoft.Extensions.Options.Options.Create(new ShippingOneLevelCatalogOptions { Enabled = enabled });

    private static ShippingOneLevelMethodSnapshot One(string code, Guid languageId, string name) =>
        new(
            Guid.NewGuid(),
            code,
            "post",
            code.Split(':')[0],
            code.Contains(':') ? code.Split(':')[1] : null,
            IsActive: true,
            SortOrder: 0,
            [new ShippingOneLevelMethodTranslationSnapshot(languageId, name, null)]);

    /// <summary>PostgresException واقعی بدون اتصال ساخته می‌شود؛ فقط SqlState لازم است.</summary>
    private sealed class ThrowingReader(Exception exception) : IShippingOneLevelCatalogReader
    {
        public Task<IReadOnlyList<ShippingOneLevelMethodSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            throw exception;

        public Task<IReadOnlyList<ShippingOneLevelMethodSnapshot>> ListActiveAsync(CancellationToken cancellationToken) =>
            throw exception;

        public Task<ShippingOneLevelMethodSnapshot?> GetByCodeAsync(string methodCode, CancellationToken cancellationToken) =>
            throw exception;
    }

    private sealed class HandwrittenReader(params ShippingOneLevelMethodSnapshot[] methods) : IShippingOneLevelCatalogReader
    {
        public int ListCalls { get; private set; }

        public Task<IReadOnlyList<ShippingOneLevelMethodSnapshot>> ListAsync(CancellationToken cancellationToken)
        {
            ListCalls++;
            return Task.FromResult<IReadOnlyList<ShippingOneLevelMethodSnapshot>>(methods);
        }

        public Task<IReadOnlyList<ShippingOneLevelMethodSnapshot>> ListActiveAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingOneLevelMethodSnapshot>>(methods.Where(m => m.IsActive).ToArray());

        public Task<ShippingOneLevelMethodSnapshot?> GetByCodeAsync(string methodCode, CancellationToken cancellationToken) =>
            Task.FromResult(methods.FirstOrDefault(m => m.MethodCode == methodCode));
    }
}
