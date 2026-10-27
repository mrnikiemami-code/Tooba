using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Localization.Contracts.Ports;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Contracts.Storefront;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Domain.Enums;
using Tooba.Payment.Contracts.Storefront;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// TB-P0-CHECKOUT-START-PAYMENT-COMPOSER-019 — ترکیب‌کنندهٔ کلیک نهایی پرداخت:
/// commit متعارف یک‌بار (یا بازگشت پذیری امن)، آماده‌سازی ۰۱۸، سپس یک فراخوانی درز متعارف Payment.
/// سیم‌کشی فقط پورت‌های مسیر واقعی را fake می‌کند؛ مسیرهای لمس‌نشده تست را می‌شکنند.
/// </summary>
public sealed class StorefrontStartPaymentComposerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-13T06:00:00Z", CultureInfo.InvariantCulture);
    private const string GuestSecret = "guest-secret-019";
    private const string RawKey = "final-click-019-key";
    private const string MethodCode = "post";
    private const int CartVersion = 7;

    private static string CommitKeyFor(string rawKey) => StorefrontStartPaymentComposer.CommitKeyPrefix + rawKey;

    private static string PaymentKeyFor(string rawKey) => StorefrontStartPaymentComposer.PaymentKeyPrefix + rawKey;

    [Fact]
    public async Task Final_click_commits_once_prepares_the_hold_and_initiates_the_chosen_method_once()
    {
        var harness = Harness.Create();
        var result = await harness.StartAsync();

        Assert.True(result.Started);
        Assert.Equal(StorefrontStartPaymentOutcome.Initiated, result.Outcome);
        Assert.Equal(StorefrontStartPaymentFault.None, result.Fault);
        Assert.False(result.CheckoutReplayed);
        Assert.Equal(harness.CheckoutId, result.CheckoutId);

        // (2) مسیر commit متعارف دقیقاً یک‌بار و با کلید namespace‌دار «commit».
        Assert.Equal(1, harness.Directory.SubmitCalls);
        Assert.Equal(CommitKeyFor(RawKey), harness.Directory.LastSubmitCommand!.IdempotencyKey);
        Assert.Equal(harness.CartId, harness.Directory.LastSubmitCommand.CartId);
        Assert.Equal(CartVersion, harness.Directory.LastSubmitCommand.ExpectedCartVersion);

        // قیمت ارسال authoritative بک‌اند است، نه مقدار ذخیره‌شده/جعل‌شدهٔ کلاینت.
        Assert.Equal(150_000m, harness.Directory.LastSubmitCommand.ShippingAmount);

        // (3) آماده‌سازی ۰۱۸ روی همان checkout پایدار و با روش انتخاب‌شده.
        Assert.Equal(1, harness.Preparation.Calls);
        Assert.Equal(harness.CheckoutId, harness.Preparation.LastRequest!.CheckoutId);
        Assert.Equal(harness.CartId, harness.Preparation.LastRequest.CartId);
        Assert.Equal(GuestSecret, harness.Preparation.LastRequest.GuestSecret);
        Assert.Equal(PaymentProviderCodes.Wallet, harness.Preparation.LastRequest.ProviderCode);

        // (4) درز متعارف Payment دقیقاً یک‌بار و با کلید namespace‌دار «payment».
        Assert.Equal(1, harness.Payments.Calls);
        Assert.Equal(harness.CheckoutId, harness.Payments.LastRequest!.CheckoutId);
        Assert.Equal(harness.CartId, harness.Payments.LastRequest.CartId);
        Assert.Equal(PaymentKeyFor(RawKey), harness.Payments.LastRequest.IdempotencyKey);
        Assert.True(harness.Payments.LastRequest.UseWallet);
        Assert.Equal(GuestSecret, harness.Payments.LastRequest.GuestSecret);

        Assert.NotEqual(StorefrontStartPaymentComposer.CommitKeyPrefix, StorefrontStartPaymentComposer.PaymentKeyPrefix);
        Assert.Equal(harness.PaymentId, result.PaymentId);
        Assert.Equal("PendingPayment", result.PaymentStatus);
        Assert.Equal("IRR", result.Currency);
        Assert.Equal(2_225_000m, result.Amount);
        Assert.Equal(3, result.ReservationCount);
    }

    [Fact]
    public async Task Lost_response_with_the_same_key_replays_the_checkout_without_a_second_commit()
    {
        var harness = Harness.Create(replaying: true);

        var first = await harness.StartAsync();
        var second = await harness.StartAsync();

        // هیچ commit دوباره‌ای: نه سفارش دوم، نه سهمیهٔ تازه، نه رزرو تکراری.
        Assert.Equal(0, harness.Directory.SubmitCalls);
        Assert.True(first.CheckoutReplayed);
        Assert.True(second.CheckoutReplayed);
        Assert.Equal(harness.CheckoutId, first.CheckoutId);
        Assert.Equal(harness.CheckoutId, second.CheckoutId);

        // همان کلید پایدار در هر دو مرحله و در هر دو کلیک؛ dedupe نهایی مالکیت Payment است.
        Assert.Equal(CommitKeyFor(RawKey), harness.Tracker.LastLookupKey);
        Assert.Equal(2, harness.Payments.Calls);
        Assert.All(harness.Payments.Requests, request => Assert.Equal(PaymentKeyFor(RawKey), request.IdempotencyKey));
        Assert.All(harness.Payments.Requests, request => Assert.Equal(harness.CheckoutId, request.CheckoutId));
        Assert.Equal(2, harness.Preparation.Calls);
    }

    [Theory]
    [InlineData("checkout.cart.expired")]
    [InlineData("checkout.version.conflict")]
    [InlineData("shipping.cart.stale")]
    public async Task Commit_failure_keeps_the_cart_and_runs_no_money_path(string producerCode)
    {
        var harness = Harness.Create(commitFailureCode: producerCode);

        var result = await harness.StartAsync();

        // کد تایپ‌شدهٔ تولیدکنندهٔ خطا بدون بازنویسی گزارش می‌شود (هیچ ۵۰۰ عمومی ساخته نمی‌شود).
        Assert.Equal(StorefrontStartPaymentOutcome.CheckoutCommitFailed, result.Outcome);
        Assert.Equal(producerCode, result.FailureCode);
        Assert.Null(result.CheckoutId);
        Assert.False(result.Started);

        // نه مهلتی لمس شد و نه مسیر پولی اجرا شد؛ سبد دست‌نخورده باقی می‌ماند.
        Assert.Equal(0, harness.Preparation.Calls);
        Assert.Equal(0, harness.Payments.Calls);
    }

    [Fact]
    public async Task Commit_that_returns_no_persisted_checkout_is_rejected_typed_without_money_movement()
    {
        var harness = Harness.Create(commitReturnsCheckoutId: false);

        var result = await harness.StartAsync();

        Assert.Equal(StorefrontStartPaymentOutcome.CheckoutCommitFailed, result.Outcome);
        Assert.Equal(StorefrontOrderErrors.CheckoutRejected, result.FailureCode);
        Assert.Null(result.CheckoutId);
        Assert.Equal(0, harness.Preparation.Calls);
        Assert.Equal(0, harness.Payments.Calls);
    }

    [Theory]
    [InlineData(OrderPaymentHoldPreparationOutcome.AccessDenied)]
    [InlineData(OrderPaymentHoldPreparationOutcome.ReservationSetIncomplete)]
    [InlineData(OrderPaymentHoldPreparationOutcome.CycleNotPayable)]
    [InlineData(OrderPaymentHoldPreparationOutcome.PolicyUnavailable)]
    [InlineData(OrderPaymentHoldPreparationOutcome.HoldUnavailable)]
    [InlineData(OrderPaymentHoldPreparationOutcome.DeadlineRejected)]
    [InlineData(OrderPaymentHoldPreparationOutcome.AmbientTransactionUnsupported)]
    [InlineData(OrderPaymentHoldPreparationOutcome.CycleClosedAfterExtension)]
    public async Task Preparation_failure_keeps_the_existing_checkout_and_never_calls_payment(
        OrderPaymentHoldPreparationOutcome preparationOutcome)
    {
        var harness = Harness.Create(preparationOutcome: preparationOutcome);

        var result = await harness.StartAsync();

        Assert.Equal(StorefrontStartPaymentOutcome.HoldPreparationFailed, result.Outcome);
        Assert.Equal(preparationOutcome, result.PreparationOutcome);
        Assert.Equal(harness.CheckoutId, result.CheckoutId);
        Assert.True(result.CheckoutPersisted);
        Assert.False(result.Started);
        Assert.Null(result.FailureCode);

        // سفارش موجود قابل‌بازیابی می‌ماند: نه commit دوباره، نه لغو خودکار، نه فراخوانی Payment.
        Assert.Equal(1, harness.Directory.SubmitCalls);
        Assert.Equal(1, harness.Preparation.Calls);
        Assert.Equal(0, harness.Payments.Calls);
        if (preparationOutcome == OrderPaymentHoldPreparationOutcome.HoldUnavailable)
        {
            Assert.Contains(harness.UnavailableReservationId, result.UnavailableReservationIds);
        }
    }

    [Fact]
    public async Task Selected_neutral_gateway_selector_resolves_to_the_concrete_provider_before_hold_preparation()
    {
        var gateway = Harness.Create(useWallet: false, providerCode: PaymentProviderCodes.GatewayCatalog);
        var gatewayResult = await gateway.StartAsync();
        Assert.True(gatewayResult.Started);

        // رزولوشن یک‌بار و پیش از هر لمس سفارش/مهلت/پول.
        Assert.Equal(1, gateway.Selection.Calls);
        Assert.False(gateway.Selection.LastUseWallet);
        Assert.Equal(PaymentProviderCodes.GatewayCatalog, gateway.Selection.LastRequestedSelector);

        // ۰۱۸ هرگز bare «gateway» نمی‌گیرد؛ Provider متعارف نهایی می‌گیرد.
        Assert.Equal(Harness.ConcreteGatewayProvider, gateway.Preparation.LastRequest!.ProviderCode);
        Assert.NotEqual(PaymentProviderCodes.GatewayCatalog, gateway.Preparation.LastRequest.ProviderCode);

        // Payment همان انتخاب‌گر اصلی را می‌گیرد تا بررسی صلاحیت معتبر در مالکیت Payment بماند.
        Assert.Equal(PaymentProviderCodes.GatewayCatalog, gateway.Payments.LastRequest!.ProviderCode);
        Assert.False(gateway.Payments.LastRequest.UseWallet);
    }

    [Fact]
    public async Task Unresolvable_selector_fails_closed_before_any_commit_hold_or_payment()
    {
        var harness = Harness.Create(
            useWallet: false,
            providerCode: "unknown-provider",
            providerSelectionResolvable: false);

        var result = await harness.StartAsync();

        Assert.Equal(StorefrontStartPaymentOutcome.InvalidRequest, result.Outcome);
        Assert.Equal(StorefrontStartPaymentFault.ProviderUnavailable, result.Fault);
        Assert.Null(result.CheckoutId);
        Assert.False(result.Started);

        // رزولوشن پیش از commit است: هیچ سفارش/مهلت/پولی لمس نشد.
        Assert.Equal(1, harness.Selection.Calls);
        Assert.Equal(0, harness.Directory.SubmitCalls);
        Assert.Equal(0, harness.Preparation.Calls);
        Assert.Equal(0, harness.Payments.Calls);
    }

    [Fact]
    public async Task Selected_method_is_forwarded_to_preparation_unchanged_and_wallet_is_mapped_to_its_stable_code()
    {
        var manual = Harness.Create(useWallet: false, providerCode: PaymentProviderCodes.Manual);
        var manualResult = await manual.StartAsync();
        Assert.True(manualResult.Started);
        Assert.Equal(PaymentProviderCodes.Manual, manual.Preparation.LastRequest!.ProviderCode);
        Assert.Equal(PaymentProviderCodes.Manual, manual.Payments.LastRequest!.ProviderCode);
        Assert.False(manual.Payments.LastRequest.UseWallet);

        var wallet = Harness.Create(useWallet: true, providerCode: null);
        var walletResult = await wallet.StartAsync();
        Assert.True(walletResult.Started);
        Assert.Equal(PaymentProviderCodes.Wallet, wallet.Preparation.LastRequest!.ProviderCode);
        Assert.True(wallet.Payments.LastRequest!.UseWallet);
    }

    [Fact]
    public async Task Authenticated_caller_identity_is_forwarded_to_preparation_and_payment()
    {
        var userId = Guid.NewGuid();
        var harness = Harness.Create(authenticatedUserId: userId);

        var result = await harness.StartAsync();

        Assert.True(result.Started);
        Assert.Equal(userId, harness.Preparation.LastRequest!.AuthenticatedUserId);
        Assert.Equal(userId, harness.Payments.LastRequest!.AuthenticatedUserId);
        Assert.Null(harness.Preparation.LastRequest.GuestSecret);
    }

    [Fact]
    public async Task Guest_mismatch_on_a_replay_fails_closed_before_any_commit_or_payment()
    {
        var harness = Harness.Create(replaying: true, replayOwnershipGranted: false);

        var result = await harness.StartAsync();

        Assert.Equal(StorefrontStartPaymentOutcome.CheckoutCommitFailed, result.Outcome);
        Assert.Equal(StorefrontOrderErrors.CheckoutAccessDenied, result.FailureCode);
        Assert.False(result.CheckoutReplayed);
        Assert.Equal(0, harness.Directory.SubmitCalls);
        Assert.Equal(0, harness.Preparation.Calls);
        Assert.Equal(0, harness.Payments.Calls);
    }

    [Fact]
    public async Task Preparation_access_denied_for_a_foreign_actor_never_starts_payment()
    {
        var harness = Harness.Create(
            authenticatedUserId: Guid.NewGuid(),
            preparationOutcome: OrderPaymentHoldPreparationOutcome.AccessDenied);

        var result = await harness.StartAsync();

        Assert.Equal(StorefrontStartPaymentOutcome.HoldPreparationFailed, result.Outcome);
        Assert.Equal(OrderPaymentHoldPreparationOutcome.AccessDenied, result.PreparationOutcome);
        Assert.Equal(0, harness.Payments.Calls);
    }

    [Fact]
    public async Task Payment_faults_propagate_untouched_so_no_payment_code_is_forged_and_the_checkout_stays()
    {
        var harness = Harness.Create(paymentFailureCode: "payment.method.unavailable");

        var exception = await Assert.ThrowsAsync<ContractOperationException>(() => harness.StartAsync());

        Assert.Equal("payment.method.unavailable", exception.Code);
        Assert.Equal(1, harness.Directory.SubmitCalls);
        Assert.Equal(1, harness.Preparation.Calls);
        Assert.Equal(1, harness.Payments.Calls);
    }

    [Fact]
    public async Task Incomplete_request_is_rejected_before_any_order_or_money_touch()
    {
        var harness = Harness.Create();

        var noCart = await harness.Composer.StartPaymentAsync(
            harness.Request() with { CartId = Guid.Empty },
            CancellationToken.None);
        var noKey = await harness.Composer.StartPaymentAsync(
            harness.Request() with { IdempotencyKey = "   " },
            CancellationToken.None);

        Assert.Equal(StorefrontStartPaymentOutcome.InvalidRequest, noCart.Outcome);
        Assert.Equal(StorefrontStartPaymentFault.CartRequired, noCart.Fault);
        Assert.Equal(StorefrontStartPaymentOutcome.InvalidRequest, noKey.Outcome);
        Assert.Equal(StorefrontStartPaymentFault.IdempotencyKeyRequired, noKey.Fault);
        Assert.Equal(0, harness.Directory.SubmitCalls);
        Assert.Equal(0, harness.Preparation.Calls);
        Assert.Equal(0, harness.Payments.Calls);
    }

    [Fact]
    public async Task Key_length_compatibility_is_checked_explicitly_against_the_shared_column_limit()
    {
        var harness = Harness.Create();

        // ۱۰۶ کاراکتر + namespace «commit» (۲۱) = ۱۲۷ و namespace «payment» (۲۲) = ۱۲۸ → مجاز.
        var longestAccepted = new string('k', 106);
        var accepted = await harness.Composer.StartPaymentAsync(
            harness.Request() with { IdempotencyKey = longestAccepted },
            CancellationToken.None);

        Assert.True(accepted.Started);
        Assert.Equal(CommitKeyFor(longestAccepted), harness.Directory.LastSubmitCommand!.IdempotencyKey);
        Assert.Equal(PaymentKeyFor(longestAccepted), harness.Payments.LastRequest!.IdempotencyKey);
        Assert.Equal(127, harness.Directory.LastSubmitCommand.IdempotencyKey.Length);
        Assert.Equal(128, harness.Payments.LastRequest.IdempotencyKey.Length);

        // یک کاراکتر بیشتر از سقف ستون‌ها → رد پیش از هر commit/مهلت/پرداخت.
        var tooLong = new string('k', 107);
        var rejected = await harness.Composer.StartPaymentAsync(
            harness.Request() with { IdempotencyKey = tooLong },
            CancellationToken.None);

        Assert.Equal(StorefrontStartPaymentOutcome.InvalidRequest, rejected.Outcome);
        Assert.Equal(StorefrontStartPaymentFault.IdempotencyKeyTooLong, rejected.Fault);
        Assert.Equal(1, harness.Directory.SubmitCalls);
        Assert.Equal(1, harness.Payments.Calls);
    }

    [Fact]
    public async Task Composer_without_a_process_tracker_falls_back_to_the_canonical_commit_replay_only()
    {
        var harness = Harness.Create(withoutTracker: true);

        var result = await harness.StartAsync();

        Assert.True(result.Started);
        Assert.False(result.CheckoutReplayed);
        Assert.Equal(1, harness.Directory.SubmitCalls);
    }

    /// <summary>سیم‌کشی مسیر واقعی: commit متعارف با پورت‌های fake، بقیه رفتارها ثبت‌کننده.</summary>
    private sealed class Harness
    {
        public const string ConcreteGatewayProvider = "concrete-gateway-provider";

        public required Guid CartId { get; init; }
        public required Guid CheckoutId { get; init; }
        public required Guid PaymentId { get; init; }
        public required Guid UnavailableReservationId { get; init; }
        public required StorefrontStartPaymentComposer Composer { get; init; }
        public required DirectoryFake Directory { get; init; }
        public required TrackerFake Tracker { get; init; }
        public required PreparationFake Preparation { get; init; }
        public required PaymentsFake Payments { get; init; }
        public required ProviderSelectionFake Selection { get; init; }
        public required Guid? AuthenticatedUserId { get; init; }

        public Task<StorefrontStartPaymentResult> StartAsync() =>
            Composer.StartPaymentAsync(Request(), CancellationToken.None);

        public StorefrontStartPaymentRequest Request() =>
            new(
                CartId,
                AuthenticatedUserId is null ? GuestSecret : null,
                CartVersion,
                RawKey,
                UseWallet,
                ProviderCode,
                AuthenticatedUserId);

        public required bool UseWallet { get; init; }
        public required string? ProviderCode { get; init; }

        public static Harness Create(
            bool replaying = false,
            bool replayOwnershipGranted = true,
            bool commitReturnsCheckoutId = true,
            string? commitFailureCode = null,
            string? paymentFailureCode = null,
            OrderPaymentHoldPreparationOutcome preparationOutcome = OrderPaymentHoldPreparationOutcome.Prepared,
            bool useWallet = true,
            string? providerCode = null,
            bool providerSelectionResolvable = true,
            Guid? authenticatedUserId = null,
            bool withoutTracker = false)
        {
            var cartId = Guid.NewGuid();
            var checkoutId = Guid.NewGuid();
            var directory = new DirectoryFake(cartId, checkoutId, commitReturnsCheckoutId, commitFailureCode);
            var drafts = new DraftStoreFake(Draft(cartId));
            var actor = new ActorFake(authenticatedUserId);
            var carts = new CartsFake(cartId, replayOwnershipGranted);
            var checkout = new StorefrontCheckoutService(carts, directory, addresses: null!, actor);
            var shipping = new StorefrontShippingService(
                carts,
                checkout,
                addresses: null!,
                shippingCatalog: new ShippingCatalogFake(),
                oneLevelLabels: new OneLevelShippingLabelFake(),
                drafts,
                languages: new LanguagesFake(),
                shippingOptions: new ShippingMethodsOptions(),
                shippingSeed: new ShippingSeedFake(),
                actor,
                clock: new FixedClock(Now));
            var tracker = new TrackerFake(
                replaying ? SucceededProcess(checkoutId, cartId, CommitKeyFor(RawKey)) : null);
            var preparation = new PreparationFake(checkoutId, preparationOutcome);
            var payments = new PaymentsFake(checkoutId, paymentFailureCode);
            var selection = new ProviderSelectionFake(ConcreteGatewayProvider, providerSelectionResolvable);
            return new Harness
            {
                CartId = cartId,
                CheckoutId = checkoutId,
                PaymentId = payments.PaymentId,
                UnavailableReservationId = preparation.UnavailableReservationId,
                Composer = new StorefrontStartPaymentComposer(
                    shipping,
                    checkout,
                    preparation,
                    payments,
                    selection,
                    withoutTracker ? null : tracker),
                Directory = directory,
                Tracker = tracker,
                Preparation = preparation,
                Payments = payments,
                Selection = selection,
                UseWallet = useWallet,
                ProviderCode = providerCode,
                AuthenticatedUserId = authenticatedUserId,
            };
        }

        private static CheckoutProcess SucceededProcess(Guid checkoutId, Guid cartId, string key)
        {
            var process = CheckoutProcess.Start(Guid.NewGuid(), key, cartId, $"checkout-process:{key}", Now);
            process.MarkValidating(Now);
            process.MarkInventoryReserving(Now);
            process.MarkOrderPersisting(checkoutId, Now);
            process.MarkCartCommitting(Now);
            process.MarkPaymentPending(Now);
            return process;
        }
    }

    private static string HashGuestSecret(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret.Trim())));

    private static CartShippingDraft Draft(Guid cartId)
    {
        var draft = CartShippingDraft.Create(
            cartId,
            HashGuestSecret(GuestSecret),
            CartVersion,
            "گیرنده تست",
            "09120000000",
            "تهران",
            "ایران‌شهر",
            "خیابان تست پلاک ۱",
            "1234567890",
            null,
            MethodCode,
            "پست",
            45_000m,
            new DateOnly(2026, 10, 17),
            new DateOnly(2026, 10, 17),
            "12-15",
            "یادداشت مشتری",
            Now);
        draft.ApplyRecipientNames("حمید", "تستی");
        return draft;
    }

    private sealed class CartsFake(Guid cartId, bool ownershipGranted) : ICartPresentationGateway
    {
        public Task<CartPage?> GetAsync(Guid id, string? guestSecret, CancellationToken cancellationToken) =>
            Task.FromResult(id == cartId ? Cart(id) : null);

        public Task<CartPage?> TryGetForOwnershipAsync(Guid id, string? guestSecret, CancellationToken cancellationToken) =>
            Task.FromResult(ownershipGranted && id == cartId ? Cart(id) : null);

        private static CartPage Cart(Guid id) =>
            new(
                id,
                CartVersion,
                "IR",
                "IRR",
                "Marketplace",
                2m,
                [new CartCurrencyTotal("IRR", 2_000_000m)],
                [
                    new CartLineView(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        null,
                        null,
                        "کالای تست",
                        "فروشندهٔ تست",
                        null,
                        2m,
                        1_000_000m,
                        2_000_000m,
                        "IRR",
                        false),
                ],
                null,
                "Active");
    }

    private sealed class DraftStoreFake(CartShippingDraft draft) : IStorefrontShippingDraftStore
    {
        public Task<CartShippingDraft?> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken) =>
            Task.FromResult(draft.CartId == cartId ? draft : null);

        public Task SaveAsync(CartShippingDraft value, bool isNew, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<CartShippingDraft?> GetForCapturedSelectionAsync(Guid cartId, CancellationToken cancellationToken) =>
            GetByCartIdAsync(cartId, cancellationToken);

        public Task<CartShippingDraft> ReplaceCapturedSelectionAsync(
            CartShippingDraft value,
            IReadOnlyList<CartShippingDraftGroup> groups,
            bool isNew,
            long expectedSelectionRevision,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class ActorFake(Guid? authenticatedUserId) : IOrderStorefrontActor
    {
        public Guid GuestActorId { get; } = Guid.NewGuid();

        public bool IsAuthenticated => authenticatedUserId is not null;

        public Guid? AuthenticatedUserId => authenticatedUserId;

        public Guid ResolvePlacementActor(bool usingSavedAddress) =>
            authenticatedUserId ?? GuestActorId;

        public Guid? TryResolveListActor() => null;

        public CartAccess BuildCartAccess(string? guestSecret) => new(null, guestSecret);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class ShippingSeedFake : IShippingCatalogSeedPort
    {
        public Task EnsureSeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class LanguagesFake : ILanguageLookup
    {
        public Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LanguageLookupSnapshot>>([]);
    }

    private sealed class ShippingCatalogFake : IShippingCatalogReader
    {
        public Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingCatalogServiceSnapshot>>(
            [
                new ShippingCatalogServiceSnapshot(
                    Guid.NewGuid(),
                    MethodCode,
                    ProviderKind: "post",
                    IconKey: "post",
                    ColorKey: "#000",
                    IsActive: true,
                    SortOrder: 0,
                    Translations: [],
                    Options: []),
            ]);

        public Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class TrackerFake(CheckoutProcess? process) : ICheckoutProcessTracker
    {
        public string? LastLookupKey { get; private set; }

        public Task<CheckoutProcess?> FindSucceededBySubmissionKeyAsync(
            string submissionIdempotencyKey,
            CancellationToken cancellationToken)
        {
            LastLookupKey = submissionIdempotencyKey;
            return Task.FromResult(process);
        }

        public Task<CheckoutProcess> BeginAsync(
            string submissionIdempotencyKey,
            Guid cartId,
            DateTimeOffset now,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public void MarkValidating(CheckoutProcess process, DateTimeOffset now) => throw new NotSupportedException();

        public void MarkInventoryReserving(CheckoutProcess process, DateTimeOffset now) => throw new NotSupportedException();

        public void MarkOrderPersisting(CheckoutProcess process, Guid checkoutId, DateTimeOffset now) => throw new NotSupportedException();

        public void MarkCartCommitting(CheckoutProcess process, DateTimeOffset now) => throw new NotSupportedException();

        public void MarkPaymentPending(CheckoutProcess process, DateTimeOffset now) => throw new NotSupportedException();

        public void MarkFailed(CheckoutProcess process, string failureCode, DateTimeOffset now) => throw new NotSupportedException();
    }

    /// <summary>آینهٔ دقیق قاعدهٔ رزولوشن: wallet/manual پایدار، bare «gateway» به Provider متعارف، بقیه رد.</summary>
    private sealed class ProviderSelectionFake(string concreteGatewayProvider, bool resolvable)
        : IPaymentProviderSelectionPort
    {
        public int Calls { get; private set; }

        public string? LastRequestedSelector { get; private set; }

        public bool? LastUseWallet { get; private set; }

        public Task<StorefrontProviderSelection?> ResolveAsync(
            string? requestedProviderCode,
            bool useWallet,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastRequestedSelector = requestedProviderCode;
            LastUseWallet = useWallet;
            if (!resolvable)
            {
                return Task.FromResult<StorefrontProviderSelection?>(null);
            }

            var selector = requestedProviderCode?.Trim() ?? string.Empty;
            if (useWallet)
            {
                return Task.FromResult<StorefrontProviderSelection?>(
                    new StorefrontProviderSelection(selector, PaymentProviderCodes.Wallet));
            }

            if (PaymentProviderCodes.IsManual(selector))
            {
                return Task.FromResult<StorefrontProviderSelection?>(
                    new StorefrontProviderSelection(selector, PaymentProviderCodes.Manual));
            }

            if (string.Equals(selector, PaymentProviderCodes.GatewayCatalog, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<StorefrontProviderSelection?>(
                    new StorefrontProviderSelection(selector, concreteGatewayProvider));
            }

            return Task.FromResult<StorefrontProviderSelection?>(null);
        }
    }

    private sealed class PreparationFake(Guid checkoutId, OrderPaymentHoldPreparationOutcome outcome)
        : IOrderPaymentHoldPreparationPort
    {
        public Guid UnavailableReservationId { get; } = Guid.NewGuid();

        public int Calls { get; private set; }

        public OrderPaymentHoldPreparationRequest? LastRequest { get; private set; }

        public Task<OrderPaymentHoldPreparationResult> PrepareAsync(
            OrderPaymentHoldPreparationRequest request,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastRequest = request;
            var deadline = Now.AddHours(2);
            return Task.FromResult(new OrderPaymentHoldPreparationResult(
                outcome,
                checkoutId,
                request.ProviderCode,
                ReservationCount: 3,
                RequestedDeadlineUtc: deadline,
                GuaranteedUntilUtc: deadline,
                CycleNumber: 1,
                CycleExpiresAtUtc: deadline,
                outcome is OrderPaymentHoldPreparationOutcome.HoldUnavailable
                    ? [UnavailableReservationId]
                    : []));
        }
    }

    private sealed class PaymentsFake(Guid checkoutId, string? failureCode) : IPaymentInitiationPort
    {
        private readonly List<StorefrontPaymentInitiationRequest> _requests = [];

        public Guid PaymentId { get; } = Guid.NewGuid();

        public int Calls => _requests.Count;

        public IReadOnlyList<StorefrontPaymentInitiationRequest> Requests => _requests;

        public StorefrontPaymentInitiationRequest? LastRequest => _requests.Count == 0 ? null : _requests[^1];

        public Task<StorefrontPaymentInitiationSnapshot> InitiateForCheckoutAsync(
            StorefrontPaymentInitiationRequest request,
            CancellationToken cancellationToken)
        {
            _requests.Add(request);
            if (failureCode is not null)
            {
                throw new ContractOperationException(failureCode);
            }

            return Task.FromResult(new StorefrontPaymentInitiationSnapshot(
                PaymentId,
                Guid.NewGuid(),
                checkoutId,
                "PendingPayment",
                request.UseWallet ? PaymentProviderCodes.Wallet : request.ProviderCode ?? PaymentProviderCodes.Manual,
                "provider-ref-019",
                "https://gateway.test/redirect",
                2_225_000m,
                "IRR",
                RequiresPspRedirect: true));
        }
    }

    /// <summary>هر mutation خارج از SubmitAsync باید تست را بشکند (بدون رزرو/تبدیل سبد/نوشتن سفارش دوم).</summary>
    private sealed class DirectoryFake(
        Guid cartId,
        Guid checkoutId,
        bool commitReturnsCheckoutId,
        string? commitFailureCode) : ICheckoutDirectory
    {
        public int SubmitCalls { get; private set; }

        public SubmitCheckoutCommand? LastSubmitCommand { get; private set; }

        public Task<CheckoutSnapshot> PreviewAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CheckoutSnapshot> SubmitAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken)
        {
            SubmitCalls++;
            LastSubmitCommand = command;
            if (commitFailureCode is not null)
            {
                // کد تایپ‌شدهٔ همان تولیدکنندهٔ متعارف Order/Inventory بدون بازنویسی عبور می‌کند.
                throw new ContractOperationException(commitFailureCode);
            }

            return Task.FromResult(Snapshot(commitReturnsCheckoutId ? checkoutId : Guid.Empty, command));
        }

        public Task<CheckoutSnapshot?> GetCheckoutAsync(
            Guid requestedCheckoutId,
            OrderAccess access,
            CancellationToken cancellationToken) =>
            Task.FromResult<CheckoutSnapshot?>(
                requestedCheckoutId == checkoutId ? Snapshot(checkoutId, null) : null);

        public Task<SellerOrderSnapshot?> GetSellerOrderByNumberAsync(
            string orderNumber,
            OrderAccess access,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task CancelSellerOrderAsync(Guid sellerOrderId, OrderAccess access, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task RestoreCancelledCheckoutAsync(Guid checkoutIdToRestore, OrderAccess access, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<CheckoutOperationalNoteSnapshot>> ListNotesAsync(
            Guid requestedCheckoutId,
            Guid viewerUserId,
            int take,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CheckoutOperationalNoteSnapshot> AddNoteAsync(
            Guid requestedCheckoutId,
            Guid actorUserId,
            string body,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CheckoutNoteDeleteOutcome> DeleteNoteAsync(
            Guid requestedCheckoutId,
            Guid noteId,
            Guid actorUserId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RecordAdminViewAsync(
            Guid requestedCheckoutId,
            Guid viewerUserId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        private CheckoutSnapshot Snapshot(Guid snapshotCheckoutId, SubmitCheckoutCommand? command) =>
            new(
                snapshotCheckoutId,
                cartId,
                command?.Mode ?? OrderMode.OnlinePurchase,
                command?.BuyerPartyId,
                command?.PlacedByUserId ?? Guid.Empty,
                "IR",
                "IRR",
                SalesChannel.Marketplace,
                Now,
                [
                    new SellerOrderSnapshot(
                        Guid.NewGuid(),
                        "SO-019",
                        Guid.NewGuid(),
                        SellerOrderStatus.Submitted,
                        2_000_000m,
                        180_000m,
                        0m,
                        2_180_000m,
                        "IRR",
                        []),
                ],
                RecipientName: command?.RecipientName ?? "گیرنده تست",
                ContactMobile: command?.ContactMobile ?? "09120000000",
                ProvinceName: command?.ProvinceName ?? "تهران",
                CityName: command?.CityName ?? "ایران‌شهر",
                PostalAddress: command?.PostalAddress ?? "خیابان تست پلاک ۱",
                PostalCode: command?.PostalCode ?? "1234567890",
                ShippingMethodCode: command?.ShippingMethodCode ?? MethodCode,
                ShippingMethodLabel: command?.ShippingMethodLabel ?? "پست",
                ShippingAmount: command?.ShippingAmount ?? 45_000m,
                MinimumDeliveryDate: command?.MinimumDeliveryDate,
                RequestedDeliveryDate: command?.RequestedDeliveryDate,
                RequestedDeliveryTimeWindow: command?.RequestedDeliveryTimeWindow,
                CustomerNote: command?.CustomerNote);
    }
}
