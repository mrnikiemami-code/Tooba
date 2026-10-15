using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-096 — نگهبان «دو نویسنده» روی <c>ShippingServiceDirectory.UpdateAsync</c>.
/// </summary>
/// <remarks>
/// پیش از این، یک ویرایش سرویس (حتی ویرایش جزئیات بی‌ربط) می‌توانست فعال‌بودن گزینه‌ای را که مرجع
/// فعال‌سازی فروشگاه است صفر کند، هویت/UUID گزینه را بازنویسی کند، یا با بدنهٔ بدون گزینه همهٔ
/// گزینه‌ها را حذف کند. این تست‌ها همان سه سناریو را قفل می‌کنند.
/// </remarks>
public sealed class ShippingServiceDirectoryDualWriterTests
{
    private static readonly Guid PostServiceId = Guid.Parse("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid ExpressOptionId = Guid.Parse("cccccccc-cccc-7ccc-8ccc-cccccccccccc");
    private static readonly Guid StandardOptionId = Guid.Parse("dddddddd-dddd-7ddd-8ddd-dddddddddddd");
    private static readonly Guid Fa = Guid.Parse("01a06a53-4139-7000-bb9b-3078a0d2e5c4");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-15T00:00:00Z");

    private static FulfillmentDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static ShippingServiceDirectory Directory(FulfillmentDbContext db) =>
        new(db, new FixedClock(), new UuidV7IdGenerator(), new AcceptAllLanguageGate());

    private static void Seed(FulfillmentDbContext db)
    {
        db.ShippingServices.Add(ShippingService.Create(PostServiceId, "post", "post", "post", "blue", 10, true, Now));
        db.ShippingServiceOptions.AddRange(
            ShippingServiceOption.Create(ExpressOptionId, PostServiceId, "express", 10, false, Now),
            ShippingServiceOption.Create(StandardOptionId, PostServiceId, "standard", 20, true, Now));
        db.SaveChanges();
    }

    private static ShippingServiceWriteModel Payload(
        string code = "post",
        string? optionId = null,
        bool? optionActive = null,
        bool includeOptions = true,
        string optionCode = "express",
        bool includeSibling = true)
    {
        var options = new List<ShippingServiceOptionWriteModel>();
        if (includeOptions)
        {
            options.Add(new(
                optionId is null ? null : Guid.Parse(optionId),
                optionCode,
                optionActive ?? false,
                10,
                [new ShippingServiceOptionTranslationWriteModel(Fa, "پیشتاز")]));
            if (includeSibling)
            {
                options.Add(new(
                    null, "standard", true, 20,
                    [new ShippingServiceOptionTranslationWriteModel(Fa, "معمولی")]));
            }
        }

        return new ShippingServiceWriteModel(
            code, "post", "post", "blue", true, 10,
            [new ShippingServiceTranslationWriteModel(Fa, "پست", null)],
            options);
    }

    [Fact]
    public async Task A_payload_without_options_leaves_existing_options_and_activation_untouched()
    {
        await using var db = CreateDb();
        Seed(db);
        // فعال‌سازی مرجع (همان کاری که TB-096 انجام می‌دهد) و سپس ویرایش بی‌ربط جزئیات سرویس.
        (await db.ShippingServiceOptions.SingleAsync(x => x.ShippingServiceOptionId == ExpressOptionId))
            .SetActive(true, Now);
        await db.SaveChangesAsync();

        await Directory(db).UpdateAsync(PostServiceId, Payload(code: "post", includeOptions: false), CancellationToken.None);

        Assert.Equal(2, await db.ShippingServiceOptions.AsNoTracking().CountAsync());
        Assert.True((await db.ShippingServiceOptions.AsNoTracking()
            .SingleAsync(x => x.ShippingServiceOptionId == ExpressOptionId)).IsActive);
    }

    [Fact]
    public async Task A_code_matched_carry_over_updates_details_but_never_resets_activation()
    {
        await using var db = CreateDb();
        Seed(db);
        (await db.ShippingServiceOptions.SingleAsync(x => x.ShippingServiceOptionId == ExpressOptionId))
            .SetActive(true, Now);
        await db.SaveChangesAsync();

        // بدون شناسه و با isActive=false (کلاینت کهنه): فقط انتقال ساده، نه صفر کردن فعال‌بودن.
        await Directory(db).UpdateAsync(
            PostServiceId, Payload(optionId: null, optionActive: false), CancellationToken.None);

        var express = await db.ShippingServiceOptions.AsNoTracking()
            .SingleAsync(x => x.ShippingServiceOptionId == ExpressOptionId);
        Assert.True(express.IsActive);
        // هویت ردیف موجود حفظ شده و ردیف تکراری ساخته نشده است.
        Assert.Equal(2, await db.ShippingServiceOptions.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task An_explicit_option_identity_still_applies_the_requested_activation()
    {
        await using var db = CreateDb();
        Seed(db);
        (await db.ShippingServiceOptions.SingleAsync(x => x.ShippingServiceOptionId == ExpressOptionId))
            .SetActive(true, Now);
        await db.SaveChangesAsync();

        // شناسهٔ بازگردانده‌شده ⇒ ویرایش صریح همان گزینه و اعمال فعال‌بودن خواسته‌شده.
        await Directory(db).UpdateAsync(
            PostServiceId, Payload(optionId: ExpressOptionId.ToString(), optionActive: false), CancellationToken.None);

        Assert.False((await db.ShippingServiceOptions.AsNoTracking()
            .SingleAsync(x => x.ShippingServiceOptionId == ExpressOptionId)).IsActive);
        // خواهر دست‌نخورده.
        Assert.True((await db.ShippingServiceOptions.AsNoTracking()
            .SingleAsync(x => x.ShippingServiceOptionId == StandardOptionId)).IsActive);
    }

    [Fact]
    public async Task A_non_empty_payload_still_removes_a_genuinely_absent_option()
    {
        await using var db = CreateDb();
        Seed(db);

        await Directory(db).UpdateAsync(
            PostServiceId, Payload(includeOptions: true, includeSibling: false), CancellationToken.None);

        // گزینهٔ استاندارد در بدنه نبود ⇒ حذف واقعی همچنان کار می‌کند (بدون گزینه‌زدایی ناخواسته).
        Assert.Null(await db.ShippingServiceOptions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ShippingServiceOptionId == StandardOptionId));
        Assert.NotNull(await db.ShippingServiceOptions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ShippingServiceOptionId == ExpressOptionId));
    }

    private sealed class AcceptAllLanguageGate : IShippingServiceLanguageGate
    {
        public Task EnsureKnownAsync(IReadOnlyList<Guid> languageIds, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<ShippingServiceSeedLanguage>> ListForSeedAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingServiceSeedLanguage>>([]);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = DateTimeOffset.Parse("2026-10-15T12:00:00Z");
    }
}
