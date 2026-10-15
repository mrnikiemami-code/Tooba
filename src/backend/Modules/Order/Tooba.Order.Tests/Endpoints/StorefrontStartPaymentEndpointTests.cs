using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Localization.Contracts.Ports;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Contracts.Storefront;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Domain.Enums;
using Tooba.Order.Endpoints;
using Tooba.Order.Endpoints.Errors;
using Tooba.Order.Endpoints.Resources;
using Tooba.Payment.Contracts.Errors;
using Tooba.Payment.Contracts.Storefront;
using Xunit;

namespace Tooba.Order.Tests.Endpoints;

/// <summary>
/// TB-P0-CHECKOUT-FINAL-CLICK-API-028 — اثبات قرارداد مسیر عمومی «کلیک نهایی پرداخت»:
/// هویت/گواه از مرز اعتماد، رد بدنهٔ ناقص/مبهم پیش از هر commit، نگاشت شکست‌ها به کدهای ثبت‌شدهٔ ۴xx
/// مالک واقعی، بازگشت مرجع سفارش، و replay با همان کلید/مالکیت. مسیر واقعی با کامپوزر واقعی و پورت‌های
/// fake اجرا می‌شود؛ هیچ DB/PSP واقعی لمس نمی‌شود و هیچ جای Host تغییر نمی‌کند.
/// </summary>
public sealed class StorefrontStartPaymentEndpointTests
{
    private static readonly DateTimeOffset Now =
        DateTimeOffset.Parse("2026-10-13T06:00:00Z", CultureInfo.InvariantCulture);

    private const string GuestSecret = "guest-secret-028";
    private const string RawKey = "final-click-028-key";
    private const string MethodCode = "post";
    private const int CartVersion = 7;

    private const string EndpointsTypeName =
        "Tooba.Order.Endpoints.Storefront.StorefrontStartPaymentEndpoints";

    [Fact]
    public async Task Guest_final_click_returns_checkout_and_payment_references_and_aligns_the_hold_provider()
    {
        using var harness = Harness.Create(providerCode: PaymentProviderCodes.GatewayCatalog);

        var (http, result) = await harness.InvokeAsync(GuestSecret);
        await result.ExecuteAsync(http);

        Assert.Equal(StatusCodes.Status200OK, http.Response.StatusCode);
        var json = await ReadJsonAsync(http);
        Assert.Equal(harness.CheckoutId, json.GetProperty("checkoutId").GetGuid());
        Assert.Equal(harness.PaymentId, json.GetProperty("paymentId").GetGuid());
        Assert.Equal("PendingPayment", json.GetProperty("paymentStatus").GetString());
        Assert.Equal("IRR", json.GetProperty("currency").GetString());

        // هویت/گواه مهمان از هدر متعارف به commit و آماده‌سازی رسیده و commit دقیقاً یک‌بار بوده است.
        Assert.Equal(1, harness.Gate.Invocations);
        Assert.Equal(1, harness.Directory.SubmitCalls);
        Assert.Equal(GuestSecret, harness.Preparation.LastRequest!.GuestSecret);

        // ۰۱۸ هرگز انتخاب‌گر خنثی «gateway» نمی‌گیرد؛ Payment همان انتخاب‌گر اصلی را می‌گیرد.
        Assert.Equal(Harness.ConcreteGatewayProvider, harness.Preparation.LastRequest.ProviderCode);
        Assert.Equal(PaymentProviderCodes.GatewayCatalog, harness.Payments.LastRequest!.ProviderCode);
        Assert.Equal(1, harness.Preparation.Calls);
        Assert.Equal(1, harness.Payments.Calls);
    }

    [Fact]
    public async Task Guest_secret_is_also_accepted_from_the_canonical_cookie()
    {
        using var harness = Harness.Create();

        var (http, result) = await harness.InvokeAsync(guestSecret: null, guestSecretCookie: GuestSecret);
        await result.ExecuteAsync(http);

        Assert.Equal(StatusCodes.Status200OK, http.Response.StatusCode);
        Assert.Equal(GuestSecret, harness.Preparation.LastRequest!.GuestSecret);
    }

