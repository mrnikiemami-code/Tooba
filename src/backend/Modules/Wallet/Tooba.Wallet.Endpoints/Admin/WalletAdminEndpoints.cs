using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Wallet.Application.Commands.AdjustAdminWallet;
using Tooba.Wallet.Application.Commands.IssueAdminGiftCard;
using Tooba.Wallet.Application.Commands.RevokeAdminGiftCard;
using Tooba.Wallet.Application.Queries.GetAdminGiftCard;
using Tooba.Wallet.Application.Queries.GetAdminWallet;
using Tooba.Wallet.Application.Queries.GetWalletDemoPreview;
using Tooba.Wallet.Application.Queries.ListAdminGiftCards;
using Tooba.Wallet.Application.Queries.ListAdminWalletLedger;

namespace Tooba.Wallet.Endpoints.Admin;

/// <summary>Wire-only issue gift-card body.</summary>
public sealed record IssueBody(
    decimal InitialAmount,
    string? Currency,
    DateTimeOffset? ExpiresAt,
    Guid? RecipientActorUserId,
    string? IdempotencyKey);

/// <summary>Wire-only admin wallet adjustment body.</summary>
public sealed record AdjustBody(decimal Amount, string Direction, string Reason, string? IdempotencyKey);

/// <summary>Thin admin Wallet/GiftCard HTTP routes.</summary>
public static class WalletAdminEndpoints
{
    /// <summary>Maps admin Wallet/GiftCard routes.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/v1/admin/gift-cards", ListGiftCardsAsync);
        app.MapPost("/v1/admin/gift-cards", IssueGiftCardAsync);
        app.MapGet("/v1/admin/gift-cards/{cardId:guid}", GetGiftCardAsync);
        app.MapPost("/v1/admin/gift-cards/{cardId:guid}/revoke", RevokeGiftCardAsync);
        app.MapGet("/v1/admin/wallets/{customerActorUserId:guid}", GetWalletAsync);
        app.MapGet("/v1/admin/wallets/{customerActorUserId:guid}/ledger", WalletLedgerAsync);
        app.MapPost("/v1/admin/wallets/{customerActorUserId:guid}/adjustments", AdjustWalletAsync);
        app.MapGet("/v1/admin/wallet/demo-preview", DemoPreviewAsync);
    }

    private static async Task<IResult> ListGiftCardsAsync(
        ISender sender, IWalletAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken,
        string? status = null, string? q = null, int page = 1, int pageSize = 20)
    {
        await authorizer.RequireAuthorizedAsync(context, "giftcard.view", cancellationToken);
        return api.From(await sender.Send(
            new ListAdminGiftCardsQuery(status, q, page, pageSize), cancellationToken));
    }

    private static async Task<IResult> IssueGiftCardAsync(
        IssueBody body, ISender sender, IWalletAdminAuthorizer authorizer,
        ApiResponseFactory api, IIdGenerator ids, HttpContext context, CancellationToken cancellationToken)
    {
        var actor = await authorizer.RequireAuthorizedAsync(context, "giftcard.manage", cancellationToken);
        var idem = ResolveIdempotencyKey(body.IdempotencyKey, context.Request, ids);
        return api.From(await sender.Send(
            new IssueAdminGiftCardCommand(
                actor, body.InitialAmount, body.Currency, body.ExpiresAt, body.RecipientActorUserId, idem),
            cancellationToken));
    }

    private static async Task<IResult> GetGiftCardAsync(
        Guid cardId, ISender sender, IWalletAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, "giftcard.view", cancellationToken);
        return api.From(await sender.Send(new GetAdminGiftCardQuery(cardId), cancellationToken));
    }

    private static async Task<IResult> RevokeGiftCardAsync(
        Guid cardId, ISender sender, IWalletAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, "giftcard.manage", cancellationToken);
        return api.From(await sender.Send(new RevokeAdminGiftCardCommand(cardId), cancellationToken));
    }

    private static async Task<IResult> GetWalletAsync(
        Guid customerActorUserId, ISender sender, IWalletAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, "wallet.view", cancellationToken);
        return api.From(await sender.Send(new GetAdminWalletQuery(customerActorUserId), cancellationToken));
    }

    private static async Task<IResult> WalletLedgerAsync(
        Guid customerActorUserId, ISender sender, IWalletAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken,
        int page = 1, int pageSize = 20)
    {
        await authorizer.RequireAuthorizedAsync(context, "wallet.view", cancellationToken);
        return api.From(await sender.Send(
            new ListAdminWalletLedgerQuery(customerActorUserId, page, pageSize), cancellationToken));
    }

    private static async Task<IResult> AdjustWalletAsync(
        Guid customerActorUserId, AdjustBody body, ISender sender, IWalletAdminAuthorizer authorizer,
        ApiResponseFactory api, IIdGenerator ids, HttpContext context, CancellationToken cancellationToken)
    {
        var actor = await authorizer.RequireAuthorizedAsync(context, "wallet.adjust", cancellationToken);
        var idem = ResolveIdempotencyKey(body.IdempotencyKey, context.Request, ids);
        return api.From(await sender.Send(
            new AdjustAdminWalletCommand(
                customerActorUserId, actor, body.Amount, body.Direction, body.Reason, idem),
            cancellationToken));
    }

    private static async Task<IResult> DemoPreviewAsync(
        ISender sender, ApiResponseFactory api,
        IHostEnvironment environment, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return Results.NotFound();
        return api.From(await sender.Send(new GetWalletDemoPreviewQuery(), cancellationToken));
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
