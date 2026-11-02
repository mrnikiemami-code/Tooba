using System.Net;
using System.Text;
using Tooba.Shipping.Application.Carriers;
using Tooba.Shipping.Infrastructure.Carriers.Tipax;
using Xunit;

namespace Tooba.Shipping.Tests.Carriers;

/// <summary>CONTROLLED Tipax V4 shipment adapter facts. No live provider call.</summary>
public sealed class TipaxV4ShipmentProviderTests
{
    [Fact]
    public async Task Create_uses_official_omorder_shape_and_returns_tracking()
    {
        string? body = null;
        var handler = new RecordingHandler(async request =>
        {
            if (request.RequestUri!.AbsolutePath.Contains("/Token", StringComparison.OrdinalIgnoreCase))
            {
                return Json(HttpStatusCode.OK, """{"access_token":"token-1","expires_in":3600}""");
            }

            Assert.Equal("/api/OM/v4/OmOrder", request.RequestUri.AbsolutePath);
            Assert.Equal(HttpMethod.Post, request.Method);
            body = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, """
                {"trackingCodesWithTitles":[{"trackingCode":"TRK-1","customTitle":"x"}],"correlationId":"corr-9"}
                """);
        });
        var provider = CreateProvider(handler);

        var result = await provider.CreateAsync(ValidCreate(), CancellationToken.None);

        Assert.Equal(CarrierShipmentOutcome.Succeeded, result.Outcome);
        Assert.Equal("TRK-1", result.TrackingCode);
        Assert.Equal("corr-9", result.ProviderOrderId);
        Assert.Contains("\"successCallbackUrl\":\"https://store.test/ok\"", body, StringComparison.Ordinal);
        Assert.Contains("\"secondaryTraceCode\":\"create:op\"", body, StringComparison.Ordinal);
        Assert.Contains("\"cityId\":101", body, StringComparison.Ordinal);
        Assert.False(provider.Capabilities.CanLabel);
    }

    [Fact]
    public async Task Create_timeout_reconciles_by_secondary_then_unknown()
    {
        var calls = 0;
        var handler = new RecordingHandler(request =>
        {
            calls++;
            if (request.RequestUri!.AbsolutePath.Contains("/Token", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Json(HttpStatusCode.OK, """{"access_token":"token-1","expires_in":3600}"""));
            }

            if (request.RequestUri.AbsolutePath.Contains("/OmOrder", StringComparison.Ordinal))
            {
                return Task.FromException<HttpResponseMessage>(new OperationCanceledException());
            }

            Assert.Contains("/api/OM/v4/Tracking/BySecondaryTraceCode/", request.RequestUri.AbsolutePath, StringComparison.Ordinal);
            return Task.FromResult(Json(HttpStatusCode.NotFound, "{}"));
        });
        var provider = CreateProvider(handler);

        var result = await provider.CreateAsync(ValidCreate(), CancellationToken.None);

        Assert.Equal(CarrierShipmentOutcome.Unknown, result.Outcome);
        Assert.True(calls >= 2);
    }

    [Fact]
    public async Task Track_and_cancel_use_official_routes()
    {
        var paths = new List<string>();
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.Contains("/Token", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Json(HttpStatusCode.OK, """{"access_token":"token-1","expires_in":3600}"""));
            }

            paths.Add($"{request.Method} {request.RequestUri.AbsolutePath}");
            if (request.Method == HttpMethod.Post)
            {
                return Task.FromResult(Json(HttpStatusCode.OK, """{"trackingCode":"TRK-1","status":"in_transit","orderId":"o1"}"""));
            }

            return Task.FromResult(Json(HttpStatusCode.OK, "{}"));
        });
        var provider = CreateProvider(handler);
        var account = ValidCreate().Account;

        var tracked = await provider.TrackAsync(
            new CarrierShipmentTrackRequest("op", account, "TRK-1", null),
            CancellationToken.None);
        var cancelled = await provider.CancelAsync(
            new CarrierShipmentCancelRequest("op", account, "TRK-1"),
            CancellationToken.None);

        Assert.Equal(CarrierShipmentOutcome.Succeeded, tracked.Outcome);
        Assert.Equal("in_transit", tracked.ProviderStatus);
        Assert.Equal(CarrierShipmentOutcome.Succeeded, cancelled.Outcome);
        Assert.Contains("POST /api/OM/v4/Tracking/ByTrackingCode", paths);
        Assert.Contains("PUT /api/OM/v4/Parcels/Cancel/TRK-1", paths);
    }

    [Fact]
    public async Task Malformed_create_response_is_rejected_without_fake_tracking()
    {
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.Contains("/Token", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Json(HttpStatusCode.OK, """{"access_token":"token-1","expires_in":3600}"""));
            }

            return Task.FromResult(Json(HttpStatusCode.OK, """{"trackingCodesWithTitles":[]}"""));
        });
        var provider = CreateProvider(handler);

        var result = await provider.CreateAsync(ValidCreate(), CancellationToken.None);

        Assert.Equal(CarrierShipmentOutcome.ProviderRejected, result.Outcome);
        Assert.Null(result.TrackingCode);
    }

    private static TipaxV4ShipmentProvider CreateProvider(HttpMessageHandler handler)
        => new(new SingleClientFactory(handler), new TipaxV4AccountTokenSource(new SingleClientFactory(handler)));

    private static CarrierShipmentCreateRequest ValidCreate() => new(
        "create:op",
        Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
        1,
        new CarrierAccountExecution(
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            1,
            "merchant",
            "secret",
            "IRR",
            "11",
            "21",
            "31",
            "1",
            "2",
            "3",
            "https://api.test/",
            "https://tokens.test/Token"),
        "7",
        new CarrierShipmentParty("Sender", "09120000000", "Origin addr", "101", "1111111111"),
        new CarrierShipmentParty("Recipient", "09121111111", "Dest addr", "202", "2222222222"),
        new CarrierShipmentPackage(1500m, 2_000_000m, 20m, 15m, 10m, 0m),
        "https://store.test/ok",
        "https://store.test/fail");

    private static HttpResponseMessage Json(HttpStatusCode status, string body)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class RecordingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
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
}
