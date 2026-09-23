using Tooba.Order.Domain;

namespace Tooba.Order.Application.Customer.Ports;

/// <summary>بارگذاری Checkout متعلق به مشتری بدون افشای DbContext به Application handlers دیگر.</summary>
public interface ICustomerOrderCheckoutStore
{
    /// <summary>فهرست Checkoutهای Actor به ترتیب نزولی زمان ثبت.</summary>
    Task<IReadOnlyList<CheckoutGroup>> ListByActorAsync(
        Guid actorUserId,
        int take,
        CancellationToken cancellationToken);

    /// <summary>یک Checkout با خطوط فقط اگر PlacedByUserId برابر Actor باشد.</summary>
    Task<CheckoutGroup?> GetOwnedAsync(
        Guid actorUserId,
        Guid checkoutId,
        CancellationToken cancellationToken);
}
