using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Promotion.Contracts.Merchandising;
using Tooba.Promotion.Application.Merchandising;
using Tooba.Promotion.Application.Merchandising.Admin;

namespace Tooba.Promotion.Endpoints.Admin;

public static class MerchandisingCampaignAdminEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin/merchandising-campaigns");
        group.MapGet("/", ListAsync);
        group.MapGet("/types", ListTypesAsync);
        group.MapGet("/offer-candidates", ListCandidatesAsync);
        group.MapGet("/{campaignId:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{campaignId:guid}", UpdateAsync);
        group.MapPost("/{campaignId:guid}/publish", PublishAsync);
        group.MapPost("/{campaignId:guid}/archive", ArchiveAsync);
        group.MapPost("/{campaignId:guid}/members", AddMemberAsync);
        group.MapDelete("/{campaignId:guid}/members/{sellerOfferId:guid}", RemoveMemberAsync);
        group.MapPut("/{campaignId:guid}/members/order", ReorderAsync);
        group.MapPut("/{campaignId:guid}/members/{sellerOfferId:guid}/price", SetPriceAsync);
    }

    private static async Task<IResult> ListAsync(ISender sender, IPromotionAdminAuthorizer authorizer, ApiResponseFactory api, HttpContext http, string? search, string? lifecycle, Guid? promotionTypeId, string? runtimeWindow, int? skip, int? take, string? locale, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ListMerchandisingCampaignsQuery(search, lifecycle, promotionTypeId, runtimeWindow, skip ?? 0, take ?? 25, locale), cancellationToken));
    }

    private static async Task<IResult> ListTypesAsync(ISender sender, IPromotionAdminAuthorizer authorizer, ApiResponseFactory api, HttpContext http, string? locale, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ListMerchandisingCampaignTypesQuery(locale), cancellationToken));
    }

    private static async Task<IResult> ListCandidatesAsync(ISender sender, IPromotionAdminAuthorizer authorizer, ApiResponseFactory api, HttpContext http, string? search, int? skip, int? take, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ListMerchandisingOfferCandidatesQuery(search, skip ?? 0, take ?? 20), cancellationToken));
    }

    private static async Task<IResult> GetAsync(Guid campaignId, ISender sender, IPromotionAdminAuthorizer authorizer, ApiResponseFactory api, HttpContext http, string? locale, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetMerchandisingCampaignQuery(campaignId, locale), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(AdminMerchCampaignWriteRequest body, ISender sender, IPromotionAdminAuthorizer authorizer, ApiResponseFactory api, HttpContext http, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new CreateMerchandisingCampaignCommand(body), cancellationToken));
    }

    private static async Task<IResult> UpdateAsync(Guid campaignId, AdminMerchCampaignUpdateRequest body, ISender sender, IPromotionAdminAuthorizer authorizer, ApiResponseFactory api, HttpContext http, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new UpdateMerchandisingCampaignCommand(campaignId, body), cancellationToken));
    }

    private static async Task<IResult> PublishAsync(Guid campaignId, ISender sender, IPromotionAdminAuthorizer authorizer, ApiResponseFactory api, HttpContext http, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new PublishMerchandisingCampaignCommand(campaignId), cancellationToken));
    }

    private static async Task<IResult> ArchiveAsync(Guid campaignId, ISender sender, IPromotionAdminAuthorizer authorizer, ApiResponseFactory api, HttpContext http, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ArchiveMerchandisingCampaignCommand(campaignId), cancellationToken));
    }

    private static async Task<IResult> AddMemberAsync(Guid campaignId, AdminMerchAddMemberRequest body, ISender sender, IPromotionAdminAuthorizer authorizer, ApiResponseFactory api, HttpContext http, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new AddMerchandisingCampaignMemberCommand(campaignId, body.SellerOfferId), cancellationToken));
    }

    private static async Task<IResult> RemoveMemberAsync(Guid campaignId, Guid sellerOfferId, ISender sender, IPromotionAdminAuthorizer authorizer, ApiResponseFactory api, HttpContext http, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new RemoveMerchandisingCampaignMemberCommand(campaignId, sellerOfferId), cancellationToken));
    }

    private static async Task<IResult> ReorderAsync(Guid campaignId, AdminMerchReorderMembersRequest body, ISender sender, IPromotionAdminAuthorizer authorizer, ApiResponseFactory api, HttpContext http, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ReorderMerchandisingCampaignMembersCommand(campaignId, body.OrderedSellerOfferIds), cancellationToken));
    }

    private static async Task<IResult> SetPriceAsync(Guid campaignId, Guid sellerOfferId, AdminMerchMemberPriceRequest body, ISender sender, IPromotionAdminAuthorizer authorizer, ApiResponseFactory api, HttpContext http, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new SetMerchandisingCampaignMemberPriceCommand(campaignId, sellerOfferId, body), cancellationToken));
    }
}
