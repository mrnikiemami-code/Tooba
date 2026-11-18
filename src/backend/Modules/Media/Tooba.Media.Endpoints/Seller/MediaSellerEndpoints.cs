using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;
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
        IStoryMediaAssetPort media,
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
        try
        {
            var asset = await media.UploadForSellerAsync(
                stream,
                file.FileName,
                file.ContentType,
                tenantId,
                sellerPartyId,
                actor,
                string.IsNullOrWhiteSpace(occurrence) ? null : occurrence,
                cancellationToken);
            return api.From(Result.Success(asset));
        }
        catch (ContractOperationException ex) when (MediaErrorCodes.IsKnown(ex.Code))
        {
            return api.From(Fail(ex.Code));
        }
    }

    private static async Task<IResult> ListAsync(
        HttpRequest request,
        IStoryMediaAssetPort media,
        ISellerPanelAccess access,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var (_, sellerPartyId) = await access.RequireAuthorizedAsync(request, cancellationToken);
        var tenantId = ResolveTenant(tenant);
        var rows = await media.ListSellerAssetsAsync(tenantId, sellerPartyId, take, cancellationToken);
        return api.From(Result.Success(rows));
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        HttpRequest request,
        IStoryMediaAssetPort media,
        ISellerPanelAccess access,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await access.RequireAuthorizedAsync(request, cancellationToken);
        var tenantId = ResolveTenant(tenant);
        try
        {
            var asset = await media.AuthorizeForStoryAsync(
                id, tenantId, sellerPartyId, allowUnscopedAdminAssets: false, cancellationToken);
            return api.From(Result.Success(asset));
        }
        catch (ContractOperationException ex) when (MediaErrorCodes.IsKnown(ex.Code))
        {
            return api.From(Fail(ex.Code));
        }
    }

    private static Guid ResolveTenant(ICurrentTenant tenant)
    {
        var key = tenant.Current?.TenantId.Value;
        if (string.IsNullOrWhiteSpace(key))
            throw new SemanticException(new SemanticError(MediaErrorCodes.ValidationFailed));
        // Stable SHA-derived Guid matching Story tenant mapping for store-alpha/beta keys.
        if (string.Equals(key, "store-alpha", StringComparison.Ordinal))
            return Guid.Parse("a0000000-0001-4000-8000-000000000001");
        if (string.Equals(key, "store-beta", StringComparison.Ordinal))
            return Guid.Parse("a0000000-0002-4000-8000-000000000002");
        var payload = System.Text.Encoding.UTF8.GetBytes($"tooba:story:{key.Trim()}");
        var hash = System.Security.Cryptography.SHA256.HashData(payload);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x40);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }

    private static Result<StoryMediaAssetAuthorization> Fail(string code) =>
        Result.Failure<StoryMediaAssetAuthorization>(new SemanticError(code));
}
