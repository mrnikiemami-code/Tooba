using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Infrastructure;
using Tooba.Order.Infrastructure.Checkout.Persistence;
using Tooba.Tax.Contracts.Dtos;
using Tooba.Tax.Contracts.Ports;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-217: فکت مبلغ «همین گروه کالاها» پس از تخفیف و پس از مالیات از پیش‌نمایش authoritative
/// checkout؛ فقط-خواندنی، بدون بازمحاسبهٔ مالیات/تخفیف و بدون ماندگاری.
/// </summary>
[Collection("ShippingGroupGoodsFacts")]
public sealed class ShippingGroupGoodsFactsTests
{
    private static readonly Guid LineA = Guid.Parse("0199dddd-0000-7000-8000-00000000000a");
    private static readonly Guid LineB = Guid.Parse("0199dddd-0000-7000-8000-00000000000b");
    private static readonly Guid LineC = Guid.Parse("0199dddd-0000-7000-8000-00000000000c");
    private static readonly Guid OfferA = Guid.Parse("0199eeee-0000-7000-8000-00000000000a");
    private static readonly Guid OfferB = Guid.Parse("0199eeee-0000-7000-8000-00000000000b");
    private static readonly Guid OfferC = Guid.Parse("0199eeee-0000-7000-8000-00000000000c");

    /// <summary>
    /// دو گروه از یک فروشنده با عضو و مبلغ جدا: 100-10+9=99 و 200-20+18=198؛ نتیجه 99 و 198 است،
    /// نه 297 و نه سهمی از جمع کل سبد.
    /// </summary>
    [Fact]
    public async Task Two_groups_of_the_same_seller_return_their_own_after_discount_and_tax_amount()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var cart = harness.Cart(
            harness.Line(LineA, OfferA, 100m),
            harness.Line(LineB, OfferB, 200m));
        harness.Carts.Cart = cart;
        harness.Promotions.Discounts[OfferA] = 10m;
        harness.Promotions.Discounts[OfferB] = 20m;

        var result = await harness.Directory.ReadAsync(
            new CheckoutShippingGroupGoodsFactsRequest(
                harness.Command(),
                [
                    new CheckoutShippingGroupMembership("g1", [LineA]),
                    new CheckoutShippingGroupMembership("g2", [LineB]),
                ]),
            CancellationToken.None);

        Assert.Equal(harness.CartId, result.CartId);
        Assert.Equal(7, result.CartVersion);
        Assert.Equal(ShippingGroupGoodsFactsHarness.Currency, result.Currency);
        Assert.Equal(2, result.Groups.Count);

        var first = result.Groups[0];
        Assert.Equal("g1", first.GroupKey);
        Assert.Equal(harness.SellerId, first.SellerPartyId);
        Assert.Equal(99m, first.FinalGoodsAmountAfterDiscountAndTax);
        var firstContribution = Assert.Single(first.Contributions);
        Assert.Equal(LineA, firstContribution.CartLineId);
        Assert.Equal(10m, firstContribution.DiscountAmount);
        Assert.Equal(90m, firstContribution.PostDiscountTaxExclusiveAmount);
        Assert.Equal(9m, firstContribution.TaxAmount);
        Assert.Equal(99m, firstContribution.TaxInclusiveAmount);

        var second = result.Groups[1];
        Assert.Equal("g2", second.GroupKey);
        Assert.Equal(198m, second.FinalGoodsAmountAfterDiscountAndTax);
        var secondContribution = Assert.Single(second.Contributions);
        Assert.Equal(20m, secondContribution.DiscountAmount);
        Assert.Equal(18m, secondContribution.TaxAmount);

