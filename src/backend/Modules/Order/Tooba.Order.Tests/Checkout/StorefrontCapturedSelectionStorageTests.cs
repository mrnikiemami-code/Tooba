using Microsoft.EntityFrameworkCore;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Shipping.CapturedPreview;
using Tooba.Order.Infrastructure.Storefront;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232 (و ۲۳۲-R1) — اثبات رابطه‌ای نوشتن اتمیک انتخاب
/// capture‌شده روی همان پایگاه‌دادهٔ دورانداختنیِ متعلق به این تسک (127.0.0.1:15433/order_tb231/tb231).
/// </summary>
/// <remarks>
/// <para>
/// همهٔ حقایق با <see cref="StorefrontShippingDraftStore"/> واقعی اجرا می‌شوند. قصد نوشتن همان چیزی است
/// که فراخوان واقعی می‌سازد: یک گراف <b>تازه و detached</b> از
/// <see cref="IStorefrontShippingDraftStore.GetForCapturedSelectionAsync"/>، نه ریشهٔ کهنهٔ tracked.
/// </para>
/// <para>
/// این اثبات هیچ سفارش/رزرو/پرداختی نمی‌سازد و هیچ ردیفی بیرون از سبدهای همین تست نمی‌نویسد.
/// </para>
/// </remarks>
[Collection(OrderEvidenceRelationalCollection.Name)]
public sealed class StorefrontCapturedSelectionStorageTests
{
    private static readonly Guid CartId = Guid.Parse("01a17e00-0000-7000-9000-0000000000c1");
    private static readonly Guid OtherCartId = Guid.Parse("01a17e00-0000-7000-9000-0000000000c2");
    private static readonly Guid SellerPartyId = Guid.Parse("01a17e00-0000-7000-9000-0000000000aa");
    private static readonly Guid LineA = Guid.Parse("01a17e00-0000-7000-9000-0000000000a1");
    private static readonly Guid LineB = Guid.Parse("01a17e00-0000-7000-9000-0000000000b2");
    private static readonly Guid Snapshot = Guid.Parse("01a17e00-0000-7000-9000-0000000000e1");
    private static readonly Guid DraftSource = Guid.Parse("01a17e00-0000-7000-9000-0000000000d1");
    private static readonly Guid AreaGroupA = Guid.Parse("01a17e00-0000-7000-9000-0000000000f1");
    private static readonly Guid RateRowA = Guid.Parse("01a17e00-0000-7000-9000-0000000000f2");
    private static readonly Guid AreaGroupB = Guid.Parse("01a17e00-0000-7000-9000-0000000000f3");
    private static readonly Guid RateRowB = Guid.Parse("01a17e00-0000-7000-9000-0000000000f4");
    private static readonly Guid ShippingTypeId = Guid.Parse("01a1785e-01f3-7000-b50d-d6372e756f89");
    private static readonly Guid CountryId = Guid.Parse("01a17e00-0000-7000-9000-0000000000c9");
    private static readonly Guid ProvinceId = Guid.Parse("01a17e00-0000-7000-9000-0000000000ca");
    private static readonly Guid CityId = Guid.Parse("01a17e00-0000-7000-9000-0000000000cb");
    private static readonly DateOnly ReceivingDate = new(2026, 10, 29);
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task First_selection_persists_revision_destination_and_compact_evidence_atomically()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();

        await using (var write = CreateContext(target.ConnectionString))
        {
            await write.Database.MigrateAsync();
            await ResetAsync(write);

            var draft = NewDraft();
            var groups = Groups(AreaGroupA, RateRowA, freight: 15_000m);
            draft.AddShippingGroups(groups, Now);
            draft.SetDestinationIdentity(CountryId, ProvinceId, CityId, null, Now);

            await new StorefrontShippingDraftStore(write)
                .ReplaceCapturedSelectionAsync(draft, groups, isNew: true, expectedSelectionRevision: 0, CancellationToken.None);
        }

