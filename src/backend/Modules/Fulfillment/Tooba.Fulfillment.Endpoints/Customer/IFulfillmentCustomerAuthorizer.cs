using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;

namespace Tooba.Fulfillment.Endpoints.Customer;

/// <summary>Neutral customer/guest ownership seam for checkout fulfillments.</summary>
public interface IFulfillmentCustomerAuthorizer
{
    /// <summary>null = allowed; otherwise SemanticError code.</summary>
    Task<SemanticError?> EnsureCanViewCheckoutAsync(
        HttpContext httpContext, Guid checkoutId, CancellationToken cancellationToken);
}
