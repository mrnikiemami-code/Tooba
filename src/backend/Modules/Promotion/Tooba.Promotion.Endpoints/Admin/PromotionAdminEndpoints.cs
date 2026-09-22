using Microsoft.AspNetCore.Builder; using Microsoft.AspNetCore.Http; using MediatR; using Microsoft.AspNetCore.Routing; using Tooba.BuildingBlocks.Presentation; using Tooba.Promotion.Application.Commands.DeactivateAdminPromotion; using Tooba.Promotion.Application.Queries.GetAdminPromotion; using Tooba.Promotion.Application.Queries.ListAdminPromotions;
namespace Tooba.Promotion.Endpoints.Admin;
public static class PromotionAdminEndpoints
{
 public static void Map(IEndpointRouteBuilder app){var g=app.MapGroup("/v1/admin/promotions");g.MapGet("",List);g.MapGet("/{id:guid}",Get);g.MapPost("/{id:guid}/deactivate",Deactivate);}
 private static async Task<IResult> List(ISender s,IPromotionAdminAuthorizer a,ApiResponseFactory api,HttpContext c,Guid? sellerPartyId=null,CancellationToken ct=default){await a.RequireAuthorizedAsync(c,ct);return api.From(await s.Send(new ListAdminPromotionsQuery(sellerPartyId),ct));}
 private static async Task<IResult> Get(Guid id,ISender s,IPromotionAdminAuthorizer a,ApiResponseFactory api,HttpContext c,CancellationToken ct){await a.RequireAuthorizedAsync(c,ct);return api.From(await s.Send(new GetAdminPromotionQuery(id),ct));}
 private static async Task<IResult> Deactivate(Guid id,ISender s,IPromotionAdminAuthorizer a,ApiResponseFactory api,HttpContext c,CancellationToken ct){await a.RequireAuthorizedAsync(c,ct);return api.From(await s.Send(new DeactivateAdminPromotionCommand(id),ct));}
}


