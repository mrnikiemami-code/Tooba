using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.OperatorProfile.Application.Admin.Commands;
using Tooba.OperatorProfile.Application.Admin.Queries;

namespace Tooba.OperatorProfile.Endpoints.Admin;

/// <summary>مرز HTTP پروفایل شخصی اپراتور Admin؛ تنظیمات سراسری platform اینجا نیست.</summary>
public static class OperatorProfileAdminEndpoints
{
    /// <summary>مسیرهای GET/PUT پروفایل اپراتور را ثبت می‌کند.</summary>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/", GetAsync);
        group.MapPut("/", PutAsync);
    }

    private static async Task<IResult> GetAsync(
        HttpContext httpContext,
        IOperatorProfileAdminAuthorizer adminAuthorizer,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
        return api.From(await sender.Send(new GetOperatorProfileQuery(actor), cancellationToken));
    }

    private static async Task<IResult> PutAsync(
        OperatorProfileWriteRequest body,
        HttpContext httpContext,
        IOperatorProfileAdminAuthorizer adminAuthorizer,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
        return api.From(await sender.Send(
            new UpsertOperatorProfileCommand(actor, body.DisplayName, body.FirstName, body.LastName, body.Bio),
            cancellationToken));
    }
}

/// <summary>بدنهٔ ویرایش پروفایل اپراتور.</summary>
public sealed record OperatorProfileWriteRequest(
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? Bio);
