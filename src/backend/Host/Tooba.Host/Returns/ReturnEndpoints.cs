using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Host.Admin;
using Tooba.Host.Seller;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Ports;
using Tooba.Returns.Contracts;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Host.Returns;

/// <summary>خط مرجوعی در درخواست HTTP.</summary>
public sealed record ReturnLineRequest(Guid OrderLineId, decimal Quantity);

/// <summary>درخواست ایجاد مرجوعی.</summary>
public sealed record CreateReturnRequest(
    Guid SellerOrderId,
    string IdempotencyKey,
    string? Reason,
    IReadOnlyList<ReturnLineRequest> Items,
    string? RefundDestination = null,
    string? Destination = null)
{
    /// <summary>مقصد بازپرداخت از فیلدهای هم‌نام FE/Host.</summary>
    public string? EffectiveRefundDestination => RefundDestination ?? Destination;
}

/// <summary>درخواست تأیید مرجوعی فروشنده.</summary>
public sealed record ApproveReturnRequest(string? RefundDestination = null, string? Destination = null)
{
    /// <summary>مقصد بازپرداخت از فیلدهای هم‌نام FE/Host.</summary>
    public string? EffectiveRefundDestination => RefundDestination ?? Destination;
}

/// <summary>درخواست رد مرجوعی.</summary>
public sealed record RejectReturnRequest(string? Reason);

/// <summary>
/// Endpoint-State: HOST_THIN_TRANSPORT — auth + ApiResponseFactory/central semantic failures.
/// </summary>
public static class ReturnEndpoints
{
    /// <summary>مسیرهای مرجوعی را ثبت می‌کند.</summary>
    public static void MapReturnEndpoints(this WebApplication app)
    {
        var customer = app.MapGroup("/v1/customer");
        customer.MapGet("/returns", CustomerListAsync);
        customer.MapGet("/returns/{returnRequestId:guid}", CustomerGetAsync);
        customer.MapPost("/returns", CustomerCreateAsync);

        var seller = app.MapGroup("/v1/seller");
        seller.MapGet("/returns", SellerListAsync);
        seller.MapGet("/returns/{returnRequestId:guid}", SellerGetAsync);
        seller.MapPost("/returns/{returnRequestId:guid}/approve", SellerApproveAsync);
        seller.MapPost("/returns/{returnRequestId:guid}/reject", SellerRejectAsync);

        var admin = app.MapGroup("/v1/admin");
        admin.MapGet("/returns", AdminListAsync);
        admin.MapPost("/returns/query", AdminQueryGridAsync);
        admin.MapGet("/returns/{returnRequestId:guid}", AdminGetAsync);
        admin.MapPost("/returns/{returnRequestId:guid}/retry-refund", AdminRetryRefundAsync);
    }

