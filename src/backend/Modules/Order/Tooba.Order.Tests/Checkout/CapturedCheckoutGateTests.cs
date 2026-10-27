using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;
using Tooba.BuildingBlocks;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.Storefront.Shipping.CapturedPreview;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Checkout.Persistence;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// دروازهٔ ثبت نهایی capture (TB-233): کلید <c>CheckoutEnabled</c>، مسیر قدیمی دست‌نخورده و رد شدن
/// شاهد بدون بازاعتبارسنجی سرور.
/// </summary>
/// <remarks>
/// فقط مسیرهایی که عمداً <b>هیچ</b> خواندنی از پایگاه داده انجام نمی‌دهند اینجا اثبات می‌شوند؛ مسیر
/// روشن‌بودن کلید به خواندن واقعی پیش‌نویس نیاز دارد و در این برش اثبات نشده است (در RESULT.md ثبت شده).
/// </remarks>
public sealed class CapturedCheckoutGateTests
{
    private static readonly Guid CartId = Guid.Parse("01a17d0d-e033-7000-af43-49a1daa28822");

    [Fact]
    public async Task Legacy_submit_without_evidence_gets_the_exact_same_plans_and_no_refusal()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyContextAsync(target);
        var directory = Directory(db, checkoutEnabled: false);
        var plans = new[] { LegacyPlan() };

        var accepted = await Host(directory).AcceptCapturedShippingAsync(
            Command(plans, expectedSelectionRevision: null),
            new ShippingGroupGoodsFactsHarness().Cart(),
            [],
            DateTimeOffset.UnixEpoch,
            CancellationToken.None);