    [Fact]
    public async Task Authenticated_identity_comes_only_from_the_trusted_actor_seam()
    {
        var userId = Guid.NewGuid();
        using var harness = Harness.Create(authenticatedUserId: userId);

        var (http, result) = await harness.InvokeAsync(guestSecret: null);
        await result.ExecuteAsync(http);

        Assert.Equal(StatusCodes.Status200OK, http.Response.StatusCode);
        Assert.Equal(1, harness.Gate.Invocations);
        Assert.Equal(userId, harness.Preparation.LastRequest!.AuthenticatedUserId);
        Assert.Null(harness.Preparation.LastRequest.GuestSecret);
        Assert.Equal(userId, harness.Payments.LastRequest!.AuthenticatedUserId);
    }

    [Fact]
    public async Task Unresolvable_payment_selector_is_rejected_with_the_registered_4xx_and_touches_nothing()
    {
        using var harness = Harness.Create(providerCode: "unknown-provider", providerSelectionResolvable: false);

        var (http, result) = await harness.InvokeAsync(GuestSecret);
        await result.ExecuteAsync(http);

        Assert.Equal(StatusCodes.Status400BadRequest, http.Response.StatusCode);
        var json = await ReadJsonAsync(http);
        Assert.Equal(PaymentErrorCodes.MethodUnavailable, json.GetProperty("errorCode").GetString());
        Assert.False(json.TryGetProperty("checkoutId", out _));

        // هیچ commit/مهلت/پولی لمس نشد و سبد Active می‌ماند.
        Assert.Equal(0, harness.Directory.SubmitCalls);
        Assert.Equal(0, harness.Preparation.Calls);
        Assert.Equal(0, harness.Payments.Calls);
    }

    [Fact]
    public async Task Missing_idempotency_key_is_rejected_with_a_registered_4xx_before_any_commit()
    {
        using var harness = Harness.Create();

        var (http, result) = await harness.InvokeAsync(GuestSecret, idempotencyKey: null, withHeaderKey: false);
        await result.ExecuteAsync(http);

        Assert.Equal(StatusCodes.Status400BadRequest, http.Response.StatusCode);
        var json = await ReadJsonAsync(http);
        Assert.Equal(StorefrontOrderErrors.CheckoutRejected, json.GetProperty("errorCode").GetString());
        Assert.Equal(0, harness.Directory.SubmitCalls);
        Assert.Equal(0, harness.Payments.Calls);
    }

    [Fact]
    public async Task Idempotency_key_is_read_from_the_stable_header_when_the_body_omits_it()
    {
        using var harness = Harness.Create();

        var (http, result) = await harness.InvokeAsync(GuestSecret, idempotencyKey: null, withHeaderKey: true);
        await result.ExecuteAsync(http);

        Assert.Equal(StatusCodes.Status200OK, http.Response.StatusCode);
        Assert.Equal(
            StorefrontStartPaymentComposer.PaymentKeyPrefix + RawKey,
            harness.Payments.LastRequest!.IdempotencyKey);
    }

    [Fact]
    public async Task Absent_body_is_rejected_without_committing_the_cart()
    {
        using var harness = Harness.Create();

        var (http, result) = await harness.InvokeAsync(GuestSecret, absentBody: true);
        await result.ExecuteAsync(http);

        Assert.Equal(StatusCodes.Status400BadRequest, http.Response.StatusCode);
        var json = await ReadJsonAsync(http);
        Assert.Equal(StorefrontOrderErrors.CheckoutRejected, json.GetProperty("errorCode").GetString());
        Assert.Equal(0, harness.Directory.SubmitCalls);
        Assert.Equal(0, harness.Payments.Calls);
    }

    [Fact]
    public async Task Empty_cart_route_identity_is_rejected_and_never_committed()
    {
        using var harness = Harness.Create();

        var (http, result) = await harness.InvokeAsync(GuestSecret, cartId: Guid.Empty);
        await result.ExecuteAsync(http);

        Assert.Equal(StatusCodes.Status404NotFound, http.Response.StatusCode);
        var json = await ReadJsonAsync(http);
        Assert.Equal(StorefrontOrderErrors.CheckoutCartMissing, json.GetProperty("errorCode").GetString());
        Assert.Equal(0, harness.Directory.SubmitCalls);
        Assert.Equal(0, harness.Payments.Calls);
    }

