#pragma warning disable CS1591
using Tooba.Host.Storefront;
using Tooba.Payment.Application.Errors;
using Tooba.Payment.Application.Ports;

namespace Tooba.Host.Storefront;

/// <summary>Host adapter: checkout ownership/payable for Payment Application (no Checkout redesign).</summary>
public sealed class HostStorefrontCheckoutPaymentAccessAdapter : IStorefrontCheckoutPaymentAccessPort
{
    private readonly StorefrontCheckoutComposer _checkouts;
    private readonly CheckoutIdentityGate _gate;

    public HostStorefrontCheckoutPaymentAccessAdapter(
        StorefrontCheckoutComposer checkouts,
        CheckoutIdentityGate gate)
    {
        _checkouts = checkouts;
        _gate = gate;
    }

    public Task EnsureCheckoutActorAsync(CancellationToken cancellationToken) =>
        _gate.EnsureCheckoutActorAsync(cancellationToken);

    public async Task<StorefrontCheckoutPaymentAccessDto?> GetForMutationAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var page = await _checkouts.GetAsync(checkoutId, cartId, guestSecret, cancellationToken);
        return Map(page);
    }

    public async Task<StorefrontCheckoutPaymentAccessDto?> GetOwnedForPaymentResultAsync(
        Guid checkoutId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var page = await _checkouts.GetOwnedForPaymentResultAsync(checkoutId, guestSecret, cancellationToken);
        return Map(page);
    }

    public async Task<StorefrontCheckoutPaymentAccessDto?> GetOwnedAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var page = await _checkouts.GetAsync(checkoutId, cartId, guestSecret, cancellationToken);
        return Map(page);
    }

    private static StorefrontCheckoutPaymentAccessDto? Map(StorefrontCheckoutPage? page)
    {
        if (page?.CheckoutId is null)
            return null;
        var orderNumber = page.SellerOrders.Select(x => x.OrderNumber).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        return new StorefrontCheckoutPaymentAccessDto(
            page.CheckoutId.Value,
            page.PayableAmount,
            page.Currency,
            orderNumber);
    }
}