        await using var read = CreateContext(target.ConnectionString);
        var stored = await read.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);

        Assert.Equal(1, stored.SelectionRevision);
        Assert.Equal(CountryId, stored.DestinationCountryId);
        Assert.Equal(ProvinceId, stored.DestinationAdministrativeAreaId);
        Assert.Equal(CityId, stored.DestinationCityId);
        Assert.Null(stored.DestinationServiceAreaId);

        var group = Assert.Single(stored.ShippingGroups);
        Assert.Equal([LineA], group.Lines.Select(line => line.CartLineId).ToArray());
        var evidence = group.AcceptedEvidence;
        Assert.NotNull(evidence);
        Assert.Equal(Snapshot, evidence!.SnapshotId);
        Assert.Equal(DraftSource, evidence.DraftId);
        Assert.Equal(3, evidence.SourceRevision);
        Assert.Equal(1, evidence.FormatVersion);
        Assert.Equal(1, evidence.SelectionVersion);
        Assert.Equal(ShippingGroupOwnerKind.Store, evidence.Owner.Kind);
        Assert.Equal("store-alpha", evidence.Owner.TenantKey);
        Assert.Equal(AreaGroupA, evidence.AreaGroupId);
        Assert.Equal(RateRowA, evidence.RateRowId);
        Assert.Equal(15_000m, evidence.FreightAmount);
        Assert.Equal(2_000m, evidence.PackagingAmount);
        Assert.Equal(17_000m, evidence.PayableAtCheckoutAmount);
        Assert.Equal(0m, evidence.CarrierCollectAmount);
        Assert.Equal("IRR", evidence.Currency);
        Assert.Equal(ReceivingDate, evidence.ReceivingDate);
        Assert.Equal(new TimeOnly(9, 0).Ticks, evidence.ReceivingOpenTicks);
        Assert.Equal(new TimeOnly(17, 0).Ticks, evidence.ReceivingCloseTicks);
        Assert.Equal("Asia/Tehran", evidence.ReceivingTimeZone);

        var raw = await read.Database
            .SqlQuery<string>($"SELECT selection_revision::text AS \"Value\" FROM \"order\".cart_shipping_drafts WHERE cart_id = {CartId}")
            .SingleAsync();
        Assert.Equal("1", raw);
    }

    /// <summary>
    /// دو فروشندهٔ متفاوت با همان Binding ذخیره‌شده هر دو پابرجا می‌مانند: گراف کامل ذخیره‌شده دو گروه و
    /// دو شاهد مستقل (ردیف/ساعت خودشان) دارد، نه یک شاهد بازنویسی‌شده.
    /// </summary>
    [Fact]
    public async Task Two_sellers_with_one_binding_persist_two_independent_evidences()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();

        await using (var write = CreateContext(target.ConnectionString))
        {
            await write.Database.MigrateAsync();
            await ResetAsync(write);

            var draft = NewDraft();
            var groups = TwoSellerGroups().ToArray();
            draft.AddShippingGroups(groups, Now);
            draft.SetDestinationIdentity(CountryId, ProvinceId, CityId, null, Now);

            await new StorefrontShippingDraftStore(write)
                .ReplaceCapturedSelectionAsync(draft, groups, isNew: true, expectedSelectionRevision: 0, CancellationToken.None);
        }

        await using var read = CreateContext(target.ConnectionString);
        var stored = await read.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);

        Assert.Equal(1, stored.SelectionRevision);
        Assert.Equal(2, stored.ShippingGroups.Count);

        var first = stored.ShippingGroups.Single(group => group.SellerPartyId == SellerPartyId);
        var second = stored.ShippingGroups.Single(group => group.SellerPartyId == OtherSellerPartyId);
        Assert.Equal(AreaGroupA, first.AcceptedEvidence!.AreaGroupId);
        Assert.Equal(RateRowA, first.AcceptedEvidence!.RateRowId);
        Assert.Equal(new TimeOnly(9, 0).Ticks, first.AcceptedEvidence!.ReceivingOpenTicks);
        Assert.Equal(AreaGroupB, second.AcceptedEvidence!.AreaGroupId);
        Assert.Equal(RateRowB, second.AcceptedEvidence!.RateRowId);
        Assert.Equal(new TimeOnly(10, 0).Ticks, second.AcceptedEvidence!.ReceivingOpenTicks);
        Assert.NotEqual(first.AcceptedEvidence!.RateRowId, second.AcceptedEvidence!.RateRowId);
        Assert.Equal(34_000m, stored.ShippingAmount);
    }

    [Fact]
    public async Task Zero_expected_revision_on_an_existing_selection_is_a_conflict_and_writes_nothing()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();

        await using var db = CreateContext(target.ConnectionString);
        await db.Database.MigrateAsync();
        await ResetAsync(db);
        await SeedFirstSelectionAsync(db);

        var store = new StorefrontShippingDraftStore(db);
        var intent = await store.GetForCapturedSelectionAsync(CartId, CancellationToken.None);
        Assert.NotNull(intent);
        ApplyIntent(intent!, "قصد دوم", Now.AddMinutes(1));
        var replacement = Groups(AreaGroupB, RateRowB, freight: 90_000m);

        var conflict = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            store.ReplaceCapturedSelectionAsync(intent!, replacement, isNew: false, expectedSelectionRevision: 0, CancellationToken.None));

        Assert.Equal(StorefrontShippingSelectionConflict.Stale, conflict.Code);

        await using var verify = CreateContext(target.ConnectionString);
        var stored = await verify.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Equal(1, stored.SelectionRevision);
        Assert.Null(stored.CustomerNote);
        var group = Assert.Single(stored.ShippingGroups);
        Assert.Equal(AreaGroupA, group.AcceptedEvidence!.AreaGroupId);
        Assert.Equal([LineA], group.Lines.Select(line => line.CartLineId).ToArray());
    }

    [Fact]
    public async Task Second_lawful_selection_replaces_every_group_and_increments_the_revision()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();

        await using var db = CreateContext(target.ConnectionString);
        await db.Database.MigrateAsync();
        await ResetAsync(db);
        await SeedFirstSelectionAsync(db);

        var store = new StorefrontShippingDraftStore(db);
        var intent = await store.GetForCapturedSelectionAsync(CartId, CancellationToken.None);
        ApplyIntent(intent!, "قصد دوم", Now.AddMinutes(2));
        intent!.SetDestinationIdentity(CountryId, ProvinceId, CityId, null, Now.AddMinutes(2));
        var replacement = Groups(AreaGroupB, RateRowB, freight: 90_000m, lineIds: [LineB]);

        var persisted = await store.ReplaceCapturedSelectionAsync(
            intent, replacement, isNew: false, expectedSelectionRevision: 1, CancellationToken.None);

        Assert.Equal(2, persisted.SelectionRevision);

        await using var verify = CreateContext(target.ConnectionString);
        var stored = await verify.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);

        Assert.Equal(2, stored.SelectionRevision);
        Assert.Equal("قصد دوم", stored.CustomerNote);
        var group = Assert.Single(stored.ShippingGroups);
        Assert.Equal(AreaGroupB, group.AcceptedEvidence!.AreaGroupId);
        Assert.Equal(RateRowB, group.AcceptedEvidence!.RateRowId);
        Assert.Equal(90_000m, group.AcceptedEvidence!.FreightAmount);
        Assert.Equal(92_000m, group.AcceptedEvidence!.PayableAtCheckoutAmount);
        Assert.Equal([LineB], group.Lines.Select(line => line.CartLineId).ToArray());
        Assert.Equal(92_000m, stored.ShippingAmount);
    }

    [Fact]
    public async Task A_new_selection_with_a_nonzero_expected_revision_is_refused_before_any_insert()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();

        await using var db = CreateContext(target.ConnectionString);
        await db.Database.MigrateAsync();
        await ResetAsync(db);

        var draft = NewDraft();
        var groups = Groups(AreaGroupA, RateRowA, freight: 15_000m);
        draft.AddShippingGroups(groups, Now);

        var conflict = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            new StorefrontShippingDraftStore(db)
                .ReplaceCapturedSelectionAsync(draft, groups, isNew: true, expectedSelectionRevision: 1, CancellationToken.None));

        Assert.Equal(StorefrontShippingSelectionConflict.Stale, conflict.Code);

        await using var verify = CreateContext(target.ConnectionString);
        Assert.False(await verify.ShippingDrafts.AnyAsync(x => x.CartId == CartId));
    }

    /// <summary>
    /// context کهنه: ریشهٔ tracked قبلی نسخهٔ ۱ را دارد. خواندن مخصوص نوشتن باید حقیقت ذخیره‌شده (۲) را
    /// بیاورد و همان توکن جاری باید موفق شود — CAS از identity map گرفته نمی‌شود.
    /// </summary>
    [Fact]
    public async Task Exact_current_token_succeeds_through_an_actually_stale_context()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();

        await using var stale = CreateContext(target.ConnectionString);
        await stale.Database.MigrateAsync();
        await ResetAsync(stale);
        await SeedFirstSelectionAsync(stale);

        var staleRoot = await LoadAsync(stale);
        Assert.Equal(1, staleRoot.SelectionRevision);

        await using (var other = CreateContext(target.ConnectionString))
        {
            var otherStore = new StorefrontShippingDraftStore(other);
            var otherIntent = await otherStore.GetForCapturedSelectionAsync(CartId, CancellationToken.None);
            ApplyIntent(otherIntent!, "نوشتن موازی", Now.AddMinutes(5));
            await otherStore.ReplaceCapturedSelectionAsync(
                otherIntent!,
                Groups(AreaGroupB, RateRowB, freight: 90_000m, lineIds: [LineB]),
                isNew: false,
                expectedSelectionRevision: 1,
                CancellationToken.None);
        }

        // همان context کهنه اکنون باید نسخهٔ ذخیره‌شدهٔ ۲ را ببیند (نه مقدار کهنهٔ ۱).
        var store = new StorefrontShippingDraftStore(stale);
        var intent = await store.GetForCapturedSelectionAsync(CartId, CancellationToken.None);
        Assert.NotNull(intent);
        Assert.Equal(2, intent!.SelectionRevision);
        Assert.Equal("نوشتن موازی", intent.CustomerNote);

        ApplyIntent(intent, "نوشتن سوم", Now.AddMinutes(6));
        var persisted = await store.ReplaceCapturedSelectionAsync(
            intent,
            Groups(AreaGroupB, RateRowB, freight: 90_000m, lineIds: [LineB]),
            isNew: false,
            expectedSelectionRevision: 2,
            CancellationToken.None);

        Assert.Equal(3, persisted.SelectionRevision);

        await using var verify = CreateContext(target.ConnectionString);
        var stored = await verify.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Equal(3, stored.SelectionRevision);
        Assert.Equal("نوشتن سوم", stored.CustomerNote);
    }

    /// <summary>
    /// توکن کهنه (۱) بعد از آنکه نویسندهٔ دیگری ردیف را به ۲ رساند: تضاد، بدون هیچ تغییری. این یک
    /// وضعیت <b>متوالی</b> است، نه مسابقهٔ هم‌زمان.
    /// </summary>
    [Fact]
    public async Task A_stale_observed_token_conflicts_after_another_writer_advanced_it()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();

        await using var first = CreateContext(target.ConnectionString);
        await first.Database.MigrateAsync();
        await ResetAsync(first);
        await SeedFirstSelectionAsync(first);
        await LoadAsync(first);

        await using (var second = CreateContext(target.ConnectionString))
        {
            var secondStore = new StorefrontShippingDraftStore(second);
            var secondIntent = await secondStore.GetForCapturedSelectionAsync(CartId, CancellationToken.None);
            ApplyIntent(secondIntent!, "نویسنده دوم", Now.AddMinutes(3));
            await secondStore.ReplaceCapturedSelectionAsync(
                secondIntent!,
                Groups(AreaGroupB, RateRowB, freight: 90_000m, lineIds: [LineB]),
                isNew: false,
                expectedSelectionRevision: 1,
                CancellationToken.None);
        }

        var store = new StorefrontShippingDraftStore(first);
        var intent = await store.GetForCapturedSelectionAsync(CartId, CancellationToken.None);
        ApplyIntent(intent!, "توکن کهنه", Now.AddMinutes(4));

        var conflict = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            store.ReplaceCapturedSelectionAsync(
                intent!,
                Groups(AreaGroupA, RateRowA, freight: 15_000m),
                isNew: false,
                expectedSelectionRevision: 1,
                CancellationToken.None));
        Assert.Equal(StorefrontShippingSelectionConflict.Stale, conflict.Code);

        await using var verify = CreateContext(target.ConnectionString);
        var stored = await verify.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Equal(2, stored.SelectionRevision);
        Assert.Equal("نویسنده دوم", stored.CustomerNote);
        var group = Assert.Single(stored.ShippingGroups);
        Assert.Equal(AreaGroupB, group.AcceptedEvidence!.AreaGroupId);
        Assert.Equal([LineB], group.Lines.Select(line => line.CartLineId).ToArray());
    }

    /// <summary>
    /// شکست <b>بعد از</b> حذف و <b>داخل</b> درج: تراکنش کامل برمی‌گردد، ریشه هم Current و هم Original را
    /// از حقیقت پیش از تلاش می‌گیرد، هیچ گرافی از تلاش نمی‌ماند، SaveChanges بعدی صفر تغییر می‌نویسد و
    /// یک ویرایش قانونیِ بعدی نمی‌تواند مقدارهای ردشده را منتشر کند. گراف تمیز و ذخیره‌شدهٔ سبد دیگر هم
    /// دست‌نخورده می‌ماند.
    /// </summary>
    [Fact]
    public async Task A_failure_between_the_delete_and_insert_stages_restores_the_whole_attempt()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();

        await using var db = CreateContext(target.ConnectionString);
        await db.Database.MigrateAsync();
        await ResetAsync(db);
        await ResetAsync(db, OtherCartId);
        await SeedFirstSelectionAsync(db);
        var otherDraft = await SeedOtherDraftAsync(db);

        var store = new StorefrontShippingDraftStore(db);
        var intent = await store.GetForCapturedSelectionAsync(CartId, CancellationToken.None);
        Assert.Equal(1, intent!.SelectionRevision);

        // حقیقت پیش از تلاش (همان چیزی که باید بعد از رد شدن دست‌نخورده بماند).
        var baselineUpdatedAt = intent.UpdatedAt;
        var baselineCountry = intent.DestinationCountryId;
        var baselineProvince = intent.DestinationAdministrativeAreaId;
        var baselineCity = intent.DestinationCityId;

        // قصدِ تلاش: نشانی، نسخهٔ سبد، یادداشت، مبلغ، نام‌ها، مقصد و UpdatedAt همه تغییر می‌کنند.
        intent.Replace(
            null,
            99,
            "گیرنده تلاش",
            "09129999999",
            "کرمان",
            "کرمان",
            "خیابان تلاش",
            "9999999999",
            null,
            "post",
            "پست",
            90_000m,
            ReceivingDate,
            ReceivingDate,
            "09:00-12:00",
            "قصد شکست‌خورده",
            Now.AddMinutes(7));
        intent.ApplyRecipientNames("نام تلاش", "نام‌خانوادگی تلاش");
        intent.SetDestinationIdentity(
            OtherCountryId,
            OtherProvinceId,
            OtherCityId,
            null,
            Now.AddMinutes(7));

        // دو گروه با همان خط سبد: حذف مرحلهٔ اول انجام می‌شود و درج مرحلهٔ دوم روی ایندکس یکتای خط
        // (CartLineId) می‌شکند — یعنی شکست دقیقاً بین دو مرحله، نه پیش از حذف.
        var broken = Groups(AreaGroupB, RateRowB, freight: 90_000m, lineIds: [LineA])
            .Concat(Groups(
                AreaGroupB,
                RateRowB,
                freight: 90_000m,
                lineIds: [LineA],
                seller: OtherSellerPartyId))
            .ToArray();

        await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
            store.ReplaceCapturedSelectionAsync(intent, broken, isNew: false, expectedSelectionRevision: 1, CancellationToken.None));

        var persistedRoot = await LoadAsync(db);
        var rootEntry = db.Entry(persistedRoot);
        var revision = persistedRoot.SelectionRevision;
        Assert.Equal(1, revision);
        Assert.Equal(1, rootEntry.OriginalValues.GetValue<long>(nameof(CartShippingDraft.SelectionRevision)));

        // همهٔ فیلدهای اسکالر ریشه — نه فقط نسخه — همان مقدار پیش از تلاش را دارند.
        Assert.Null(persistedRoot.CustomerNote);
        Assert.Equal(7, persistedRoot.CartVersion);
        Assert.Equal(17_000m, persistedRoot.ShippingAmount);
        Assert.Equal("گیرنده آزمایشی", persistedRoot.RecipientName);
        Assert.Equal("09120000000", persistedRoot.ContactMobile);
        Assert.Equal("خیابان یک", persistedRoot.PostalAddress);
        Assert.Equal("1234567890", persistedRoot.PostalCode);
        Assert.Equal("تهران", persistedRoot.ProvinceName);
        Assert.True(string.IsNullOrEmpty(persistedRoot.FirstName));
        Assert.True(string.IsNullOrEmpty(persistedRoot.LastName));
        Assert.Equal(baselineUpdatedAt, persistedRoot.UpdatedAt);
        Assert.Equal(baselineCountry, persistedRoot.DestinationCountryId);
        Assert.Equal(baselineProvince, persistedRoot.DestinationAdministrativeAreaId);
        Assert.Equal(baselineCity, persistedRoot.DestinationCityId);
        Assert.Null(persistedRoot.DestinationServiceAreaId);

        // Original همان Current است؛ مقدارهای ردشده هیچ‌جا (نه Current، نه Original) باقی نمانده‌اند.
        Assert.Equal(1, rootEntry.OriginalValues.GetValue<long>(nameof(CartShippingDraft.SelectionRevision)));
        Assert.Null(rootEntry.OriginalValues.GetValue<string?>(nameof(CartShippingDraft.CustomerNote)));
        Assert.Equal(7, rootEntry.OriginalValues.GetValue<int>(nameof(CartShippingDraft.CartVersion)));
        Assert.Equal(17_000m, rootEntry.OriginalValues.GetValue<decimal>(nameof(CartShippingDraft.ShippingAmount)));
        Assert.Equal(baselineCity, rootEntry.OriginalValues.GetValue<Guid?>(nameof(CartShippingDraft.DestinationCityId)));

        Assert.Equal(1, persistedRoot.ShippingGroups.Count);
        Assert.Equal(AreaGroupA, persistedRoot.ShippingGroups[0].AcceptedEvidence!.AreaGroupId);

        // هیچ بازمانده‌ای در tracking نیست و SaveChanges بعدی چیزی نمی‌نویسد.
        Assert.False(db.ChangeTracker.HasChanges());
        Assert.All(
            db.ChangeTracker.Entries<CartShippingDraftGroup>(),
            entry => Assert.Equal(EntityState.Unchanged, entry.State));
        Assert.All(
            db.ChangeTracker.Entries<CartShippingDraftGroupLine>(),
            entry => Assert.Equal(EntityState.Unchanged, entry.State));
        Assert.Equal(0, await db.SaveChangesAsync());

        // سبد دیگر با گراف تمیز و ذخیره‌شده خودش دقیقاً دست‌نخورده است.
        var otherEntry = db.Entry(otherDraft);
        Assert.Equal(EntityState.Unchanged, otherEntry.State);
        Assert.Equal(1, otherDraft.SelectionRevision);
        Assert.Equal("یادداشت دیگر", otherDraft.CustomerNote);
        Assert.Single(otherDraft.ShippingGroups);

        // ویرایش قانونی بعدی فقط همان تغییر را منتشر می‌کند؛ نه مقصد/مبلغ/نسخهٔ ردشده.
        persistedRoot.Replace(
            null,
            7,
            persistedRoot.RecipientName,
            persistedRoot.ContactMobile,
            persistedRoot.ProvinceName,
            persistedRoot.CityName,
            persistedRoot.PostalAddress,
            persistedRoot.PostalCode,
            null,
            "post",
            "پست",
            persistedRoot.ShippingAmount,
            ReceivingDate,
            ReceivingDate,
            "09:00-12:00",
            "ویرایش قانونی",
            Now.AddMinutes(9));
        await db.SaveChangesAsync();

        await using var verify = CreateContext(target.ConnectionString);
        var stored = await verify.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Equal(1, stored.SelectionRevision);
        Assert.Equal("ویرایش قانونی", stored.CustomerNote);
        Assert.Equal(CountryId, stored.DestinationCountryId);
        Assert.Equal(17_000m, stored.ShippingAmount);
        var group = Assert.Single(stored.ShippingGroups);
        Assert.Equal(AreaGroupA, group.AcceptedEvidence!.AreaGroupId);
        Assert.Equal([LineA], group.Lines.Select(line => line.CartLineId).ToArray());
    }

    /// <summary>
    /// کار در انتظار نامرتبط پیش از هر تراکنش/تغییر رد می‌شود و دقیقاً همان‌طور که هست باقی می‌ماند.
    /// </summary>
    [Fact]
    public async Task Unrelated_pending_work_is_refused_before_mutation_and_preserved_exactly()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();

        await using var db = CreateContext(target.ConnectionString);
        await db.Database.MigrateAsync();
        await ResetAsync(db);
        await SeedFirstSelectionAsync(db);

        var trackedRoot = await LoadAsync(db);
        var entry = db.Entry(trackedRoot);
        entry.Property(draft => draft.CustomerNote).CurrentValue = "کار در انتظار";
        entry.Property(draft => draft.CustomerNote).IsModified = true;
        Assert.Equal(EntityState.Modified, entry.State);

        var store = new StorefrontShippingDraftStore(db);
        var refusal = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            store.GetForCapturedSelectionAsync(CartId, CancellationToken.None));
        Assert.Equal(StorefrontCapturedSelectionReasons.PendingWork, refusal.Code);

        // کار فراخوان پاک/flush نشده است: همان حالت، همان مقدار، همان تغییر در انتظار.
        Assert.Equal(EntityState.Modified, entry.State);
        Assert.Equal("کار در انتظار", entry.Property(draft => draft.CustomerNote).CurrentValue);
        Assert.True(db.ChangeTracker.HasChanges());

        entry.State = EntityState.Unchanged;

        await using var verify = CreateContext(target.ConnectionString);
        var stored = await verify.ShippingDrafts.SingleAsync(x => x.CartId == CartId);
        Assert.Null(stored.CustomerNote);
        Assert.Equal(1, stored.SelectionRevision);
    }

    /// <summary>
    /// لغو فراخوان در همان مرحلهٔ شروع: توکن اصلی حفظ می‌شود، هیچ گرافی از تلاش نمی‌ماند و ردیف قدیمی
    /// دست‌نخورده است.
    /// </summary>
    [Fact]
    public async Task Caller_cancellation_leaves_no_attempt_and_the_old_selection()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();

        await using var db = CreateContext(target.ConnectionString);
        await db.Database.MigrateAsync();
        await ResetAsync(db);
        await SeedFirstSelectionAsync(db);

        var store = new StorefrontShippingDraftStore(db);
        var intent = await store.GetForCapturedSelectionAsync(CartId, CancellationToken.None);
        ApplyIntent(intent!, "قصد لغوشده", Now.AddMinutes(10));

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            store.ReplaceCapturedSelectionAsync(
                intent!,
                Groups(AreaGroupB, RateRowB, freight: 90_000m, lineIds: [LineB]),
                isNew: false,
                expectedSelectionRevision: 1,
                cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);

        Assert.False(db.ChangeTracker.HasChanges());
        Assert.Equal(0, await db.SaveChangesAsync());

        await using var verify = CreateContext(target.ConnectionString);
        var stored = await verify.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Equal(1, stored.SelectionRevision);
        Assert.Null(stored.CustomerNote);
        var group = Assert.Single(stored.ShippingGroups);
        Assert.Equal(AreaGroupA, group.AcceptedEvidence!.AreaGroupId);
        Assert.Equal([LineA], group.Lines.Select(line => line.CartLineId).ToArray());
    }

    // ---------------------------------------------------------------- helpers

    private static readonly Guid OtherSellerPartyId = Guid.Parse("01a17e00-0000-7000-9000-0000000000ab");
    private static readonly Guid OtherLineId = Guid.Parse("01a17e00-0000-7000-9000-0000000000b3");
    private static readonly Guid OtherCountryId = Guid.Parse("01a17e00-0000-7000-9000-0000000000d4");
    private static readonly Guid OtherProvinceId = Guid.Parse("01a17e00-0000-7000-9000-0000000000d5");
    private static readonly Guid OtherCityId = Guid.Parse("01a17e00-0000-7000-9000-0000000000d6");

    private static async Task SeedFirstSelectionAsync(OrderDbContext db)
    {
        var draft = NewDraft();
        var groups = Groups(AreaGroupA, RateRowA, freight: 15_000m);
        draft.AddShippingGroups(groups, Now);
        draft.SetDestinationIdentity(CountryId, ProvinceId, CityId, null, Now);
        await new StorefrontShippingDraftStore(db)
            .ReplaceCapturedSelectionAsync(draft, groups, isNew: true, expectedSelectionRevision: 0, CancellationToken.None);
    }

    private static async Task<CartShippingDraft> SeedOtherDraftAsync(OrderDbContext db)
    {
        var draft = CartShippingDraft.Create(
            OtherCartId,
            null,
            7,
            "گیرنده دیگر",
            "09120000001",
            "تهران",
            "تهران",
            "خیابان دو",
            "1234567891",
            null,
            "post",
            "پست",
            17_000m,
            ReceivingDate,
            ReceivingDate,
            "09:00-12:00",
            "یادداشت دیگر",
            Now);
        var groups = CartShippingGroupPlanner.Plan(
            OtherCartId,
            [new CartShippingGroupCartLine(OtherLineId, OtherSellerPartyId, 1m)],
            [
                new CartShippingGroupRequest(
                    "post",
                    "post",
                    "standard",
                    OriginContext: null,
                    [OtherLineId],
                    SelectedDeliveryDate: ReceivingDate,
                    SelectedDeliveryTimeWindow: "09:00-12:00"),
            ],
            [
                new CartShippingGroupEligibleMethod(
                    "post",
                    "post",
                    "standard",
                    "پست",
                    17_000m,
                    "IRR",
                    2,
                    ReceivingDate,
                    OtherSellerPartyId),
            ],
            selectedDeliveryDate: null,
            selectedDeliveryTimeWindow: null,
            Now,
            () => Guid.NewGuid());
        draft.AddShippingGroups(groups, Now);
        await new StorefrontShippingDraftStore(db)
            .ReplaceCapturedSelectionAsync(draft, groups, isNew: true, expectedSelectionRevision: 0, CancellationToken.None);
        return draft;
    }

    private static void ApplyIntent(CartShippingDraft intent, string note, DateTimeOffset at) =>
        intent.Replace(
            null,
            7,
            intent.RecipientName,
            intent.ContactMobile,
            intent.ProvinceName,
            intent.CityName,
            intent.PostalAddress,
            intent.PostalCode,
            null,
            "post",
            "پست",
            17_000m,
            ReceivingDate,
            ReceivingDate,
            "09:00-12:00",
            note,
            at);

    private static async Task<CartShippingDraft> LoadAsync(OrderDbContext db) =>
        await db.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);

    private static CartShippingDraft NewDraft() =>
        CartShippingDraft.Create(
            CartId,
            null,
            7,
            "گیرنده آزمایشی",
            "09120000000",
            "تهران",
            "تهران",
            "خیابان یک",
            "1234567890",
            null,
            "post",
            "پست",
            17_000m,
            ReceivingDate,
            ReceivingDate,
            "09:00-12:00",
            null,
            Now);

    private static IReadOnlyList<CartShippingDraftGroup> TwoSellerGroups()
    {
        var groups = new List<CartShippingDraftGroup>();
        groups.AddRange(Groups(AreaGroupA, RateRowA, freight: 15_000m));
        var second = Groups(
            AreaGroupB,
            RateRowB,
            freight: 15_000m,
            lineIds: [LineB],
            seller: OtherSellerPartyId,
            openHour: 10,
            closeHour: 14);
        groups.AddRange(second);
        return groups;
    }

    private static IReadOnlyList<CartShippingDraftGroup> Groups(
        Guid areaGroup,
        Guid rateRow,
        decimal freight,
        IReadOnlyList<Guid>? lineIds = null,
        decimal packaging = 2_000m,
        Guid? seller = null,
        int openHour = 9,
        int closeHour = 17)
    {
        var lines = lineIds ?? [LineA];
        var sellerId = seller ?? SellerPartyId;
        var groups = CartShippingGroupPlanner.Plan(
            CartId,
            lines.Select(line => new CartShippingGroupCartLine(line, sellerId, 1m)).ToArray(),
            [
                new CartShippingGroupRequest(
                    "post",
                    "post",
                    "standard",
                    OriginContext: null,
                    lines,
                    SelectedDeliveryDate: ReceivingDate,
                    SelectedDeliveryTimeWindow: "09:00-12:00"),
            ],
            [
                new CartShippingGroupEligibleMethod(
                    "post",
                    "post",
                    "standard",
                    "پست",
                    freight + packaging,
                    "IRR",
                    2,
                    ReceivingDate,
                    sellerId),
            ],
            selectedDeliveryDate: null,
            selectedDeliveryTimeWindow: null,
            Now,
            () => Guid.NewGuid());

        groups[0].ReplaceAcceptedEvidence(
            ShippingGroupAcceptedEvidence.Create(
                Snapshot,
                DraftSource,
                3,
                1,
                1,
                ShippingGroupOwner.Store("store-alpha"),
                areaGroup,
                rateRow,
                ShippingTypeId,
                freight,
                packaging,
                "IRR",
                freight + packaging,
                0m,
                ReceivingDate,
                new TimeOnly(openHour, 0).Ticks,
                new TimeOnly(closeHour, 0).Ticks,
                "Asia/Tehran",
                2,
                4),
            Now);

        return groups;
    }

    private static async Task ResetAsync(OrderDbContext db, Guid? cartId = null)
    {
        var target = cartId ?? CartId;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"order\".cart_shipping_group_lines WHERE shipping_group_id IN (SELECT shipping_group_id FROM \"order\".cart_shipping_groups WHERE cart_id = {target})");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"order\".cart_shipping_groups WHERE cart_id = {target}");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"order\".cart_shipping_drafts WHERE cart_id = {target}");
    }

    private static OrderDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        return new OrderDbContext(options.Options);
    }
}
