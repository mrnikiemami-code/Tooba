using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Settings.HoldPolicy.Commands;
using Tooba.Catalog.Application.Settings.HoldPolicy.Models;
using Tooba.Catalog.Application.Settings.HoldPolicy.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.Settings;

/// <summary>Admin hold-policy settings aggregate — Catalog-owned HTTP via MediatR.</summary>
public static class HoldPolicySettingsEndpoints
{
    /// <summary>Maps GET/PUT /v1/admin/settings/hold-policy.</summary>
    public static void MapHoldPolicySettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin/settings/hold-policy");
        group.MapGet("/", GetAsync);
        group.MapPut("/", PutAsync);
    }

    private static async Task<IResult> GetAsync(
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetHoldPolicySettingsQuery(), cancellationToken));
    }

    private static async Task<IResult> PutAsync(
        HoldPolicySettingsWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actor = await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new SaveHoldPolicySettingsCommand(
                body.CartPersistenceHours,
                body.OnlinePaymentHoldHours,
                body.ManualPaymentInitialHoldHours,
                body.ManualPaymentReviewHoldHours,
                body.Methods,
                body.InitialReservationHoldMinutes,
                body.RetryReservationHoldMinutes,
                body.MaxReservationCycles,
                actor),
            cancellationToken));
    }
}

/// <summary>PUT body for hold-policy settings (Host transport parity).</summary>
public sealed record HoldPolicySettingsWriteRequest(
    int? CartPersistenceHours,
    int? OnlinePaymentHoldHours,
    int? ManualPaymentInitialHoldHours,
    int? ManualPaymentReviewHoldHours,
    IReadOnlyList<PaymentMethodHoldView>? Methods,
    int? InitialReservationHoldMinutes,
    int? RetryReservationHoldMinutes,
    int? MaxReservationCycles);
