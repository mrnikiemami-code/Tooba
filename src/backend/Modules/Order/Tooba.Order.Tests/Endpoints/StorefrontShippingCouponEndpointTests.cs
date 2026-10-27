using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Contracts.Storefront;
using Tooba.Order.Application.Storefront.Checkout.Queries.GetStorefrontPaymentDraft;
using Tooba.Order.Application.Storefront.Shipping.Queries.ProjectStorefrontShipping;
using Xunit;

namespace Tooba.Order.Tests.Endpoints;

/// <summary>
/// TB-SHIPPING-CAPTURED-COUPON-CONTEXT-PARITY-236: قصد کوپن در <b>مرز HTTP</b> فقط عبور می‌کند.
/// </summary>
/// <remarks>
/// دلیگیت endpoint هیچ کوپنی را trim/case-fold/بازنویسی نمی‌کند و هیچ مبلغ/تخفیفی از بدنه نمی‌پذیرد؛
/// مقدار همان‌طور که رسیده به پرس‌وجوی application می‌رسد. این فایل هیچ DB/شبکه‌ای لمس نمی‌کند.
/// </remarks>
public sealed class StorefrontShippingCouponEndpointTests
{
    private const string EndpointsTypeName = "Tooba.Order.Endpoints.Storefront.StorefrontOrderEndpoints";

    [Theory]
    [InlineData(" SAVE10 ")]
    [InlineData("save10")]
    [InlineData("4294967296")]
    [InlineData("کد-تخفیف-۲۳۶")]
    public async Task Projection_endpoint_forwards_the_coupon_token_verbatim(string coupon)
    {
        var sender = new CapturingSender<ProjectStorefrontShippingQuery>();
        var gate = new GateFake();

        var exception = await Assert.ThrowsAsync<ContractOperationException>(() =>
            InvokeAsync(
                "ProjectShippingAsync",
                [
                    new StorefrontShippingProjectionRequest(
                        Guid.NewGuid(),
                        ProvinceName: "تهران",
                        MethodCode: "post:standard",
                        Language: "fa",
                        CityName: "تهران",
                        Destination: null,
                        CouponCode: coupon),
                    sender,
                    gate,
                    null!,
                    new DefaultHttpContext().Request,
                    CancellationToken.None,
                ]));

        Assert.Equal(CapturingSender<ProjectStorefrontShippingQuery>.Marker, exception.Code);
        var query = Assert.IsType<ProjectStorefrontShippingQuery>(sender.Captured);
        // بدون trim و بدون case-fold: تصمیم نهایی با Promotion است، نه با مرز HTTP.
        Assert.Equal(coupon, query.CouponCode);
        Assert.Equal("تهران", query.ProvinceName);
        Assert.Equal(1, gate.Invocations);
    }

    [Fact]
    public async Task Payment_draft_endpoint_forwards_the_query_string_coupon_verbatim()
    {
        var sender = new CapturingSender<GetStorefrontPaymentDraftQuery>();
        var http = new DefaultHttpContext();
        http.Request.QueryString = new QueryString("?couponCode=%20SAVE10%20");

        var exception = await Assert.ThrowsAsync<ContractOperationException>(() =>
            InvokeAsync(
                "GetPaymentDraftAsync",
                [
                    Guid.NewGuid(),
                    sender,
                    new GateFake(),
                    null!,
                    http.Request,
                    CancellationToken.None,
                ]));

        Assert.Equal(CapturingSender<GetStorefrontPaymentDraftQuery>.Marker, exception.Code);
        var query = Assert.IsType<GetStorefrontPaymentDraftQuery>(sender.Captured);
        Assert.Equal(" SAVE10 ", query.CouponCode);
    }

    private static async Task<IResult> InvokeAsync(string methodName, object?[] arguments)
    {
        var type = typeof(Tooba.Order.Endpoints.OrderEndpointModule).Assembly
            .GetType(EndpointsTypeName, throwOnError: true)!;
        var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{methodName} not found");
        var task = (Task<IResult>)method.Invoke(null, arguments)!;
        return await task;
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

    /// <summary>پرس‌وجو را ثبت می‌کند و پیش از هر کار واقعی با نشانگر متوقف می‌شود.</summary>
    private sealed class CapturingSender<TRequest> : ISender
        where TRequest : class
    {
        public const string Marker = "tb236.endpoint.captured";

        public object? Captured { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Captured = request;
            throw new ContractOperationException(Marker);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            Captured = request;
            throw new ContractOperationException(Marker);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            Captured = request;
            throw new ContractOperationException(Marker);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
