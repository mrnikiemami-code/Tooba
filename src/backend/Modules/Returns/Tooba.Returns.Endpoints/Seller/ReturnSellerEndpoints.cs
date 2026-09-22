using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Returns.Application.Commands.ApproveReturn;
using Tooba.Returns.Application.Commands.RejectReturn;
using Tooba.Returns.Application.Errors;
using Tooba.Returns.Application.Queries.GetSellerReturn;
using Tooba.Returns.Application.Queries.ListSellerReturns;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Endpoints.Seller;

/// <summary>Wire-only approve.</summary>
public sealed record ApproveReturnRequest(string? RefundDestination = null, string? Destination = null)
{
    public string? EffectiveRefundDestination => RefundDestination ?? Destination;
}

/// <summary>Wire-only reject.</summary>
public sealed record RejectReturnRequest(string? Reason);

/// <summary>Thin seller Returns HTTP routes.</summary>
public static class ReturnSellerEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/returns", SellerListAsync);
        group.MapGet("/returns/{returnRequestId:guid}", SellerGetAsync);
        group.MapPost("/returns/{returnRequestId:guid}/approve", SellerApproveAsync);
        group.MapPost("/returns/{returnRequestId:guid}/reject", SellerRejectAsync);
    }

    private static async Task<IResult> SellerListAsync(
        ISender sender, IReturnSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new ListSellerReturnsQuery(sellerPartyId), cancellationToken));
    }

    private static async Task<IResult> SellerGetAsync(
        Guid returnRequestId, ISender sender, IReturnSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new GetSellerReturnQuery(sellerPartyId, returnRequestId), cancellationToken));
    }

    private static async Task<IResult> SellerApproveAsync(
        Guid returnRequestId, ApproveReturnRequest? body,
        ISender sender, IReturnSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        RefundDestination? destination = null;
        if (!string.IsNullOrWhiteSpace(body?.EffectiveRefundDestination))
        {
            var parsed = ReturnsExceptionMapper.ParseDestination(body.EffectiveRefundDestination);
            if (parsed.IsFailure)
                return api.From(parsed);
            destination = parsed.Value;
        }

        var (actorUserId, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new ApproveReturnCommand(
            returnRequestId, actorUserId, sellerPartyId, destination), cancellationToken));
    }

    private static async Task<IResult> SellerRejectAsync(
        Guid returnRequestId, RejectReturnRequest body,
        ISender sender, IReturnSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var (actorUserId, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new RejectReturnCommand(
            returnRequestId, actorUserId, sellerPartyId, body.Reason), cancellationToken));
    }
}