        // فکت‌های فیزیکی این برش منبع authoritative ندارند و صادقانه ناشناخته‌اند.
        Assert.Null(first.GroupWeightGrams);
        Assert.Null(first.PhysicalItemCount);
        Assert.Null(second.GroupWeightGrams);
    }

    /// <summary>
    /// دو خط سبد با OfferId یکسان، هویت و سهم خودشان را نگه می‌دارند؛ تطبیق با نقشهٔ دقیق خط سبد
    /// به خط سفارش است، نه OfferId/مقدار.
    /// </summary>
    [Fact]
    public async Task Two_cart_lines_sharing_an_offer_keep_separate_contributions_and_order_line_identity()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var cart = harness.Cart(
            harness.Line(LineA, OfferA, 100m, quantity: 1m),
            harness.Line(LineC, OfferA, 100m, quantity: 3m));
        harness.Carts.Cart = cart;
        harness.Promotions.Discounts[OfferA] = 10m;

        var result = await harness.Directory.ReadAsync(
            new CheckoutShippingGroupGoodsFactsRequest(
                harness.Command(),
                [
                    new CheckoutShippingGroupMembership("one", [LineA]),
                    new CheckoutShippingGroupMembership("three", [LineC]),
                ]),
            CancellationToken.None);

        var single = Assert.Single(result.Groups[0].Contributions);
        var triple = Assert.Single(result.Groups[1].Contributions);

        Assert.Equal(1m, single.Quantity);
        Assert.Equal(3m, triple.Quantity);
        Assert.NotEqual(single.OrderLineId, triple.OrderLineId);
        Assert.Equal(OfferA, single.OfferId);
        Assert.Equal(OfferA, triple.OfferId);
        Assert.Equal(1m, result.Groups[0].QuantityTotal);
        Assert.Equal(3m, result.Groups[1].QuantityTotal);

        // تخفیف روی هر خط جداگانه اعمال می‌شود (300 - 10) و با Join اشتباه مخلوط نمی‌شود.
        Assert.Equal(100m - 10m + 9m, result.Groups[0].FinalGoodsAmountAfterDiscountAndTax);
        Assert.Equal(300m - 10m + 29m, result.Groups[1].FinalGoodsAmountAfterDiscountAndTax);
    }

    /// <summary>
    /// دسترسی/وضعیت/نسخه/افراز نامعتبر پیش از هر Pricing/Promotion/Tax رد می‌شود.
    /// </summary>
    [Fact]
    public async Task Invalid_access_status_version_or_partition_fails_before_any_quote_call()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var lineA = harness.Line(LineA, OfferA, 100m);
        var lineB = harness.Line(LineB, OfferB, 200m);
        var valid = harness.Cart(lineA, lineB);
        var goodGroups = new[]
        {
            new CheckoutShippingGroupMembership("g1", [LineA]),
            new CheckoutShippingGroupMembership("g2", [LineB]),
        };

        await AssertFailsBeforeQuote(harness, null, goodGroups, "checkout.cart.missing");
        await AssertFailsBeforeQuote(
            harness,
            valid with { Status = CartStatus.Expired },
            goodGroups,
            "checkout.cart.expired");
        await AssertFailsBeforeQuote(harness, valid with { Lines = [] }, goodGroups, "checkout.cart.empty");
        await AssertFailsBeforeQuote(harness, valid, goodGroups, "PRICE_CHANGED", expectedVersion: 6);
        await AssertFailsBeforeQuote(
            harness,
            valid,
            [new CheckoutShippingGroupMembership("g1", [LineA])],
            CheckoutDirectory.GroupPartitionInvalid);
        await AssertFailsBeforeQuote(
            harness,
            valid,
            [
                new CheckoutShippingGroupMembership("g1", [LineA, LineB]),
                new CheckoutShippingGroupMembership("g2", [LineB]),
            ],
            CheckoutDirectory.GroupPartitionInvalid);
        await AssertFailsBeforeQuote(
            harness,
            valid,
            [
                new CheckoutShippingGroupMembership("g1", [LineA]),
                new CheckoutShippingGroupMembership("g1", [LineB]),
            ],
            CheckoutDirectory.GroupPartitionInvalid);
        await AssertFailsBeforeQuote(
            harness,
            valid,
            [
                new CheckoutShippingGroupMembership("g1", []),
                new CheckoutShippingGroupMembership("g2", [LineA, LineB]),
            ],
            CheckoutDirectory.GroupPartitionInvalid);
        await AssertFailsBeforeQuote(
            harness,
            valid,
            [
                new CheckoutShippingGroupMembership("g1", [LineA, Guid.Parse("0199dddd-0000-7000-8000-0000000000ff")]),
                new CheckoutShippingGroupMembership("g2", [LineB]),
            ],
            CheckoutDirectory.GroupPartitionInvalid);
        await AssertFailsBeforeQuote(
            harness,
            valid,
            [new CheckoutShippingGroupMembership(" ", [LineA, LineB])],
            CheckoutDirectory.GroupPartitionInvalid);
        await AssertFailsBeforeQuote(
            harness,
            valid,
            [
                new CheckoutShippingGroupMembership("g1", [LineA]),
                new CheckoutShippingGroupMembership("g1 ", [LineB]),
            ],
            CheckoutDirectory.GroupPartitionInvalid);
        await AssertFailsBeforeQuote(harness, valid, [], CheckoutDirectory.GroupPartitionInvalid);
    }

    /// <summary>گروه نمی‌تواند خطوط دو فروشنده را در یک مبلغ ترکیب کند.</summary>
    [Fact]
    public async Task A_group_mixing_two_sellers_fails_closed_without_quote_calls()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var lineA = harness.Line(LineA, OfferA, 100m);
        var lineB = harness.Line(LineB, OfferB, 200m, sellerPartyId: harness.OtherSellerId);
        var cart = harness.Cart(lineA, lineB);

        await AssertFailsBeforeQuote(
            harness,
            cart,
            [new CheckoutShippingGroupMembership("mixed", [LineA, LineB])],
            CheckoutDirectory.GroupPartitionInvalid);
    }

    /// <summary>
    /// تغییر کوپن/مالیات در quote واقعی بازتاب می‌یابد (بدون کش) و توکن لغو فراخوان به درزهای
    /// بیرونی می‌رسد.
    /// </summary>
    [Fact]
    public async Task Changed_discount_and_tax_flow_through_and_caller_token_is_propagated()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        harness.Carts.Cart = harness.Cart(harness.Line(LineA, OfferA, 100m));
        using var cancellation = new CancellationTokenSource();

        var withoutCoupon = await harness.Directory.ReadAsync(
            new CheckoutShippingGroupGoodsFactsRequest(
                harness.Command(),
                [new CheckoutShippingGroupMembership("g1", [LineA])]),
            cancellation.Token);
        Assert.Equal(110m, withoutCoupon.Groups[0].FinalGoodsAmountAfterDiscountAndTax);

        harness.Promotions.Discounts[OfferA] = 25m;
        harness.Taxes.Rate = 0.20m;

        var afterChange = await harness.Directory.ReadAsync(
            new CheckoutShippingGroupGoodsFactsRequest(
                harness.Command(coupon: "SAVE25"),
                [new CheckoutShippingGroupMembership("g1", [LineA])]),
            cancellation.Token);
        Assert.Equal(75m + 15m, afterChange.Groups[0].FinalGoodsAmountAfterDiscountAndTax);

        Assert.Equal(cancellation.Token, harness.Promotions.LastToken);
        Assert.Equal(cancellation.Token, harness.Taxes.LastToken);
    }

    /// <summary>
    /// خطای درز بیرونی به فراخوان می‌رسد و نتیجهٔ ساختگی/کش‌شده ساخته نمی‌شود.
    /// </summary>
    [Fact]
    public async Task Foreign_failure_propagates_and_produces_no_facts()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        harness.Carts.Cart = harness.Cart(harness.Line(LineA, OfferA, 100m));
        harness.Taxes.Failure = new ContractOperationException("TAX_CALCULATION_ERROR");

        var error = await Assert.ThrowsAsync<ContractOperationException>(() => harness.Directory.ReadAsync(
            new CheckoutShippingGroupGoodsFactsRequest(
                harness.Command(),
                [new CheckoutShippingGroupMembership("g1", [LineA])]),
            CancellationToken.None));
        Assert.Equal("TAX_CALCULATION_ERROR", error.Code);

        harness.Taxes.Failure = null;
        harness.Taxes.Override = new TaxCalculationResult(
            TaxOutcome.Taxable,
            100m,
            0.10m,
            -1m,
            99m,
            ShippingGroupGoodsFactsHarness.Currency,
            null,
            null,
            DateTimeOffset.UtcNow);
        var amount = await Assert.ThrowsAsync<ContractOperationException>(() => harness.Directory.ReadAsync(
            new CheckoutShippingGroupGoodsFactsRequest(
                harness.Command(),
                [new CheckoutShippingGroupMembership("g1", [LineA])]),
            CancellationToken.None));
        Assert.Equal(CheckoutDirectory.GroupGoodsAmountInvalid, amount.Code);
    }

    /// <summary>
    /// ارز مختلط سبد fail-closed است (ارز تک Order) و هرگز با تبدیل نرخ جمع نمی‌شود.
    /// </summary>
    [Fact]
    public async Task Mixed_cart_currency_fails_closed()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var lineA = harness.Line(LineA, OfferA, 100m);
        var lineB = harness.Line(LineB, OfferB, 200m) with { QuotedCurrency = "USD" };
        harness.Carts.Cart = harness.Cart(lineA, lineB);

        await Assert.ThrowsAsync<StorefrontOrderException>(() => harness.Directory.ReadAsync(
            new CheckoutShippingGroupGoodsFactsRequest(
                harness.Command(),
                [
                    new CheckoutShippingGroupMembership("g1", [LineA]),
                    new CheckoutShippingGroupMembership("g2", [LineB]),
                ]),
            CancellationToken.None));
    }

    /// <summary>
    /// خواندن فکت‌ها هیچ Checkout/Order/رزرو/تبدیل سبد/پرداختی تولید نمی‌کند و ChangeTracker
    /// دست‌نخورده می‌ماند.
    /// </summary>
    [Fact]
    public async Task Reading_facts_persists_nothing_and_reserves_nothing()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        harness.Carts.Cart = harness.Cart(
            harness.Line(LineA, OfferA, 100m),
            harness.Line(LineB, OfferB, 200m));

        // هیچ provider پایگاه‌داده در این harness پیکربندی نشده است؛ پس هر خواندن/نوشتن واقعی
        // OrderDbContext حتماً استثنا می‌داد. موفق‌شدن خواندن فکت‌ها یعنی مسیر DB لمس نشده است.
        Assert.Throws<InvalidOperationException>(() => harness.Db.Checkouts.Any());

        var result = await harness.Directory.ReadAsync(
            new CheckoutShippingGroupGoodsFactsRequest(
                harness.Command(),
                [
                    new CheckoutShippingGroupMembership("g1", [LineA]),
                    new CheckoutShippingGroupMembership("g2", [LineB]),
                ]),
            CancellationToken.None);

        Assert.Equal(2, result.Groups.Count);
        Assert.Throws<InvalidOperationException>(() => harness.Db.Checkouts.Any());
        Assert.Throws<InvalidOperationException>(() => harness.Db.ChangeTracker.Entries());
        Assert.Equal(1, harness.Carts.Calls);
        Assert.Equal(1, harness.Guard.Calls);
        Assert.Equal(2, harness.Prices.Calls);
        Assert.Equal(2, harness.Promotions.Calls);
        Assert.Equal(2, harness.Taxes.Calls);
    }

    /// <summary>
    /// همان نمونهٔ scoped دایرکتوری درز فکت‌ها را می‌دهد؛ نمونهٔ دوم/DbContext دوم ساخته نمی‌شود.
    /// </summary>
    [Fact]
    public void Module_registration_exposes_the_same_scoped_directory_instance()
    {
        var services = new ServiceCollection();
        var configuration = new EmptyConfiguration();
        new OrderModule().AddServices(services, configuration, new FakeHostEnvironment());
        services.AddScoped<ICheckoutDirectory>(_ => new ProbeCheckoutDirectory());

        var descriptor = Assert.Single(
            services.Where(service => service.ServiceType == typeof(ICheckoutShippingGroupGoodsFactsReader)));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);

        using var provider = services.BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        var reader = firstScope.ServiceProvider.GetRequiredService<ICheckoutShippingGroupGoodsFactsReader>();
        var directory = firstScope.ServiceProvider.GetRequiredService<ICheckoutDirectory>();
        Assert.Same(directory, reader);
        Assert.Same(reader, firstScope.ServiceProvider.GetRequiredService<ICheckoutShippingGroupGoodsFactsReader>());

        using var secondScope = provider.CreateScope();
        Assert.NotSame(reader, secondScope.ServiceProvider.GetRequiredService<ICheckoutShippingGroupGoodsFactsReader>());
    }

    private static async Task AssertFailsBeforeQuote(
        ShippingGroupGoodsFactsHarness harness,
        CartSnapshot? cart,
        IReadOnlyList<CheckoutShippingGroupMembership> groups,
        string expectedCode,
        int expectedVersion = 7)
    {
        harness.Carts.Cart = cart;

        var error = await Record.ExceptionAsync(() => harness.Directory.ReadAsync(
            new CheckoutShippingGroupGoodsFactsRequest(harness.Command(expectedVersion), groups),
            CancellationToken.None));

        Assert.NotNull(error);
        Assert.Equal(expectedCode, ErrorCodeOf(error!));
        Assert.Equal(0, harness.Prices.Calls);
        Assert.Equal(0, harness.Promotions.Calls);
        Assert.Equal(0, harness.Taxes.Calls);
    }

    private static string? ErrorCodeOf(Exception error) => error switch
    {
        ContractOperationException contract => contract.Code,
        StorefrontOrderException storefront => storefront.Code,
        _ => null,
    };

    private sealed class EmptyConfiguration : IConfiguration
    {
        public string? this[string key]
        {
            get => null;
            set { }
        }

        public IEnumerable<IConfigurationSection> GetChildren() => [];

        public IChangeToken GetReloadToken() => throw new NotSupportedException();

        public IConfigurationSection GetSection(string key) => new EmptySection(key);

        private sealed class EmptySection(string key) : IConfigurationSection
        {
            public string? this[string childKey]
            {
                get => null;
                set { }
            }

            public string Key => key;

            public string Path => key;

            public string? Value { get; set; }

            public IEnumerable<IConfigurationSection> GetChildren() => [];

            public IChangeToken GetReloadToken() => throw new NotSupportedException();

            public IConfigurationSection GetSection(string childKey) => new EmptySection(childKey);
        }
    }
}
