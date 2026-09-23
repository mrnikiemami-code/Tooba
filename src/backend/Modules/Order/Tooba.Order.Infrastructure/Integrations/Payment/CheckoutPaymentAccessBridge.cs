#pragma warning disable CS1591
using Tooba.Cart.Contracts;
using Tooba.Order.Application;
using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;
using Tooba.Order.Contracts.Payments;

namespace Tooba.Order.Infrastructure.Integrations.Payment;

public sealed class CheckoutPaymentAccessBridge(
    ICheckoutDirectory checkouts,
    ICartPresentationGateway carts) : ICheckoutPaymentAccessReader
{
    private static readonly Guid GuestActorId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-000000000009");

    public async Task<CheckoutPaymentAccessSnapshot?> GetForMutationAsync(
        Guid checkoutId, Guid? cartId, string? guestSecret, Guid? authenticatedUserId,
        CancellationToken cancellationToken)
    {
        var snapshot = await OwnedAsync(checkoutId, guestSecret, authenticatedUserId, cancellationToken);
        return snapshot is not null && (cartId is null || cartId == snapshot.CartId) ? Map(snapshot) : null;
    }

    public async Task<CheckoutPaymentAccessSnapshot?> GetOwnedForPaymentResultAsync(
        Guid checkoutId, string? guestSecret, Guid? authenticatedUserId,
        CancellationToken cancellationToken)
    {
        var snapshot = await OwnedAsync(checkoutId, guestSecret, authenticatedUserId, cancellationToken);
        return snapshot is null ? null : Map(snapshot);
    }

    public Task<CheckoutPaymentAccessSnapshot?> GetOwnedAsync(
        Guid checkoutId, Guid? cartId, string? guestSecret, Guid? authenticatedUserId,
        CancellationToken cancellationToken) =>
        GetForMutationAsync(checkoutId, cartId, guestSecret, authenticatedUserId, cancellationToken);

    private async Task<CheckoutSnapshot?> OwnedAsync(
        Guid checkoutId, string? guestSecret, Guid? authenticatedUserId, CancellationToken cancellationToken)
    {
        var actor = authenticatedUserId is { } id && id != Guid.Empty ? id : GuestActorId;
        var snapshot = await checkouts.GetCheckoutAsync(checkoutId, new OrderAccess(null, actor), cancellationToken);
        if (snapshot is null) return null;
        if (authenticatedUserId is { } userId && userId != Guid.Empty) return snapshot;
        if (string.IsNullOrWhiteSpace(guestSecret)) return null;
        return await carts.TryGetForOwnershipAsync(snapshot.CartId, guestSecret, cancellationToken) is null ? null : snapshot;
    }

    private static CheckoutPaymentAccessSnapshot Map(CheckoutSnapshot snapshot) =>
        new(snapshot.CheckoutId,
            snapshot.SellerOrders.Sum(x => x.GrandTotalSnapshot) + snapshot.ShippingAmount,
            snapshot.Currency,
            snapshot.SellerOrders.Select(x => x.OrderNumber).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)));
}
