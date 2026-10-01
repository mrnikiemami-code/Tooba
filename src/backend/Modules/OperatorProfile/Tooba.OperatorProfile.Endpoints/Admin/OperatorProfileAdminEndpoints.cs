using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
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
        try
        {
            var actor = await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            var snapshot = await sender.Send(new GetOperatorProfileQuery(actor), cancellationToken);
            return Results.Json(snapshot is null
                ? new
                {
                    firstName = "",
                    lastName = "",
                    displayName = "",
                    bio = (string?)null,
                    createdAt = (DateTimeOffset?)null,
                    updatedAt = (DateTimeOffset?)null,
                }
                : new
                {
                    firstName = snapshot.FirstName,
                    lastName = snapshot.LastName,
                    displayName = snapshot.DisplayName,
                    bio = snapshot.Bio,
                    createdAt = snapshot.CreatedAt,
                    updatedAt = snapshot.UpdatedAt,
                });
        }
        catch (PlatformHttpException ex)
        {
            return api.FromPlatformException(ex);
        }
        catch (SemanticException ex)
        {
            return api.FromSemanticException(ex);
        }
    }

    private static async Task<IResult> PutAsync(
        OperatorProfileWriteRequest body,
        HttpContext httpContext,
        IOperatorProfileAdminAuthorizer adminAuthorizer,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            var updated = await sender.Send(
                new UpsertOperatorProfileCommand(actor, body.DisplayName, body.FirstName, body.LastName, body.Bio),
                cancellationToken);
            return Results.Json(new
            {
                firstName = updated.FirstName,
                lastName = updated.LastName,
                displayName = updated.DisplayName,
                bio = updated.Bio,
                createdAt = updated.CreatedAt,
                updatedAt = updated.UpdatedAt,
            });
        }
        catch (PlatformHttpException ex)
        {
            return api.FromPlatformException(ex);
        }
        catch (SemanticException ex)
        {
            return api.FromSemanticException(ex);
        }
    }
}

/// <summary>بدنهٔ ویرایش پروفایل اپراتور.</summary>
public sealed record OperatorProfileWriteRequest(
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? Bio);
