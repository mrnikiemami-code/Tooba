using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;
using Tooba.BuildingBlocks.Tenancy;
using Tooba.Media.Application.Assets.Commands;
using Tooba.Media.Contracts.Assets;
using Tooba.Media.Contracts.Errors;

namespace Tooba.Media.Endpoints.Seller;

/// <summary>Seller-scoped Media upload/list/get for shop-owned Story assets.</summary>
public static class MediaSellerEndpoints
{
    /// <summary>Maps /v1/seller/media routes.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/seller/media");
        group.MapPost("", UploadAsync);
        group.MapGet("", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
    }

    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess access,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var (actor, sellerPartyId) = await access.RequireAuthorizedAsync(request, cancellationToken);
        var tenantId = ResolveTenant(tenant);
        if (!request.HasFormContentType)
            return api.From(Fail(MediaErrorCodes.ValidationFailed));

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null)
            return api.From(Fail(MediaErrorCodes.UploadFailed));

        await using var stream = file.OpenReadStream();
        var occurrence = form["occurrenceKey"].ToString();
        return api.From(await sender.Send(
            new UploadSellerStoryMediaCommand(
                stream,
                file.FileName,
                file.ContentType ?? string.Empty,
                tenantId,
                sellerPartyId,
                actor,
                string.IsNullOrWhiteSpace(occurrence) ? null : occurrence),
            cancellationToken));
    }

    private static async Task<IResult> ListAsync(
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess access,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var (_, sellerPartyId) = await access.RequireAuthorizedAsync(request, cancellationToken);
        var tenantId = ResolveTenant(tenant);
        return api.From(await sender.Send(
            new ListSellerStoryMediaQuery(tenantId, sellerPartyId, take), cancellationToken));
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess access,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await access.RequireAuthorizedAsync(request, cancellationToken);
        var tenantId = ResolveTenant(tenant);
        return api.From(await sender.Send(
            new GetSellerStoryMediaQuery(tenantId, sellerPartyId, id), cancellationToken));
    }

    private static Guid ResolveTenant(ICurrentTenant tenant)
    {
        var key = tenant.Current?.TenantId.Value;
        if (string.IsNullOrWhiteSpace(key))
            throw new SemanticException(new SemanticError(MediaErrorCodes.ValidationFailed));
        return StableStoryTenantGuid.FromTenantKey(key);
    }

    private static Result<StoryMediaAssetAuthorization> Fail(string code) =>
        Result.Failure<StoryMediaAssetAuthorization>(new SemanticError(code));
}
