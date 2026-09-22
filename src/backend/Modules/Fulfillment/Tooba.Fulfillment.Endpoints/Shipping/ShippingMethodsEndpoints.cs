using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Fulfillment.Application.Queries.ListEnabledShippingMethodsTree;

namespace Tooba.Fulfillment.Endpoints.Shipping;

/// <summary>Enabled shipping-methods tree — formerly Host AdminOrderOperations.</summary>
public static class ShippingMethodsEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/v1/admin/shipping-methods", ListShippingMethodsAsync);
    }

    private static async Task<IResult> ListShippingMethodsAsync(
        ISender sender, ApiResponseFactory api, string? language, CancellationToken cancellationToken)
    {
        // Preserve prior public behavior: no admin auth gate on this route; raw tree JSON via api.From.
        return api.From(await sender.Send(new ListEnabledShippingMethodsTreeQuery(language), cancellationToken));
    }
}
