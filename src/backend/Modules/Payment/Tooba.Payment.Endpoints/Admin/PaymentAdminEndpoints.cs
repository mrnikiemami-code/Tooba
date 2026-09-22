using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Payment.Application.Commands.ConfirmAdminDeposit;
using Tooba.Payment.Application.Commands.ReconcileAdminPayment;
using Tooba.Payment.Application.Commands.RejectAdminDeposit;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Application.Queries.GetAdminPayment;
using Tooba.Payment.Application.Queries.QueryAdminPaymentsGrid;

namespace Tooba.Payment.Endpoints.Admin;

/// <summary>Thin admin Payment HTTP routes.</summary>
public static class PaymentAdminEndpoints
{
    /// <summary>Maps admin Payment routes under /v1/admin.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/admin");
        group.MapGet("/payments/{paymentId:guid}", GetAsync);
        group.MapPost("/payments/{paymentId:guid}/reconcile", ReconcileAsync);
        group.MapPost("/payments/{paymentId:guid}/confirm-deposit", ConfirmAsync);
        group.MapPost("/payments/{paymentId:guid}/reject-deposit", RejectAsync);
        group.MapPost("/payments/query", QueryGridAsync);
    }

    private static async Task<IResult> GetAsync(
        Guid paymentId,
        ISender sender,
        IPaymentAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new GetAdminPaymentQuery(paymentId), cancellationToken));
    }

    private static async Task<IResult> ReconcileAsync(
        Guid paymentId,
        ISender sender,
        IPaymentAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new ReconcileAdminPaymentCommand(paymentId), cancellationToken));
    }

    private static async Task<IResult> ConfirmAsync(
        Guid paymentId,
        ISender sender,
        IPaymentAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new ConfirmAdminDepositCommand(paymentId), cancellationToken));
    }

    private static async Task<IResult> RejectAsync(
        Guid paymentId,
        ISender sender,
        IPaymentAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new RejectAdminDepositCommand(paymentId), cancellationToken));
    }

    private static async Task<IResult> QueryGridAsync(
        GridQueryRequest body,
        ISender sender,
        IPaymentAdminAuthorizer authorizer,
        IPaymentAdminGridQueryNormalizer normalizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        var normalized = normalizer.Normalize(body);
        var input = new AdminPaymentGridQueryInput(
            normalized.Search,
            normalized.Filters.Select(f => new AdminPaymentGridFilterInput(
                f.Field, f.Operator, f.Value, f.ValueTo, f.Values)).ToList(),
            normalized.Sort.FirstOrDefault()?.Field ?? "created",
            normalized.Sort.FirstOrDefault()?.Direction ?? "desc",
            normalized.Page,
            normalized.PageSize);
        var result = await sender.Send(new QueryAdminPaymentsGridQuery(input), cancellationToken);
        if (!result.IsSuccess)
            return api.From(result);
        var page = result.Value!;
        return Results.Json(new GridPageResponse<AdminPaymentGridItemDto>(
            page.Items, page.Page, page.PageSize, page.Total));
    }
}
