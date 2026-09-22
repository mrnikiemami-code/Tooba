using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Errors;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Queries.GetCustomerReturn;
using Tooba.Returns.Application.Queries.ListCustomerReturns;

namespace Tooba.Returns.Endpoints.Customer;

/// <summary>Wire-only return line.</summary>
public sealed record ReturnLineRequest(Guid OrderLineId, decimal Quantity);

/// <summary>Wire-only create return.</summary>
public sealed record CreateReturnRequest(
    Guid SellerOrderId,
    string IdempotencyKey,
    string? Reason,
    IReadOnlyList<ReturnLineRequest> Items,
    string? RefundDestination = null,
    string? Destination = null)
{
    public string? EffectiveRefundDestination => RefundDestination ?? Destination;
}

/// <summary>Thin customer Returns HTTP routes.</summary>
public static class ReturnCustomerEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/returns", CustomerListAsync);
        group.MapGet("/returns/{returnRequestId:guid}", CustomerGetAsync);
        group.MapPost("/returns", CustomerCreateAsync);
    }

    private static async Task<IResult> CustomerListAsync(
        ISender sender, IReturnCustomerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError("customer.actor.missing"));
        return api.From(await sender.Send(new ListCustomerReturnsQuery(actor.Value), cancellationToken));
    }

    private static async Task<IResult> CustomerGetAsync(
        Guid returnRequestId, ISender sender, IReturnCustomerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError("customer.actor.missing"));
        return api.From(await sender.Send(new GetCustomerReturnQuery(actor.Value, returnRequestId), cancellationToken));
    }

    private static async Task<IResult> CustomerCreateAsync(
        CreateReturnRequest body, ISender sender, IReturnCustomerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError("customer.actor.missing"));

        var destination = ReturnsExceptionMapper.ParseDestination(body.EffectiveRefundDestination);
        if (destination.IsFailure)
            return api.From(destination);

        var command = new Application.Commands.CreateReturn.CreateReturnCommand(
            body.SellerOrderId,
            actor.Value,
            body.IdempotencyKey,
            body.Reason,
            body.Items.Select(x => new ReturnLineCommand(x.OrderLineId, x.Quantity)).ToArray(),
            destination.Value);
        return api.From(await sender.Send(command, cancellationToken));
    }
}
