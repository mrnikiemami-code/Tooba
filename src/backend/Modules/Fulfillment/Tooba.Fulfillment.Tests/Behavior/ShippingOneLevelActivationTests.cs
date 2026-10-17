using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Shipping.Commands;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-096 — مرجع باریک و تراکنشی فعال‌سازی روش یک‌سطحی گزینه‌دار.
/// تمرکز: عدم تغییر خواهرها، حفظ UUID گزینه/ترجمه، هم‌راستایی ردیف قدیمی و یک‌سطحی، fail-closed در
/// دروازهٔ خاموش/والد غیرفعال/روش بدون گزینه/پیوند کهنه، و نگهبان «دو نویسنده» روی UpdateAsync قدیمی.
/// </summary>
public sealed class ShippingOneLevelActivationTests
{
    private static readonly Guid StandardMethodId = Guid.Parse("11111111-1111-7111-8111-111111111111");
    private static readonly Guid ExpressMethodId = Guid.Parse("22222222-2222-7222-8222-222222222222");
    private static readonly Guid OptionlessMethodId = Guid.Parse("33333333-3333-7333-8333-333333333333");
    private static readonly Guid UnboundMethodId = Guid.Parse("44444444-4444-7444-8444-444444444444");
    private static readonly Guid PostServiceId = Guid.Parse("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid StoreServiceId = Guid.Parse("bbbbbbbb-bbbb-7bbb-8bbb-bbbbbbbbbbbb");
    private static readonly Guid ExpressOptionId = Guid.Parse("cccccccc-cccc-7ccc-8ccc-cccccccccccc");
    private static readonly Guid StandardOptionId = Guid.Parse("dddddddd-dddd-7ddd-8ddd-dddddddddddd");
    private static readonly Guid Fa = Guid.Parse("01a06a53-4139-7000-bb9b-3078a0d2e5c4");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-15T00:00:00Z");

    private static FulfillmentDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static ShippingOneLevelCatalogActivation Activation(FulfillmentDbContext db, bool enabled) =>
        new(Options.Create(new ShippingOneLevelCatalogOptions { Enabled = enabled }), db, new FixedClock());

    private static void Seed(FulfillmentDbContext db)
    {
        db.ShippingServices.AddRange(
            ShippingService.Create(PostServiceId, "post", "post", "post", "blue", 10, true, Now),
            ShippingService.Create(StoreServiceId, "store_courier", "store_courier", "bike", "violet", 40, true, Now));
        db.ShippingServiceOptions.AddRange(
            ShippingServiceOption.Create(ExpressOptionId, PostServiceId, "express", 10, false, Now),
            ShippingServiceOption.Create(StandardOptionId, PostServiceId, "standard", 20, true, Now));
        db.ShippingServiceOptionTranslations.AddRange(
            ShippingServiceOptionTranslation.Create(Guid.Parse("e0000000-0000-7000-8000-000000000001"), ExpressOptionId, Fa, "پیشتاز"),
            ShippingServiceOptionTranslation.Create(Guid.Parse("e0000000-0000-7000-8000-000000000002"), StandardOptionId, Fa, "معمولی"));
        db.ShippingMethods.AddRange(
            ShippingMethod.Create(StandardMethodId, "post:standard", "post", "post", "standard", 12, true, Now),
            ShippingMethod.Create(ExpressMethodId, "post:express", "post", "post", "express", 11, false, Now),
            ShippingMethod.Create(OptionlessMethodId, "store_courier", "store_courier", "store_courier", null, 40, true, Now),
            // پیوند کهنه: سرویس والد «in_person» در کاتالوگ قدیمی وجود ندارد.
            ShippingMethod.Create(UnboundMethodId, "in_person", "in_person", "in_person", null, 30, true, Now));
        db.SaveChanges();
    }

    private static async Task<bool> OptionActiveAsync(FulfillmentDbContext db, Guid optionId) =>
        (await db.ShippingServiceOptions.AsNoTracking().SingleAsync(x => x.ShippingServiceOptionId == optionId)).IsActive;

    private static async Task<bool> MethodActiveAsync(FulfillmentDbContext db, Guid methodId) =>
        (await db.ShippingMethods.AsNoTracking().SingleAsync(x => x.ShippingMethodId == methodId)).IsActive;

    [Fact]
    public async Task Activating_an_option_backed_method_aligns_the_legacy_option_and_the_projection_in_one_transaction()
    {
        await using var db = CreateDb();
        Seed(db);

        var result = await Activation(db, enabled: true)
            .SetActivationAsync("post:express", true, CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodActivationStatus.Applied, result.Status);
        Assert.Equal("post:express", result.MethodCode);
        // مرجع عملیاتی فروشگاه: ستون قدیمی گزینه.
        Assert.True(await OptionActiveAsync(db, ExpressOptionId));
        // فرافکنی ادمین، هم‌راستا در همان ذخیره.
        Assert.True(await MethodActiveAsync(db, ExpressMethodId));
    }

    [Fact]
    public async Task Deactivating_reaches_the_legacy_option_and_never_touches_a_sibling_or_its_identity()
    {
        await using var db = CreateDb();
        Seed(db);
        var expressIdsBefore = await db.ShippingServiceOptionTranslations.AsNoTracking()
            .Where(x => x.ShippingServiceOptionId == ExpressOptionId)
            .Select(x => x.TranslationId).ToListAsync();

        var result = await Activation(db, enabled: true)
            .SetActivationAsync("post:standard", false, CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodActivationStatus.Applied, result.Status);
        Assert.False(await OptionActiveAsync(db, StandardOptionId));
        Assert.False(await MethodActiveAsync(db, StandardMethodId));

        // خواهر دست‌نخورده: فعال‌بودن، کد، ترتیب و هویت ردیف و ترجمه‌ها.
        var sibling = await db.ShippingServiceOptions.AsNoTracking().SingleAsync(x => x.ShippingServiceOptionId == ExpressOptionId);
        Assert.False(sibling.IsActive);
        Assert.Equal("express", sibling.Code);
        Assert.Equal(10, sibling.SortOrder);
        var expressIdsAfter = await db.ShippingServiceOptionTranslations.AsNoTracking()
            .Where(x => x.ShippingServiceOptionId == ExpressOptionId)
            .Select(x => x.TranslationId).ToListAsync();
        Assert.Equal(expressIdsBefore, expressIdsAfter);
        // گزینهٔ هدف هم هویت و ترتیبش تغییر نکرده است.
        var target = await db.ShippingServiceOptions.AsNoTracking().SingleAsync(x => x.ShippingServiceOptionId == StandardOptionId);
        Assert.Equal("standard", target.Code);
        Assert.Equal(20, target.SortOrder);
        // روش بدون گزینه در همان سرویس دیگر هرگز لمس نمی‌شود (اینجا والدش جداست).
        Assert.True(await MethodActiveAsync(db, OptionlessMethodId));
    }

    [Fact]
    public async Task A_repeated_identical_request_is_an_idempotent_no_op()
    {
        await using var db = CreateDb();
        Seed(db);
        var first = await Activation(db, enabled: true).SetActivationAsync("post:express", true, CancellationToken.None);
        Assert.Equal(ShippingOneLevelMethodActivationStatus.Applied, first.Status);

        var second = await Activation(db, enabled: true).SetActivationAsync("post:express", true, CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodActivationStatus.NoChange, second.Status);
        Assert.True(second.EffectiveIsActive);
        Assert.True(await OptionActiveAsync(db, ExpressOptionId));
    }

    [Fact]
    public async Task An_optionless_method_is_refused_and_nothing_is_written()
    {
        await using var db = CreateDb();
        Seed(db);

        var result = await Activation(db, enabled: true)
            .SetActivationAsync("store_courier", false, CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodActivationStatus.OptionlessRequiresServiceDecision, result.Status);
        // وضعیت سرویس بدون گزینه هرگز حدس زده نمی‌شود.
        Assert.True(await MethodActiveAsync(db, OptionlessMethodId));
        Assert.True((await db.ShippingServices.AsNoTracking().SingleAsync(x => x.ShippingServiceId == StoreServiceId)).IsActive);
    }

    [Fact]
    public async Task An_inactive_legacy_parent_is_refused_and_is_never_activated_automatically()
    {
        await using var db = CreateDb();
        Seed(db);
        var service = await db.ShippingServices.SingleAsync(x => x.ShippingServiceId == PostServiceId);
        service.SetActive(false, Now);
        await db.SaveChangesAsync();

        var result = await Activation(db, enabled: true)
            .SetActivationAsync("post:express", true, CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodActivationStatus.LegacyParentInactive, result.Status);
        Assert.False((await db.ShippingServices.AsNoTracking().SingleAsync(x => x.ShippingServiceId == PostServiceId)).IsActive);
        Assert.False(await OptionActiveAsync(db, ExpressOptionId));
        Assert.False(await MethodActiveAsync(db, ExpressMethodId));
    }

    [Fact]
    public async Task A_stale_binding_or_unknown_method_is_refused_without_writing()
    {
        await using var db = CreateDb();
        Seed(db);

        var missingService = await Activation(db, enabled: true).SetActivationAsync("in_person", true, CancellationToken.None);
        Assert.Equal(ShippingOneLevelMethodActivationStatus.LegacyBindingMissing, missingService.Status);

        var unknown = await Activation(db, enabled: true).SetActivationAsync("snapp_courier", true, CancellationToken.None);
        Assert.Equal(ShippingOneLevelMethodActivationStatus.MethodNotFound, unknown.Status);

        var malformed = await Activation(db, enabled: true).SetActivationAsync("post:standard!", true, CancellationToken.None);
        Assert.Equal(ShippingOneLevelMethodActivationStatus.InvalidMethodCode, malformed.Status);

        Assert.True(await OptionActiveAsync(db, StandardOptionId));
        Assert.False(await OptionActiveAsync(db, ExpressOptionId));
    }

    [Fact]
    public async Task A_new_canonical_method_without_a_legacy_option_row_is_activated_as_admin_projection_only()
    {
        await using var db = CreateDb();
        Seed(db);
        db.ShippingServiceOptions.Remove(await db.ShippingServiceOptions.SingleAsync(x => x.ShippingServiceOptionId == ExpressOptionId));
        await db.SaveChangesAsync();
        var optionCountBefore = await db.ShippingServiceOptions.CountAsync();

        var result = await Activation(db, enabled: true).SetActivationAsync("post:express", true, CancellationToken.None);

        // TB-170: روش تازهٔ canonical سرویس والد موجود دارد اما هیچ ردیف گزینهٔ قدیمی ندارد.
        Assert.Equal(ShippingOneLevelMethodActivationStatus.ActivatedCanonicalOnly, result.Status);
        Assert.True(await MethodActiveAsync(db, ExpressMethodId));
        // هیچ ردیف گزینهٔ قدیمی جعلی فقط برای عبور از نگهبان ساخته نمی‌شود.
        Assert.Equal(optionCountBefore, await db.ShippingServiceOptions.CountAsync());
    }

    [Fact]
    public async Task Canonical_only_activation_is_idempotent_and_deactivates_cleanly()
    {
        await using var db = CreateDb();
        Seed(db);
        db.ShippingServiceOptions.Remove(await db.ShippingServiceOptions.SingleAsync(x => x.ShippingServiceOptionId == ExpressOptionId));
        await db.SaveChangesAsync();
        var activation = Activation(db, enabled: true);

        var first = await activation.SetActivationAsync("post:express", true, CancellationToken.None);
        Assert.Equal(ShippingOneLevelMethodActivationStatus.ActivatedCanonicalOnly, first.Status);

        var second = await activation.SetActivationAsync("post:express", true, CancellationToken.None);
        Assert.Equal(ShippingOneLevelMethodActivationStatus.NoChange, second.Status);

        var off = await activation.SetActivationAsync("post:express", false, CancellationToken.None);
        Assert.Equal(ShippingOneLevelMethodActivationStatus.ActivatedCanonicalOnly, off.Status);
        Assert.False(await MethodActiveAsync(db, ExpressMethodId));
        // خواهر قدیمی دست‌نخورده می‌ماند.
        Assert.True(await OptionActiveAsync(db, StandardOptionId));
        Assert.True(await MethodActiveAsync(db, StandardMethodId));
    }

    [Fact]
    public async Task A_disabled_gate_returns_gate_disabled_and_touches_nothing()
    {
        await using var db = CreateDb();
        Seed(db);

        var result = await Activation(db, enabled: false).SetActivationAsync("post:express", true, CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodActivationStatus.GateDisabled, result.Status);
        Assert.False(await OptionActiveAsync(db, ExpressOptionId));
        Assert.False(await MethodActiveAsync(db, ExpressMethodId));
    }

    [Fact]
    public async Task The_handler_reports_changed_truthfully_and_maps_every_failure_to_a_stable_code()
    {
        await using var db = CreateDb();
        Seed(db);

        var applied = await new SetOneLevelShippingMethodActivationHandler(Activation(db, enabled: true))
            .Handle(new SetOneLevelShippingMethodActivationCommand("post:express", true), CancellationToken.None);
        Assert.True(applied.IsSuccess);
        Assert.True(applied.Value!.Changed);
        Assert.True(applied.Value!.IsActive);
        Assert.Equal("post:express", applied.Value!.MethodCode);

        var noop = await new SetOneLevelShippingMethodActivationHandler(Activation(db, enabled: true))
            .Handle(new SetOneLevelShippingMethodActivationCommand("post:express", true), CancellationToken.None);
        Assert.True(noop.IsSuccess);
        Assert.False(noop.Value!.Changed);

        var optionless = await new SetOneLevelShippingMethodActivationHandler(Activation(db, enabled: true))
            .Handle(new SetOneLevelShippingMethodActivationCommand("store_courier", false), CancellationToken.None);
        AssertFailure(optionless, FulfillmentErrorCodes.ShippingMethodUnsupported);

        var unknown = await new SetOneLevelShippingMethodActivationHandler(Activation(db, enabled: true))
            .Handle(new SetOneLevelShippingMethodActivationCommand("tipax:express", true), CancellationToken.None);
        AssertFailure(unknown, FulfillmentErrorCodes.ShippingMethodNotFound);

        var malformed = await new SetOneLevelShippingMethodActivationHandler(Activation(db, enabled: true))
            .Handle(new SetOneLevelShippingMethodActivationCommand("post:standard!", true), CancellationToken.None);
        AssertFailure(malformed, FulfillmentErrorCodes.ShippingMethodCodeInvalid);

        var gateOff = await new SetOneLevelShippingMethodActivationHandler(Activation(db, enabled: false))
            .Handle(new SetOneLevelShippingMethodActivationCommand("post:express", true), CancellationToken.None);
        AssertFailure(gateOff, FulfillmentErrorCodes.Rejected);
    }

    [Fact]
    public async Task Historic_method_codes_and_option_codes_are_never_rewritten_by_activation()
    {
        await using var db = CreateDb();
        Seed(db);

        await Activation(db, enabled: true).SetActivationAsync("post:express", true, CancellationToken.None);

        Assert.Equal(["in_person", "post:express", "post:standard", "store_courier"],
            await db.ShippingMethods.AsNoTracking().OrderBy(x => x.MethodCode).Select(x => x.MethodCode).ToListAsync());
        var express = await db.ShippingMethods.AsNoTracking().SingleAsync(x => x.ShippingMethodId == ExpressMethodId);
        Assert.Equal("post", express.LegacyServiceCode);
        Assert.Equal("express", express.LegacyOptionCode);
        Assert.Equal(11, express.SortOrder);
    }

    private static void AssertFailure(Result<OneLevelShippingMethodActivationDto> result, string code)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Errors[0].Code);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = DateTimeOffset.Parse("2026-10-15T12:00:00Z");
    }
}

