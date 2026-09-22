using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Application.Commands.RedeemCustomerGiftCard;
using Tooba.Wallet.Application.Errors;
using Tooba.Wallet.Application.Queries.GetCustomerWalletSummary;
using Tooba.Wallet.Application.Queries.ListCustomerWalletLedger;

namespace Tooba.Wallet.Endpoints.Customer;

/// <summary>Wire-only redeem body.</summary>
public sealed record RedeemBody(string Code, string? IdempotencyKey);

/// <summary>Thin customer Wallet HTTP routes.</summary>
public static class WalletCustomerEndpoints
{
    /// <summary>Maps customer Wallet routes.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/v1/customer/wallet", SummaryAsync);
        app.MapGet("/v1/customer/wallet/ledger", LedgerAsync);
        app.MapPost("/v1/customer/wallet/gift-cards/redeem", RedeemAsync);
    }

    private static async Task<IResult> SummaryAsync(
        ISender sender, IWalletCustomerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError(WalletErrorCodes.CustomerSessionRequired));
        return api.From(await sender.Send(new GetCustomerWalletSummaryQuery(actor.Value), cancellationToken));
    }

    private static async Task<IResult> LedgerAsync(
        ISender sender, IWalletCustomerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken,
        int page = 1, int pageSize = 20)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError(WalletErrorCodes.CustomerSessionRequired));
        return api.From(await sender.Send(
            new ListCustomerWalletLedgerQuery(actor.Value, page, pageSize), cancellationToken));
    }

    private static async Task<IResult> RedeemAsync(
        RedeemBody body, ISender sender, IWalletCustomerAuthorizer authorizer,
        ApiResponseFactory api, IIdGenerator ids, HttpContext context, CancellationToken cancellationToken)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError(WalletErrorCodes.CustomerSessionRequired));
        var idem = ResolveIdempotencyKey(body.IdempotencyKey, context.Request, ids);
        return api.From(await sender.Send(
            new RedeemCustomerGiftCardCommand(actor.Value, body.Code, idem), cancellationToken));
    }

    private static string ResolveIdempotencyKey(string? bodyKey, HttpRequest request, IIdGenerator ids)
    {
        if (!string.IsNullOrWhiteSpace(bodyKey))
            return bodyKey.Trim();
        if (request.Headers.TryGetValue("Idempotency-Key", out var raw) && !string.IsNullOrWhiteSpace(raw))
            return raw.ToString().Trim();
        return ids.NewId().ToString("N");
    }
}
