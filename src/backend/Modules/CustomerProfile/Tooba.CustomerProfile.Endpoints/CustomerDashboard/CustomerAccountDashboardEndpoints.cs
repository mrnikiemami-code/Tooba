using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.CustomerProfile.Application.Queries.GetCustomerAccountDashboard;
using Tooba.CustomerProfile.Endpoints.Customer;
using Tooba.CustomerProfile.Endpoints.Resources;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.CustomerProfile.Endpoints.CustomerDashboard;

/// <summary>
/// Customer-account presentation composition HTTP: dashboard + dev-context.
/// Ownership is presentation/BFF only — Order/Wishlist/AddressBook/Identity business stays in those modules.
/// </summary>
public static class CustomerAccountDashboardEndpoints
{
    /// <summary>Maps dashboard and dev-context under the customer group.</summary>
    public static void MapDashboard(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/dev-context", GetDevContext);
        group.MapGet("/dashboard", GetDashboardAsync);
    }

    private static IResult GetDevContext(IHostEnvironment environment)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return Results.NotFound();
        }

        return Results.Json(new
        {
            actorUserId = StorefrontGuestActor.ActorId,
            label = CustomerAccountPresentationResources.Manager.GetString(
                "customer.account.dev_context_label",
                System.Globalization.CultureInfo.CurrentUICulture)
                ?? CustomerAccountPresentationResources.Manager.GetString(
                    "customer.account.dev_context_label",
                    System.Globalization.CultureInfo.InvariantCulture)
                ?? "customer.account.dev_context_label",
        });
    }

    private static async Task<IResult> GetDashboardAsync(
        HttpContext httpContext,
        ICustomerAccountActorResolver actorResolver,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = actorResolver.ResolveActor(httpContext);
        if (actor is null)
        {
            return Unauthorized(api);
        }

        var page = await sender.Send(new GetCustomerAccountDashboardQuery(actor.Value), cancellationToken);
        return Results.Json(page);
    }

    private static IResult Unauthorized(ApiResponseFactory api) =>
        api.FromFailure(new SemanticError("customer.session.required"));
}
