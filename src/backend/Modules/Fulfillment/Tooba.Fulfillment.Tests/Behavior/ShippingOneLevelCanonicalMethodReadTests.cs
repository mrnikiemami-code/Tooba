using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-NEW-METHOD-ACTIVATION-170 — درز فقط-خواندنی هویت روش‌های canonical بدون ردیف گزینهٔ
/// قدیمی. تمرکز: تفکیک روش تازه از روش نگاشت‌شده، fail-closed در دروازهٔ خاموش/والد غیرفعال و
/// تأیید «کلید نرخ» بدون ساختن هیچ ردیف قدیمی.
/// </summary>
public sealed class ShippingOneLevelCanonicalMethodReadTests
{
    private static readonly Guid PostServiceId = Guid.Parse("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid DisabledServiceId = Guid.Parse("bbbbbbbb-bbbb-7bbb-8bbb-bbbbbbbbbbbb");
    private static readonly Guid StandardOptionId = Guid.Parse("dddddddd-dddd-7ddd-8ddd-dddddddddddd");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-16T00:00:00Z");

    private static FulfillmentDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static ShippingOneLevelCanonicalMethodRead Reader(FulfillmentDbContext db, bool enabled) =>
        new(Options.Create(new ShippingOneLevelCatalogOptions { Enabled = enabled }), db);

    private static void Seed(FulfillmentDbContext db)
    {
        db.ShippingServices.AddRange(
            ShippingService.Create(PostServiceId, "post", "post", "post", "blue", 10, true, Now),
            ShippingService.Create(DisabledServiceId, "tipax", "tipax", "tipax", "orange", 20, false, Now));
        db.ShippingServiceOptions.Add(
            ShippingServiceOption.Create(StandardOptionId, PostServiceId, "standard", 20, true, Now));
        db.ShippingMethods.AddRange(
            // روش نگاشت‌شدهٔ قدیمی: ردیف گزینه وجود دارد ⇒ مسیر قدیمی صاحب آن است.
            ShippingMethod.Create(Guid.Parse("11111111-1111-7111-8111-111111111111"), "post:standard", "post", "post", "standard", 12, true, Now),
            // روش تازهٔ canonical روی سرویس فعال: هیچ ردیف گزینه‌ای ندارد.
            ShippingMethod.Create(Guid.Parse("22222222-2222-7222-8222-222222222222"), "post:economy", "post", "post", "economy", 14, true, Now),
            // روش غیرفعال تازه: نباید به مشتری پیشنهاد شود.
            ShippingMethod.Create(Guid.Parse("33333333-3333-7333-8333-333333333333"), "post:slow", "post", "post", "slow", 15, false, Now),
            // سرویس والد غیرفعال: ثبت ارائه‌دهنده پیش‌نیاز واقعی است و حدس زده نمی‌شود.
            ShippingMethod.Create(Guid.Parse("44444444-4444-7444-8444-444444444444"), "tipax:economy", "tipax", "tipax", "economy", 30, true, Now));
        db.SaveChanges();
    }

    [Fact]
    public async Task Only_active_unbound_methods_of_an_active_service_are_declared()
    {
        await using var db = CreateDb();
        Seed(db);

        var methods = await Reader(db, enabled: true).ListActiveUnboundAsync(CancellationToken.None);

        var single = Assert.Single(methods);
        Assert.Equal("post:economy", single.MethodCode);
        Assert.Equal("post", single.ServiceCode);
        Assert.Equal("economy", single.OptionCode);
        Assert.Equal("post", single.IconKey);
    }

    [Fact]
    public async Task A_disabled_gate_declares_nothing_and_never_touches_the_database()
    {
        await using var db = CreateDb();
        Seed(db);

        Assert.Empty(await Reader(db, enabled: false).ListActiveUnboundAsync(CancellationToken.None));
        Assert.False(await Reader(db, enabled: false)
            .IsCanonicalMethodBindingAsync("post", "economy", CancellationToken.None));
    }

    [Fact]
    public async Task Canonical_rate_key_is_confirmed_only_for_a_real_unbound_binding()
    {
        await using var db = CreateDb();
        Seed(db);
        var reader = Reader(db, enabled: true);

        Assert.True(await reader.IsCanonicalMethodBindingAsync("post", "economy", CancellationToken.None));
        Assert.False(await reader.IsCanonicalMethodBindingAsync("post", "invented", CancellationToken.None));
        Assert.False(await reader.IsCanonicalMethodBindingAsync("ghost", "economy", CancellationToken.None));
        Assert.False(await reader.IsCanonicalMethodBindingAsync("post", null, CancellationToken.None));
    }
}