    [Theory]
    [InlineData(OrderPaymentHoldPreparationOutcome.AccessDenied, StatusCodes.Status403Forbidden,
        StorefrontOrderErrors.CheckoutAccessDenied)]
    [InlineData(OrderPaymentHoldPreparationOutcome.ReservationSetIncomplete, StatusCodes.Status409Conflict,
        StorefrontOrderErrors.CheckoutInventoryUnavailable)]
    [InlineData(OrderPaymentHoldPreparationOutcome.HoldUnavailable, StatusCodes.Status409Conflict,
        StorefrontOrderErrors.CheckoutInventoryUnavailable)]
    [InlineData(OrderPaymentHoldPreparationOutcome.DeadlineRejected, StatusCodes.Status409Conflict,
        StorefrontOrderErrors.CheckoutInventoryUnavailable)]
    [InlineData(OrderPaymentHoldPreparationOutcome.CycleNotPayable, StatusCodes.Status409Conflict,
        PaymentErrorCodes.ReservationNotPayable)]
    [InlineData(OrderPaymentHoldPreparationOutcome.CycleClosedAfterExtension, StatusCodes.Status409Conflict,
        PaymentErrorCodes.ReservationNotPayable)]
    [InlineData(OrderPaymentHoldPreparationOutcome.PolicyUnavailable, StatusCodes.Status409Conflict,
        PaymentErrorCodes.ReservationNotPayable)]
    public async Task Hold_preparation_failure_maps_to_a_registered_4xx_and_keeps_the_checkout_reference(
        OrderPaymentHoldPreparationOutcome outcome,
        int expectedStatus,
        string expectedCode)
    {
        using var harness = Harness.Create(preparationOutcome: outcome);

        var (http, result) = await harness.InvokeAsync(GuestSecret);
        await result.ExecuteAsync(http);

        Assert.Equal(expectedStatus, http.Response.StatusCode);
        var json = await ReadJsonAsync(http);
        Assert.Equal(expectedCode, json.GetProperty("errorCode").GetString());

        // سفارش موجود قابل‌بازیابی می‌ماند: مرجع آن در پاسخ و هیچ مسیر پولی اجرا نشد.
        Assert.Equal(harness.CheckoutId, json.GetProperty("checkoutId").GetGuid());
        Assert.Equal(1, harness.Directory.SubmitCalls);
        Assert.Equal(0, harness.Payments.Calls);
    }

    [Fact]
    public async Task Ambient_transaction_outcome_is_a_server_invariant_not_a_client_4xx()
    {
        using var harness = Harness.Create(
            preparationOutcome: OrderPaymentHoldPreparationOutcome.AmbientTransactionUnsupported);

        var (http, result) = await harness.InvokeAsync(GuestSecret);
        await result.ExecuteAsync(http);

        Assert.Equal(StatusCodes.Status500InternalServerError, http.Response.StatusCode);
        var json = await ReadJsonAsync(http);
        Assert.Equal("platform.error", json.GetProperty("errorCode").GetString());
        Assert.NotEqual("platform.unexpected", json.GetProperty("errorCode").GetString());
        Assert.Equal(0, harness.Payments.Calls);
    }

    [Fact]
    public async Task Commit_failure_keeps_the_producer_typed_code_and_runs_no_money_path()
    {
        using var harness = Harness.Create(commitFailureCode: "checkout.version.conflict");

        var (http, result) = await harness.InvokeAsync(GuestSecret);
        await result.ExecuteAsync(http);

        Assert.Equal(StatusCodes.Status409Conflict, http.Response.StatusCode);
        var json = await ReadJsonAsync(http);
        Assert.Equal("checkout.version.conflict", json.GetProperty("errorCode").GetString());
        Assert.Equal(0, harness.Preparation.Calls);
        Assert.Equal(0, harness.Payments.Calls);
    }

