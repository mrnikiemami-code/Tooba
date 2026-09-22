using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Fulfillment.Application.Queries.ListCustomerCheckoutFulfillments;

namespace Tooba.Fulfillment.Endpoints.Customer;

/// <summary>Thin customer Fulfillment HTTP routes.</summary>
public static class FulfillmentCustomerEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/orders/{checkoutId:guid}/fulfillments", CustomerListAsync);
    }

    private static async Task<IResult> CustomerListAsync(
        Guid checkoutId, ISender sender, IFulfillmentCustomerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var denied = await authorizer.EnsureCanViewCheckoutAsync(context, checkoutId, cancellationToken);
        if (denied is not null)
            return api.FromFailure(denied);
        var result = await sender.Send(new ListCustomerCheckoutFulfillmentsQuery(checkoutId), cancellationToken);
        if (result.IsFailure) return api.From(result);
        var payload = result.Value;
        // Preserve anonymous success shape expected by clients.
        return Results.Json(new
        {
            fulfillments = payload.Fulfillments,
            preferredCustomerTrackingReference = payload.PreferredCustomerTrackingReference,
            preferredCustomerTrackingPackageNumber = payload.PreferredCustomerTrackingPackageNumber,
            preferredCustomerPackageStatus = payload.PreferredCustomerPackageStatus,
        });
    }
}
