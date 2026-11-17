using Tooba.Returns.Application.ReturnRequests.Models;

namespace Tooba.Returns.Application.ReturnRequests.Ports;

/// <summary>
/// TB-278: reads the customer-facing return eligibility of ONE seller order.
/// <para>
/// It is a query port (not a second authority): it composes the existing eligibility evaluator with the owning Order
/// line and Catalog variant display facts. It never mints permission, never writes and never derives a quantity from
/// the shipped count, an aggregate "delivered" label or the browser clock.
/// </para>
/// </summary>
public interface ICustomerReturnEligibilityQuery
{
    /// <summary>
    /// Eligibility of one seller order for this customer. A seller order that does not belong to the customer is
    /// refused with a typed owner error; a missing order is a typed not-found. No fact of a foreign order is read.
    /// </summary>
    Task<CustomerReturnEligibilitySnapshot> QueryAsync(
        Guid customerUserId,
        Guid sellerOrderId,
        CancellationToken cancellationToken);
}