    [Fact]
    public async Task Replay_with_the_same_key_reuses_the_same_checkout_keys_and_ownership_without_a_second_commit()
    {
        using var harness = Harness.Create(replaying: true);

        var (firstHttp, first) = await harness.InvokeAsync(GuestSecret);
        await first.ExecuteAsync(firstHttp);
        var (secondHttp, second) = await harness.InvokeAsync(GuestSecret);
        await second.ExecuteAsync(secondHttp);

        Assert.Equal(StatusCodes.Status200OK, firstHttp.Response.StatusCode);
        Assert.Equal(StatusCodes.Status200OK, secondHttp.Response.StatusCode);
        Assert.Equal(0, harness.Directory.SubmitCalls);
        Assert.True((await ReadJsonAsync(firstHttp)).GetProperty("checkoutReplayed").GetBoolean());
        Assert.Equal(harness.CheckoutId, (await ReadJsonAsync(secondHttp)).GetProperty("checkoutId").GetGuid());

        // همان کلید پایدار در هر دو کلیک؛ dedupe نهایی مالکیت Payment است.
        Assert.All(
            harness.Payments.Requests,
            request => Assert.Equal(
                StorefrontStartPaymentComposer.PaymentKeyPrefix + RawKey,
                request.IdempotencyKey));
    }

    [Fact]
    public async Task Replay_ownership_mismatch_fails_closed_with_the_registered_4xx_and_no_payment()
    {
        using var harness = Harness.Create(replaying: true, replayOwnershipGranted: false);

        var (http, result) = await harness.InvokeAsync(GuestSecret);
        await result.ExecuteAsync(http);

        Assert.Equal(StatusCodes.Status403Forbidden, http.Response.StatusCode);
        var json = await ReadJsonAsync(http);
        Assert.Equal(StorefrontOrderErrors.CheckoutAccessDenied, json.GetProperty("errorCode").GetString());
        Assert.Equal(0, harness.Directory.SubmitCalls);
        Assert.Equal(0, harness.Payments.Calls);
    }

    [Fact]
    public async Task Payment_faults_propagate_untouched_so_no_payment_code_is_forged()
    {
        using var harness = Harness.Create(paymentFailureCode: "payment.method.unavailable");

        var exception = await Assert.ThrowsAsync<ContractOperationException>(
            () => harness.InvokeAsync(GuestSecret));

        Assert.Equal("payment.method.unavailable", exception.Code);
        Assert.Equal(1, harness.Directory.SubmitCalls);
        Assert.Equal(1, harness.Payments.Calls);
    }