    private static async Task<IResult> CustomerListAsync(
        ReturnPanelComposer composer,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = ResolveCustomerActor(request, session, environment);
            if (actor is null)
            {
                return api.FromFailure(new SemanticError("customer.actor.missing"));
            }

            return api.From(Result.Success(await composer.ListForCustomerAsync(actor.Value, cancellationToken)));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> CustomerGetAsync(
        Guid returnRequestId,
        ReturnPanelComposer composer,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = ResolveCustomerActor(request, session, environment);
            if (actor is null)
            {
                return api.FromFailure(new SemanticError("customer.actor.missing"));
            }

            var page = await composer.GetAsync(returnRequestId, cancellationToken);
            if (page is null || page.RequestedByUserId != actor.Value)
            {
                return api.FromFailure(new SemanticError(ReturnsErrorCodes.Missing));
            }

            return api.From(Result.Success(page));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> CustomerCreateAsync(
        CreateReturnRequest body,
        ReturnPanelComposer composer,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = ResolveCustomerActor(request, session, environment);
            if (actor is null)
            {
                return api.FromFailure(new SemanticError("customer.actor.missing"));
            }

            var destination = ReturnSemanticMapper.ParseDestination(body.EffectiveRefundDestination);
            if (destination.IsFailure)
            {
                return api.From(destination);
            }

            return api.From(Result.Success(
                await composer.CreateAsync(actor.Value, body, destination.Value, cancellationToken)));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
        catch (InvalidOperationException ex)
        {
            return api.FromFailure(ReturnSemanticMapper.MapException(ex));
        }
    }

    private static async Task<IResult> SellerListAsync(
        ReturnPanelComposer composer,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            return api.From(Result.Success(await composer.ListForSellerAsync(sellerPartyId, cancellationToken)));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> SellerGetAsync(
        Guid returnRequestId,
        ReturnPanelComposer composer,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var page = await composer.GetForSellerAsync(sellerPartyId, returnRequestId, cancellationToken);
            return page is null
                ? api.FromFailure(new SemanticError(ReturnsErrorCodes.Missing))
                : api.From(Result.Success(page));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> SellerApproveAsync(
        Guid returnRequestId,
        ApproveReturnRequest? body,
        ReturnPanelComposer composer,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        Result<RefundDestination>? destination = null;
        if (!string.IsNullOrWhiteSpace(body?.EffectiveRefundDestination))
        {
            destination = ReturnSemanticMapper.ParseDestination(body.EffectiveRefundDestination);
            if (destination.IsFailure)
            {
                return api.From(destination);
            }
        }

        return await SellerMutateAsync(
            request, session, guard, environment, returnRequestId, composer, api,
            (actor, id, c) => composer.ApproveAsync(id, actor, destination?.Value, c), cancellationToken);
    }

    private static Task<IResult> SellerRejectAsync(
        Guid returnRequestId,
        RejectReturnRequest body,
        ReturnPanelComposer composer,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        SellerMutateAsync(
            request, session, guard, environment, returnRequestId, composer, api,
            (actor, id, c) => composer.RejectAsync(id, actor, body.Reason, c), cancellationToken);

    private static async Task<IResult> SellerMutateAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        Guid returnRequestId,
        ReturnPanelComposer composer,
        ApiResponseFactory api,
        Func<Guid, Guid, CancellationToken, Task<ReturnSnapshot>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actorUserId, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var existing = await composer.GetForSellerAsync(sellerPartyId, returnRequestId, cancellationToken);
            if (existing is null)
            {
                return api.FromFailure(new SemanticError(ReturnsErrorCodes.Missing));
            }

            return api.From(Result.Success(await action(actorUserId, returnRequestId, cancellationToken)));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
        catch (InvalidOperationException ex)
        {
            return api.FromFailure(ReturnSemanticMapper.MapException(ex));
        }
    }

    private static async Task<IResult> AdminListAsync(
        ReturnPanelComposer composer,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return api.From(Result.Success(await composer.ListAllAsync(cancellationToken)));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static Task<IResult> AdminQueryGridAsync(
        GridQueryRequest body,
        ReturnPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        AdminGridQueryEndpoint.ExecuteAsync(
            body,
            request,
            session,
            tenant,
            guard,
            environment,
            composer.QueryGridAsync,
            cancellationToken);

    private static async Task<IResult> AdminGetAsync(
        Guid returnRequestId,
        ReturnPanelComposer composer,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var page = await composer.GetAsync(returnRequestId, cancellationToken);
            return page is null
                ? api.FromFailure(new SemanticError(ReturnsErrorCodes.Missing))
                : api.From(Result.Success(page));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> AdminRetryRefundAsync(
        Guid returnRequestId,
        ReturnPanelComposer composer,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return api.From(Result.Success(
                await composer.RetryRefundAsync(returnRequestId, actorUserId, cancellationToken)));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
        catch (InvalidOperationException ex)
        {
            return api.FromFailure(ReturnSemanticMapper.MapException(ex));
        }
    }

    private static Guid? ResolveCustomerActor(HttpRequest request, CurrentAuthenticatedSession session, IHostEnvironment environment)
    {
        if (session.IsAuthenticated && session.UserId is { } authenticated)
        {
            return authenticated;
        }

        if ((environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            && request.Headers.TryGetValue("X-Tooba-Dev-Actor-User-Id", out var raw)
            && Guid.TryParse(raw.ToString(), out var devActor)
            && devActor != Guid.Empty)
        {
            return devActor;
        }

        return null;
    }
}
