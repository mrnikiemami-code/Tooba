using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Order.Application.Storefront.Checkout.Commands.SubmitStorefrontCheckout;
using Tooba.Order.Application.Storefront.Checkout.Queries.GetStorefrontCheckout;
using Tooba.Order.Application.Storefront.Checkout.Queries.PreviewStorefrontCheckout;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.PendingPayment.Commands.CancelPendingCheckout;
using Tooba.Order.Application.Storefront.PendingPayment.Commands.HidePendingPaymentCard;
using Tooba.Order.Application.Storefront.PendingPayment.Queries.ListStorefrontPendingPayments;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Shipping.Commands.CommitStorefrontShipping;
using Tooba.Order.Application.Storefront.Shipping.Commands.SaveStorefrontShippingSelection;
using Tooba.Order.Application.Storefront.Shipping.Queries.ProjectStorefrontShipping;

namespace Tooba.Order.Endpoints.Storefront;

internal static class StorefrontOrderEndpoints
{
    internal static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/storefront");
        group.MapPost("/pending-payments", ListPendingPaymentsAsync);
        group.MapPost("/checkout/{checkoutId:guid}/cancel", CancelPendingCheckoutAsync);
        group.MapPost("/checkout/{checkoutId:guid}/hide-pending-card", HidePendingCardAsync);
        group.MapPost("/checkout/preview", PreviewCheckoutAsync);
        group.MapPost("/checkout", SubmitCheckoutAsync);
        group.MapGet("/checkout/{checkoutId:guid}", GetCheckoutAsync);
        group.MapPost("/shipping/projection", ProjectShippingAsync);
        group.MapPut("/shipping/selection", SaveShippingSelectionAsync);
        group.MapPost("/shipping/commit", CommitShippingAsync);
    }

    private static async Task<IResult> ListPendingPaymentsAsync(
        StorefrontPendingPaymentQueryRequest? body,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken ct) =>
        api.From(await sender.Send(new ListStorefrontPendingPaymentsQuery(body), ct));

    private static async Task<IResult> CancelPendingCheckoutAsync(
        Guid checkoutId,
        HttpRequest request,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken ct) =>
        api.From(await sender.Send(
            new CancelPendingCheckoutCommand(
                checkoutId,
                await ReadOptionalCartIdAsync(request, ct),
                ReadGuestSecret(request)),
            ct));

    private static async Task<IResult> HidePendingCardAsync(
        Guid checkoutId,
        HttpRequest request,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken ct) =>
        api.From(await sender.Send(
            new HidePendingPaymentCardCommand(
                checkoutId,
                await ReadOptionalCartIdAsync(request, ct),
                ReadGuestSecret(request)),
            ct));

    private static async Task<IResult> PreviewCheckoutAsync(
        Guid cartId,
        ISender sender,
        IOrderStorefrontCheckoutIdentityGate gate,
        ApiResponseFactory api,
        HttpRequest request,
        string? couponCode = null,
        CancellationToken ct = default)
    {
        await gate.EnsureCheckoutActorAsync(ct);
        return api.From(await sender.Send(
            new PreviewStorefrontCheckoutQuery(
                cartId,
                ReadGuestSecret(request),
                couponCode ?? request.Query["couponCode"].FirstOrDefault()),
            ct));
    }

    private static async Task<IResult> SubmitCheckoutAsync(
        StorefrontSubmitCheckoutRequest body,
        ISender sender,
        IOrderStorefrontCheckoutIdentityGate gate,
        ApiResponseFactory api,
        HttpRequest request,
        CancellationToken ct)
    {
        await gate.EnsureCheckoutActorAsync(ct);
        return api.From(await sender.Send(
            new SubmitStorefrontCheckoutCommand(
                body.CartId,
                ReadGuestSecret(request),
                body.ExpectedCartVersion,
                body.IdempotencyKey,
                body.Shipping,
                body.CouponCode),
            ct));
    }

    private static async Task<IResult> GetCheckoutAsync(
        Guid checkoutId,
        Guid cartId,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CancellationToken ct) =>
        api.From(await sender.Send(
            new GetStorefrontCheckoutQuery(checkoutId, cartId, ReadGuestSecret(request)),
            ct));

    private static async Task<IResult> ProjectShippingAsync(
        StorefrontShippingProjectionRequest body,
        ISender sender,
        IOrderStorefrontCheckoutIdentityGate gate,
        ApiResponseFactory api,
        HttpRequest request,
        CancellationToken ct)
    {
        await gate.EnsureCheckoutActorAsync(ct);
        return api.From(await sender.Send(
            new ProjectStorefrontShippingQuery(
                body.CartId,
                ReadGuestSecret(request),
                body.ProvinceName,
                body.MethodCode,
                body.Language),
            ct));
    }

    private static async Task<IResult> SaveShippingSelectionAsync(
        StorefrontShippingSelectionRequest body,
        ISender sender,
        IOrderStorefrontCheckoutIdentityGate gate,
        ApiResponseFactory api,
        HttpRequest request,
        CancellationToken ct)
    {
        await gate.EnsureCheckoutActorAsync(ct);
        return api.From(await sender.Send(
            new SaveStorefrontShippingSelectionCommand(body, ReadGuestSecret(request)),
            ct));
    }

    private static async Task<IResult> CommitShippingAsync(
        StorefrontShippingCommitRequest body,
        ISender sender,
        IOrderStorefrontCheckoutIdentityGate gate,
        ApiResponseFactory api,
        HttpRequest request,
        CancellationToken ct)
    {
        await gate.EnsureCheckoutActorAsync(ct);
        return api.From(await sender.Send(
            new CommitStorefrontShippingCommand(
                body.CartId,
                ReadGuestSecret(request),
                body.ExpectedCartVersion,
                body.IdempotencyKey,
                body.CouponCode),
            ct));
    }

    private static async Task<Guid> ReadOptionalCartIdAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.Query.TryGetValue("cartId", out var query)
            && Guid.TryParse(query.ToString(), out var fromQuery)
            && fromQuery != Guid.Empty)
        {
            return fromQuery;
        }

        try
        {
            if (!request.HasJsonContentType() || request.ContentLength is 0)
            {
                return Guid.Empty;
            }

            var body = await request.ReadFromJsonAsync<StorefrontPaymentCartRequest>(cancellationToken);
            return body?.CartId ?? Guid.Empty;
        }
        catch (BadHttpRequestException)
        {
            return Guid.Empty;
        }
        catch (System.Text.Json.JsonException)
        {
            return Guid.Empty;
        }
    }

    private static string? ReadGuestSecret(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Tooba-Guest-Secret", out var header) && !string.IsNullOrWhiteSpace(header))
        {
            return header.ToString();
        }

        return request.Cookies.TryGetValue("tooba_guest_secret", out var cookie) ? cookie : null;
    }
}
