using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Domain.Checkout;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// TB-SHIPPING-INDEPENDENT-GROUP-BACKEND-149 — زمان‌بندی مستقل هر گروه ارسال.
///
/// ریشهٔ اشکال پیش از این تسک: مسیر نوشتن یک تاریخ/پنجرهٔ <b>سراسری</b> را روی همهٔ گروه‌ها کپی
/// می‌کرد (<c>latestMinimum = Max(...)</c>) و وعدهٔ هر گروه از دیرترین آماده‌سازی همهٔ فروشندگان
/// ساخته می‌شد. نتیجه: دو فروشنده با روش یکسان نمی‌توانستند دو وعدهٔ مستقل داشته باشند.
///
/// این تست‌ها فقط منطق دامنه/ماشین‌حساب را می‌سنجند: بدون Host، بدون دیتابیس مشترک، بدون پروسه.
/// </summary>
public sealed class ShippingGroupIndependentScheduleTests
{
    private static readonly Guid SellerA = Guid.Parse("aaaaaaaa-0000-7000-8000-0000000000a1");
    private static readonly Guid SellerB = Guid.Parse("bbbbbbbb-0000-7000-8000-0000000000b2");
    private static readonly Guid Cart = Guid.Parse("cccccccc-0000-7000-8000-000000000149");
    private static readonly Guid LineA = Guid.Parse("11111111-0000-7000-8000-00000000000a");
    private static readonly Guid LineB = Guid.Parse("11111111-0000-7000-8000-00000000000b");
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 16);

    // آماده‌سازی ۲ و ۳ روز برای دو فروشنده، با روش canonical یکسان. تفاضل وعده فقط از فروشنده می‌آید.
    private static readonly ShippingMethodsOptions Options = new()
    {
        DefaultSellerPreparationDays = 1,
        SellerPreparationDaysByPartyId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [SellerA.ToString("D")] = 2,
            [SellerB.ToString("D")] = 3,
        },
        DeliveryHorizonDays = 5,
    };

    private static int TipaxLeadDays => 3;

    [Fact]
    public void Two_sellers_with_the_same_method_get_two_different_group_minimums()
    {
        var minimumA = StorefrontShippingCalculator.ComputeGroupMinimumDeliveryDate(
            Today, SellerA, TipaxLeadDays, Options);
        var minimumB = StorefrontShippingCalculator.ComputeGroupMinimumDeliveryDate(
            Today, SellerB, TipaxLeadDays, Options);

        // today + prep + lead ⇒ ۲۱ و ۲۲ اکتبر؛ هیچ دیرترین-حداقل سراسری اینجا وجود ندارد.
        Assert.Equal(new DateOnly(2026, 10, 21), minimumA);
        Assert.Equal(new DateOnly(2026, 10, 22), minimumB);
        Assert.NotEqual(minimumA, minimumB);
        Assert.Equal(2, StorefrontShippingCalculator.SellerPreparationDays(SellerA, Options));
        Assert.Equal(3, StorefrontShippingCalculator.SellerPreparationDays(SellerB, Options));
        // فروشندهٔ ناشناخته هرگز آماده‌سازی دیگری نمی‌گیرد؛ همان پیش‌فرض سرور است.
        Assert.Equal(1, StorefrontShippingCalculator.SellerPreparationDays(Guid.NewGuid(), Options));
    }

    [Fact]
    public void Each_group_persists_its_own_selected_date_and_window()
    {
        var groups = PlanPerSeller();

        var groupA = groups.Single(g => g.SellerPartyId == SellerA);
        var groupB = groups.Single(g => g.SellerPartyId == SellerB);

        Assert.Equal("tipax:standard", groupA.MethodCode);
        Assert.Equal("tipax:standard", groupB.MethodCode);
        Assert.Equal(new DateOnly(2026, 10, 21), groupA.MinimumDeliveryDate);
        Assert.Equal(new DateOnly(2026, 10, 22), groupB.MinimumDeliveryDate);
        Assert.Equal(new DateOnly(2026, 10, 23), groupA.SelectedDeliveryDate);
        Assert.Equal(new DateOnly(2026, 10, 25), groupB.SelectedDeliveryDate);
        Assert.Equal("9-12", groupA.SelectedDeliveryTimeWindow);
        Assert.Equal("18-21", groupB.SelectedDeliveryTimeWindow);
    }

    [Fact]
    public void Changing_group_a_schedule_leaves_group_b_untouched()
    {
        var groups = PlanPerSeller();
        var groupB = groups.Single(g => g.SellerPartyId == SellerB);
        var groupBDate = groupB.SelectedDeliveryDate;
        var groupBWindow = groupB.SelectedDeliveryTimeWindow;

        // همان سبد، ولی زمان‌بندی گروه A تغییر می‌کند: فقط A باید عوض شود.
        var replanned = PlanPerSeller(new DateOnly(2026, 10, 24), "12-15");
        var changedA = replanned.Single(g => g.SellerPartyId == SellerA);
        var untouchedB = replanned.Single(g => g.SellerPartyId == SellerB);

        Assert.Equal(new DateOnly(2026, 10, 24), changedA.SelectedDeliveryDate);
        Assert.Equal("12-15", changedA.SelectedDeliveryTimeWindow);
        Assert.Equal(groupBDate, untouchedB.SelectedDeliveryDate);
        Assert.Equal(groupBWindow, untouchedB.SelectedDeliveryTimeWindow);
        // حداقل تحویل هر گروه هم دست‌نخورده است (وعدهٔ گروه دیگر بازنویسی نمی‌شود).
        Assert.Equal(new DateOnly(2026, 10, 21), changedA.MinimumDeliveryDate);
        Assert.Equal(new DateOnly(2026, 10, 22), untouchedB.MinimumDeliveryDate);
    }

    [Fact]
    public void A_seller_specific_promise_is_never_borrowed_by_another_seller()
    {
        var lines = new[]
        {
            new CartShippingGroupCartLine(LineA, SellerA, 1m),
            new CartShippingGroupCartLine(LineB, SellerB, 1m),
        };
        var requests = new[]
        {
            new CartShippingGroupRequest("tipax:standard", null, null, null, [LineA]),
            new CartShippingGroupRequest("tipax:standard", null, null, null, [LineB]),
        };
        // فقط فروشندهٔ A ردیف واجد شرایط دارد: گروه B باید fail-closed رد شود، نه اینکه وعدهٔ A را بگیرد.
        var eligible = new[]
        {
            new CartShippingGroupEligibleMethod(
                "tipax:standard", "tipax", "standard", "تیپاکس عادی", 120_000m, "IRT", TipaxLeadDays,
                new DateOnly(2026, 10, 21), SellerA),
        };

        var exception = Assert.Throws<ContractOperationException>(() =>
            CartShippingGroupPlanner.Plan(Cart, lines, requests, eligible, null, null, Now, NewIds()));

        Assert.Equal(CartShippingGroupFault.MethodNotEligible, exception.Code);
    }

    [Fact]
    public void A_seller_less_authoritative_row_still_serves_legacy_callers()
    {
        var lines = new[] { new CartShippingGroupCartLine(LineA, SellerA, 1m) };
        var requests = new[] { new CartShippingGroupRequest("tipax:standard", null, null, null, [LineA]) };
        var eligible = new[]
        {
            new CartShippingGroupEligibleMethod(
                "tipax:standard", "tipax", "standard", "تیپاکس عادی", 120_000m, "IRT", TipaxLeadDays,
                new DateOnly(2026, 10, 21)),
        };

        var groups = CartShippingGroupPlanner.Plan(
            Cart, lines, requests, eligible, null, null, Now, NewIds());

        Assert.Single(groups);
        Assert.Equal(new DateOnly(2026, 10, 21), groups[0].MinimumDeliveryDate);
        Assert.Null(groups[0].SelectedDeliveryDate);
    }

    [Fact]
    public void Legacy_scalar_still_applies_to_a_single_group_without_its_own_schedule()
    {
        var lines = new[] { new CartShippingGroupCartLine(LineA, SellerA, 1m) };
        var requests = new[] { new CartShippingGroupRequest("tipax:standard", null, null, null, [LineA]) };

        var groups = CartShippingGroupPlanner.Plan(
            Cart, lines, requests, EligiblePerSeller(), new DateOnly(2026, 10, 23), "9-12", Now, NewIds());

        Assert.Single(groups);
        Assert.Equal(new DateOnly(2026, 10, 23), groups[0].SelectedDeliveryDate);
        Assert.Equal("9-12", groups[0].SelectedDeliveryTimeWindow);
    }

    [Fact]
    public void Draft_legacy_scalars_are_null_for_multi_group_and_kept_for_one_group()
    {
        var multi = DraftWith(PlanPerSeller());
        Assert.Equal(string.Empty, multi.ShippingMethodCode);
        // هیچ تاریخ/پنجرهٔ سراسریِ authoritative در سبد چندگروهی ساخته نمی‌شود.
        Assert.Null(multi.SelectedDeliveryDate);
        Assert.Null(multi.SelectedDeliveryTimeWindow);
        // حداقل تاریخ قدیمی (ستون غیرnullable) فقط کران محافظه‌کارانهٔ دیرترین گروه است.
        Assert.Equal(new DateOnly(2026, 10, 22), multi.MinimumDeliveryDate);

        var single = DraftWith([PlanPerSeller().Single(g => g.SellerPartyId == SellerA)]);
        Assert.Equal("tipax:standard", single.ShippingMethodCode);
        Assert.Equal(new DateOnly(2026, 10, 23), single.SelectedDeliveryDate);
        Assert.Equal("9-12", single.SelectedDeliveryTimeWindow);
        Assert.Equal(new DateOnly(2026, 10, 21), single.MinimumDeliveryDate);
    }

    private static IReadOnlyList<CartShippingDraftGroup> PlanPerSeller(
        DateOnly? groupADate = null,
        string? groupAWindow = null)
    {
        var lines = new[]
        {
            new CartShippingGroupCartLine(LineA, SellerA, 1m),
            new CartShippingGroupCartLine(LineB, SellerB, 1m),
        };
        var requests = new[]
        {
            new CartShippingGroupRequest(
                "tipax:standard", null, null, null, [LineA],
                groupADate ?? new DateOnly(2026, 10, 23), groupAWindow ?? "9-12"),
            new CartShippingGroupRequest(
                "tipax:standard", null, null, null, [LineB], new DateOnly(2026, 10, 25), "18-21"),
        };

        return CartShippingGroupPlanner.Plan(
            Cart, lines, requests, EligiblePerSeller(), null, null, Now, NewIds());
    }

    private static IReadOnlyList<CartShippingGroupEligibleMethod> EligiblePerSeller() =>
    [
        new("tipax:standard", "tipax", "standard", "تیپاکس عادی", 120_000m, "IRT", TipaxLeadDays,
            StorefrontShippingCalculator.ComputeGroupMinimumDeliveryDate(
                Today, SellerA, TipaxLeadDays, Options),
            SellerA),
        new("tipax:standard", "tipax", "standard", "تیپاکس عادی", 120_000m, "IRT", TipaxLeadDays,
            StorefrontShippingCalculator.ComputeGroupMinimumDeliveryDate(
                Today, SellerB, TipaxLeadDays, Options),
            SellerB),
    ];

    private static CartShippingDraft DraftWith(IReadOnlyList<CartShippingDraftGroup> groups)
    {
        var draft = CartShippingDraft.Create(
            Cart, "hash", 1, "گیرنده", "09120000000", "تهران", "تهران", "نشانی", "1234567890",
            null, groups[0].MethodCode, groups[0].MethodLabel, groups[0].ShippingAmount,
            groups[0].MinimumDeliveryDate, groups[0].SelectedDeliveryDate,
            groups[0].SelectedDeliveryTimeWindow, null, Now);
        draft.AddShippingGroups(groups, Now);
        return draft;
    }

    private static Func<Guid> NewIds()
    {
        var next = 0;
        return () => Guid.Parse($"00000000-0000-7000-9000-0000000001{++next:D2}");
    }
}
