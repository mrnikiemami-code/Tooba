using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Tooba.Shipping.Application.Carriers;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Infrastructure.Carriers;
using Tooba.Shipping.Infrastructure.Carriers.Tipax;
using Xunit;

namespace Tooba.Shipping.Tests.Carriers;

public sealed class TipaxV4RateProviderTests
{
    [Fact]
    public async Task Disabled_provider_is_fail_closed_without_network_io()
    {
        var handler = new RecordingHandler(_ => Task.FromException<HttpResponseMessage>(
            new InvalidOperationException("network must stay closed")));
        var provider = CreateProvider(new TipaxV4Options(), handler);

        var result = await provider.QuoteAsync(ValidRequest(), CancellationToken.None);

        Assert.Equal(CarrierRateOutcome.Disabled, result.Outcome);
        Assert.Empty(result.Options);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Quote_uses_official_v4_wire_shape_and_normalizes_rate_options()
    {
        string? priceBody = null;
        string? bearer = null;
        var handler = new RecordingHandler(async request =>
        {
            if (request.RequestUri!.AbsoluteUri == "https://tokens.test/Token")
            {
                var form = await request.Content!.ReadAsStringAsync();
                Assert.Contains("username=merchant", form, StringComparison.Ordinal);
                Assert.Contains("password=secret", form, StringComparison.Ordinal);
                Assert.Contains("grant_type=password", form, StringComparison.Ordinal);
                return Json(HttpStatusCode.OK, """
                    {"access_token":"token-1","refresh_token":"refresh-1","expires_in":3600}
                    """);
            }

            priceBody = await request.Content!.ReadAsStringAsync();
            bearer = request.Headers.Authorization?.ToString();
            Assert.Equal("/api/OM/v4/Pricing", request.RequestUri.AbsolutePath);
            return Json(HttpStatusCode.OK, """
                [{
                  "packNum":1,
                  "regularRate":{
                    "serviceId":7,
                    "serviceTitle":"اکسپرس",
                    "transportationCost":120000,
                    "packing":5000,
                    "tax":12500,
                    "finalPrice":137500,
                    "pickupPrice":0,
                    "deliveryPrice":0
                  },
                  "regularPlusRate":null,
                  "expressRate":null,
                  "sameDayExpressRate":null,
                  "airExpressRate":null
                }]
                """);
        });
        var provider = CreateProvider(EnabledOptions(), handler);

        var result = await provider.QuoteAsync(ValidRequest(), CancellationToken.None);

        Assert.Equal(CarrierRateOutcome.Quoted, result.Outcome);
        var option = Assert.Single(result.Options);
        Assert.Equal("tipax", option.ProviderCode);
        Assert.Equal("7", option.ServiceCode);
        Assert.Equal(137_500m, option.FinalAmount);
        Assert.Equal(120_000m, option.TransportationAmount);
        Assert.Equal(12_500m, option.TaxAmount);
        Assert.Equal(5_000m, option.PackagingAmount);
        Assert.Equal("IRR", option.CurrencyCode);
        Assert.Equal("tipax:quote-42:1:7", option.ProviderReference);
        Assert.Equal("Bearer token-1", bearer);

        Assert.NotNull(priceBody);
        Assert.Contains("\"packageInputs\"", priceBody, StringComparison.Ordinal);
        Assert.Contains("\"origin\":{\"cityId\":101}", priceBody, StringComparison.Ordinal);
        Assert.Contains("\"destination\":{\"cityId\":202}", priceBody, StringComparison.Ordinal);
        Assert.Contains("\"serviceId\":7", priceBody, StringComparison.Ordinal);
        Assert.Contains("\"paymentType\":11", priceBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Username", priceBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", priceBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unauthorized_pricing_reauthenticates_once_and_retries_once()
    {
        var tokenCalls = 0;
        var priceCalls = 0;
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri!.Host == "tokens.test")
            {
                tokenCalls++;
                return Task.FromResult(Json(HttpStatusCode.OK,
                    $"{{\"access_token\":\"token-{tokenCalls}\",\"expires_in\":3600}}"));
            }

            priceCalls++;
            return Task.FromResult(priceCalls == 1
                ? Json(HttpStatusCode.Unauthorized, "{}")
                : Json(HttpStatusCode.OK, """
                    [{"packNum":1,"regularRate":{"serviceId":7,"serviceTitle":"regular","transportationCost":10,"packing":0,"tax":1,"finalPrice":11,"pickupPrice":0,"deliveryPrice":0}}]
                    """));
        });
        var provider = CreateProvider(EnabledOptions(), handler);

        var result = await provider.QuoteAsync(ValidRequest(), CancellationToken.None);

        Assert.Equal(CarrierRateOutcome.Quoted, result.Outcome);
        Assert.Equal(2, tokenCalls);
        Assert.Equal(2, priceCalls);
    }

    [Fact]
    public void Registry_is_case_insensitive_and_rejects_duplicate_provider_codes()
    {
        var provider = new StubProvider("tipax");
        var registry = new CarrierRateProviderRegistry([provider]);

        Assert.True(registry.TryResolve(" TIPAX ", out var resolved));
        Assert.Same(provider, resolved);
        Assert.False(registry.TryResolve("post", out _));
        Assert.Throws<InvalidOperationException>(() =>
            new CarrierRateProviderRegistry([provider, new StubProvider("TIPAX")]));
    }

    private static TipaxV4RateProvider CreateProvider(TipaxV4Options options, HttpMessageHandler handler)
    {
        var factory = new SingleClientFactory(handler);
        var monitor = new StaticOptionsMonitor<TipaxV4Options>(options);
        var tokens = new TipaxV4TokenSource(factory, monitor);
        return new TipaxV4RateProvider(factory, monitor, tokens);
    }

    private static TipaxV4Options EnabledOptions() => new()
    {
        Enabled = true,
        ApiBaseUrl = "https://api.test/",
        TokenUrl = "https://tokens.test/Token",
        Username = "merchant",
        Password = "secret",
        CurrencyCode = "IRR",
        PrepaidPaymentTypeId = 11,
        CollectPaymentTypeId = 12,
        CashOnDeliveryPaymentTypeId = 13,
        DoorPickupTypeId = 21,
        DropOffPickupTypeId = 22,
        DoorDistributionTypeId = 31,
        BranchDistributionTypeId = 32,
        PackingId = 1,
        PackageContentId = 2,
        PackType = 3,
        Timeout = TimeSpan.FromSeconds(2)
    };

    private static CarrierRateRequest ValidRequest() => new(
        "quote-42",
        "101",
        "202",
        "7",
        1_500m,
        2_000_000m,
        20m,
        15m,
        10m);

    private static HttpResponseMessage Json(HttpStatusCode status, string body)
        => new(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return send(request);
        }
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        private readonly HttpClient _client = new(handler, disposeHandler: false);

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class StubProvider(string providerCode) : ICarrierRateProvider
    {
        public string ProviderCode => providerCode;

        public Task<CarrierRateResult> QuoteAsync(
            CarrierRateRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(CarrierRateResult.Failure(CarrierRateOutcome.Disabled, "stub"));
    }
}