    [Fact]
    public void Final_click_route_is_registered_once_through_the_order_module()
    {
        var module = File.ReadAllText(Path.Combine(RepoRoot(), "src", "backend", "Modules", "Order",
            "Tooba.Order.Endpoints", "OrderEndpointModule.cs"));

        Assert.Contains("StorefrontStartPaymentEndpoints.Map(app);", module, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------------------

    private static async Task<JsonElement> ReadJsonAsync(DefaultHttpContext http)
    {
        http.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(http.Response.Body);
        return doc.RootElement.Clone();
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "backend", "Tooba.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private sealed class Harness : IDisposable
    {
        public const string ConcreteGatewayProvider = "concrete-gateway-provider";

        public Guid CartId { get; private init; }
        public Guid CheckoutId { get; private init; }
        public DirectoryFake Directory { get; init; } = null!;
        public PreparationFake Preparation { get; init; } = null!;
        public PaymentsFake Payments { get; init; } = null!;
        public GateFake Gate { get; init; } = null!;
        public StorefrontStartPaymentComposer Composer { get; init; } = null!;
        public Guid PaymentId => Payments.PaymentId;
        public ServiceProvider Provider { get; private init; } = null!;

        public void Dispose() => Provider.Dispose();

        public async Task<(DefaultHttpContext Http, IResult Result)> InvokeAsync(
            string? guestSecret,
            string? idempotencyKey = RawKey,
            bool withHeaderKey = false,
            bool absentBody = false,
            Guid? cartId = null,
            string? guestSecretCookie = null)
        {
            var http = new DefaultHttpContext { RequestServices = Provider };
            http.Request.Method = HttpMethods.Post;
            http.Response.Body = new MemoryStream();
            if (!string.IsNullOrWhiteSpace(guestSecret))
            {
                http.Request.Headers["X-Tooba-Guest-Secret"] = guestSecret;
            }

            if (!string.IsNullOrWhiteSpace(guestSecretCookie))
            {
                http.Request.Headers.Cookie = "tooba_guest_secret=" + guestSecretCookie;
            }

            if (withHeaderKey)
            {
                http.Request.Headers["Idempotency-Key"] = RawKey;
            }

            Provider.GetRequiredService<IHttpContextAccessor>().HttpContext = http;

            var wire = absentBody
                ? null
                : new StorefrontStartPaymentBody(
                    CartVersion,
                    idempotencyKey ?? string.Empty,
                    UseWallet,
                    ProviderCode,
                    CouponCode);

            var type = typeof(OrderEndpointModule).Assembly.GetType(EndpointsTypeName, throwOnError: true)!;
            var method = type.GetMethod("StartPaymentAsync", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("StartPaymentAsync not found");

            var task = (Task<IResult>)method.Invoke(null, [
                cartId ?? CartId,
                wire,
                Composer,
                Gate,
                Provider.GetRequiredService<IOrderStorefrontActor>(),
                Provider.GetRequiredService<ApiResponseFactory>(),
                http.Request,
                CancellationToken.None,
            ])!;

            return (http, await task);
        }

        public bool UseWallet { get; init; }
        public string? ProviderCode { get; init; }
        public string? CouponCode { get; init; }

        public static Harness Create(
            bool replaying = false,
            bool replayOwnershipGranted = true,
            string? commitFailureCode = null,
            string? paymentFailureCode = null,
            OrderPaymentHoldPreparationOutcome preparationOutcome = OrderPaymentHoldPreparationOutcome.Prepared,
            bool useWallet = false,
            string? providerCode = PaymentProviderCodes.Manual,
            bool providerSelectionResolvable = true,
            Guid? authenticatedUserId = null)
        {
            var cartId = Guid.NewGuid();
            var checkoutId = Guid.NewGuid();
            var directory = new DirectoryFake(cartId, checkoutId, commitFailureCode);
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
                replaying
                    ? SucceededProcess(
                        checkoutId,
                        cartId,
                        StorefrontStartPaymentComposer.CommitKeyPrefix + RawKey)
                    : null);
            var preparation = new PreparationFake(checkoutId, preparationOutcome);
            var payments = new PaymentsFake(checkoutId, paymentFailureCode);
            var selection = new ProviderSelectionFake(ConcreteGatewayProvider, providerSelectionResolvable);
            var gate = new GateFake();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
            services.AddToobaObservabilityFoundation();
            services.AddSingleton<IErrorCatalogContributor, OrderErrorCatalogContributor>();
            services.AddSingleton<IErrorResourceSet, OrderErrorResourceSet>();
            services.AddSingleton<IErrorCatalogContributor, PaymentOwnedDescriptorMirror>();
            services.AddSingleton<IErrorResourceSet, PaymentErrorResourceSet>();
            services.AddSingleton<IOrderStorefrontActor>(actor);
            var provider = services.BuildServiceProvider();

            return new Harness
            {
                CartId = cartId,
                CheckoutId = checkoutId,
                Directory = directory,
                Preparation = preparation,
                Payments = payments,
                Gate = gate,
                UseWallet = useWallet,
                ProviderCode = providerCode,
                Composer = new StorefrontStartPaymentComposer(
                    shipping,
                    checkout,
                    preparation,
                    payments,
                    selection,
                    tracker),
                Provider = provider,
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

    /// <summary>
    /// توصیف‌گرهای کدهای Payment که Order مصرف می‌کند. این آینهٔ آزمون فقط چون
    /// <c>Tooba.Payment.Endpoints</c> (مالک واقعی کاتالوگ Payment) طبق قرارداد پروژه نباید به تست ماژول
    /// دیگر ارجاع شود؛ مقادیر وضعیت دقیقاً همان ثبت مالک است (۴۰۰ برای method.unavailable،
    /// ۴۰۹ برای reservation.not_payable).
    /// </summary>
    private sealed class PaymentOwnedDescriptorMirror : IErrorCatalogContributor
    {
        public IReadOnlyList<ErrorDescriptor> Contribute() =>
        [
            new(
                PaymentErrorCodes.MethodUnavailable,
                ErrorClassification.Business,
                StatusCodes.Status400BadRequest,
                PaymentErrorCodes.MethodUnavailable,
                ErrorSeverity.Warning,
                "Bad Request"),
            new(
                PaymentErrorCodes.ReservationNotPayable,
                ErrorClassification.Conflict,
                StatusCodes.Status409Conflict,
                PaymentErrorCodes.ReservationNotPayable,
                ErrorSeverity.Warning,
                "Conflict"),
        ];
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

    private sealed class GateFake : IOrderStorefrontCheckoutIdentityGate
    {
        public int Invocations { get; private set; }

        public Task EnsureCheckoutActorAsync(CancellationToken cancellationToken)
        {
            Invocations++;
            return Task.CompletedTask;
        }
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
    }

    private sealed class ActorFake(Guid? authenticatedUserId) : IOrderStorefrontActor
    {
        public Guid GuestActorId { get; } = Guid.NewGuid();

        public bool IsAuthenticated => authenticatedUserId is not null;

        public Guid? AuthenticatedUserId => authenticatedUserId;

        public Guid ResolvePlacementActor(bool usingSavedAddress) => authenticatedUserId ?? GuestActorId;

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
        public Task<CheckoutProcess?> FindSucceededBySubmissionKeyAsync(
            string submissionIdempotencyKey,
            CancellationToken cancellationToken) => Task.FromResult(process);

        public Task<CheckoutProcess> BeginAsync(
            string submissionIdempotencyKey,
            Guid cartId,
            DateTimeOffset now,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public void MarkValidating(CheckoutProcess process, DateTimeOffset now) => throw new NotSupportedException();

        public void MarkInventoryReserving(CheckoutProcess process, DateTimeOffset now) => throw new NotSupportedException();

        public void MarkOrderPersisting(CheckoutProcess process, Guid checkoutId, DateTimeOffset now) =>
            throw new NotSupportedException();

        public void MarkCartCommitting(CheckoutProcess process, DateTimeOffset now) => throw new NotSupportedException();

        public void MarkPaymentPending(CheckoutProcess process, DateTimeOffset now) => throw new NotSupportedException();

        public void MarkFailed(CheckoutProcess process, string failureCode, DateTimeOffset now) =>
            throw new NotSupportedException();
    }

    private sealed class ProviderSelectionFake(string concreteGatewayProvider, bool resolvable)
        : IPaymentProviderSelectionPort
    {
        public Task<StorefrontProviderSelection?> ResolveAsync(
            string? requestedProviderCode,
            bool useWallet,
            CancellationToken cancellationToken)
        {
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
                []));
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
                "provider-ref-028",
                "https://gateway.test/redirect",
                2_225_000m,
                "IRR",
                RequiresPspRedirect: true));
        }
    }

    private sealed class DirectoryFake(Guid cartId, Guid checkoutId, string? commitFailureCode) : ICheckoutDirectory
    {
        public int SubmitCalls { get; private set; }

        public Task<CheckoutSnapshot> PreviewAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CheckoutSnapshot> SubmitAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken)
        {
            SubmitCalls++;
            if (commitFailureCode is not null)
            {
                throw new ContractOperationException(commitFailureCode);
            }

            return Task.FromResult(Snapshot(checkoutId, command));
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

        public Task RestoreCancelledCheckoutAsync(
            Guid checkoutIdToRestore,
            OrderAccess access,
            CancellationToken cancellationToken) => throw new NotSupportedException();

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
                        "SO-028",
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

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
