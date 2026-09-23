using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Order.Application.Customer;
using Tooba.Order.Application.Customer.Commands.RetryCustomerUnpaidOrder;
using Tooba.Order.Application.Customer.Queries.GetCustomerOrderDetail;
using Tooba.Order.Application.Customer.Queries.ListCustomerOrders;

namespace Tooba.Order.Endpoints.Customer;

/// <summary>Host transport adapter — resolves authenticated/dev customer Actor for Order customer routes.</summary>
public interface IOrderCustomerAuthorizer
{
    /// <summary>
    /// Returns Actor when session (or Dev seam) is present; otherwise SessionRequired error.
    /// Never trusts body-supplied customer ids.
    /// </summary>
    Task<(Guid? ActorUserId, SemanticError? Error)> ResolveActorAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken);
}

internal static class CustomerOrderEndpoints
{
    internal static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/customer");
        group.MapGet("/orders", ListAsync);
        group.MapGet("/orders/{checkoutId:guid}", GetAsync);
        group.MapPost("/orders/{checkoutId:guid}/retry-unpaid", RetryAsync);
    }

    private static async Task<IResult> ListAsync(
        ISender sender,
        IOrderCustomerAuthorizer auth,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken ct)
    {
        var (actor, error) = await auth.ResolveActorAsync(context, ct);
        if (actor is null)
        {
            return api.FromFailure(error ?? new SemanticError(CustomerOrderErrors.SessionRequired));
        }

        return api.From(await sender.Send(new ListCustomerOrdersQuery(actor.Value), ct));
    }

    private static async Task<IResult> GetAsync(
        Guid checkoutId,
        ISender sender,
        IOrderCustomerAuthorizer auth,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken ct)
    {
        var (actor, error) = await auth.ResolveActorAsync(context, ct);
        if (actor is null)
        {
            return api.FromFailure(error ?? new SemanticError(CustomerOrderErrors.SessionRequired));
        }

        return api.From(await sender.Send(new GetCustomerOrderDetailQuery(actor.Value, checkoutId), ct));
    }

    private static async Task<IResult> RetryAsync(
        Guid checkoutId,
        ISender sender,
        IOrderCustomerAuthorizer auth,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken ct)
    {
        var (actor, error) = await auth.ResolveActorAsync(context, ct);
        if (actor is null)
        {
            return api.FromFailure(error ?? new SemanticError(CustomerOrderErrors.SessionRequired));
        }

        return api.From(await sender.Send(new RetryCustomerUnpaidOrderCommand(actor.Value, checkoutId), ct));
    }
}