        Assert.Same(plans[0], accepted.Single());
        Assert.Null(accepted.Single().AcceptedEvidence);
    }

    [Fact]
    public async Task Caller_supplied_evidence_is_refused_when_the_final_gate_is_off()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyContextAsync(target);
        var directory = Directory(db, checkoutEnabled: false);
        var plans = new[] { CapturedPlan() };

        var error = await Assert.ThrowsAsync<ContractOperationException>(() =>
            Host(directory).AcceptCapturedShippingAsync(
                Command(plans, expectedSelectionRevision: "1"),
                new ShippingGroupGoodsFactsHarness().Cart(),
                [],
                DateTimeOffset.UnixEpoch,
                CancellationToken.None));

        Assert.Equal(StorefrontCapturedPreviewReasons.PreviewNotReady, error.Code);
    }

    /// <summary>
    /// شاهد ساختگی بدون هیچ پیش‌نویس capture‌شدهٔ ذخیره‌شده، حتی با کلید روشن، هرگز legacy نمی‌شود و
    /// پیش از هر خواندن ماژول بیرونی رد می‌شود.
    /// </summary>
    [Fact]
    public async Task Forged_caller_evidence_without_a_stored_capture_is_refused_even_when_the_gate_is_on()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyContextAsync(target);
        var directory = Directory(db, checkoutEnabled: true);
        var plans = new[] { CapturedPlan() };

        var error = await Assert.ThrowsAsync<ContractOperationException>(() =>
            Host(directory).AcceptCapturedShippingAsync(
                Command(plans, expectedSelectionRevision: "1"),
                new ShippingGroupGoodsFactsHarness().Cart(),
                [],
                DateTimeOffset.UnixEpoch,
                CancellationToken.None));

        Assert.Equal(StorefrontCapturedPreviewReasons.PreviewNotReady, error.Code);
    }

    /// <summary>
    /// قصد capture تنها با متن نسخه (بدون شاهد ذخیره‌شده) هم مسیر capture است و هرگز به مسیر قدیمی
    /// نمی‌افتد؛ پیش از رزرو/نوشتن سفارش رد می‌شود.
    /// </summary>
    [Fact]
    public async Task Revision_intent_alone_never_falls_back_to_the_legacy_path()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyContextAsync(target);
        var directory = Directory(db, checkoutEnabled: true);
        var plans = new[] { LegacyPlan() };

        var error = await Assert.ThrowsAsync<ContractOperationException>(() =>
            Host(directory).AcceptCapturedShippingAsync(
                Command(plans, expectedSelectionRevision: "2"),
                new ShippingGroupGoodsFactsHarness().Cart(),
                [],
                DateTimeOffset.UnixEpoch,
                CancellationToken.None));

        Assert.Equal(StorefrontCapturedPreviewReasons.PreviewNotReady, error.Code);
    }

    [Theory]
    [InlineData("0", true, 0)]
    [InlineData("1", true, 1)]
    [InlineData("4294967296", true, 4294967296)]
    [InlineData("", false, 0)]
    [InlineData(" ", false, 0)]
    [InlineData(" 1", false, 0)]
    [InlineData("1 ", false, 0)]
    [InlineData("-1", false, 0)]
    [InlineData("+1", false, 0)]
    [InlineData("1.0", false, 0)]
    [InlineData("1e3", false, 0)]
    [InlineData("abc", false, 0)]
    public void Selection_revision_text_is_parsed_strictly(string text, bool expected, long revision)
    {
        var parsed = CheckoutDirectory.TryParseSelectionRevision(text, out var value);

        Assert.Equal(expected, parsed);
        if (expected)
        {
            Assert.Equal(revision, value);
        }
    }

    private static ICheckoutSubmitHost Host(CheckoutDirectory directory) => directory;

    private static SubmitCheckoutCommand Command(
        IReadOnlyList<OrderShippingGroupPlan> plans,
        string? expectedSelectionRevision) => new(
        CartId,
        new Tooba.Cart.Contracts.CartAccess(Guid.Parse("01a17cf7-e38d-7000-82a5-00c57929bc54"), null),
        7,
        OrderMode.OnlinePurchase,
        BuyerPartyId: null,
        PlacedByUserId: Guid.Parse("01a17cf7-e38d-7000-82a5-00c57929bc54"),
        IdempotencyKey: "tb233-gate",
        TaxJurisdiction: "IR-NAT",
        ShippingGroups: plans,
        ExpectedSelectionRevision: expectedSelectionRevision);

    private static OrderShippingGroupPlan LegacyPlan() => new(
        Guid.Parse("01a17d0f-0000-7000-8000-000000000001"),
        Guid.Parse("0199aaaa-0000-7000-8000-0000000000aa"),
        "post|standard",
        "post",
        "standard",
        "پست",
        null,
        5000m,
        "IRR",
        new DateOnly(2026, 10, 29),
        new DateOnly(2026, 10, 29),
        "09:00–17:00",
        2,
        [Guid.Parse("01a17cfd-0000-7000-8000-000000000001")],
        null);

    private static OrderShippingGroupPlan CapturedPlan()
    {
        var legacy = LegacyPlan();
        return legacy with
        {
            AcceptedEvidence = ShippingGroupAcceptedEvidenceTests.Evidence(
                Guid.Parse("01a17d0e-1111-7000-8000-0000000000a1"),
                Guid.Parse("01a17d0e-2222-7000-8000-0000000000b2"),
                freight: 120000m,
                packaging: 5000m),
        };
    }

    private static CheckoutDirectory Directory(OrderDbContext db, bool checkoutEnabled)
    {
        var harness = new ShippingGroupGoodsFactsHarness();
        return new CheckoutDirectory(
            db,
            harness.Guard,
            harness.Carts,
            harness.Offers,
            harness.Prices,
            harness.Inventory,
            harness.Taxes,
            harness.Promotions,
            harness.Catalog,
            new NoCallCancelGate(),
            new FixedClock(),
            new SequentialIds(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CheckoutProcessManager>.Instance,
            inventoryReservation: harness.Reservation,
            cartConversion: harness.Conversion,
            capturedPreview: Options.Create(new StorefrontCapturedPreviewOptions
            {
                CheckoutEnabled = checkoutEnabled,
            }));
    }

    /// <summary>پایگاه دادهٔ یک‌بار مهاجرت‌شده با جدول‌های ارسال خالی (همان هدف یک‌بارمصرف ۲۳۱).</summary>
    private static async Task<OrderDbContext> ReadyContextAsync(OrderEvidenceRelationalTestTarget target)
    {
        var builder = new DbContextOptionsBuilder<OrderDbContext>();
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
            builder, target.ConnectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        var db = new OrderDbContext(builder.Options);
        try
        {
            // جدول‌های ارسال همین‌جا کافی‌اند؛ مهاجرت فقط وقتی اجرا می‌شود که هنوز ساخته نشده‌اند.
            await db.Database.ExecuteSqlRawAsync("SELECT 1 FROM \"order\".cart_shipping_drafts LIMIT 1");
        }
        catch (Npgsql.PostgresException)
        {
            await db.Database.MigrateAsync();
        }

        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"order\".cart_shipping_group_lines;"
            + "DELETE FROM \"order\".cart_shipping_groups;"
            + "DELETE FROM \"order\".cart_shipping_drafts;");
        return db;
    }
}
