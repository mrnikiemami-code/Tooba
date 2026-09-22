using MediatR; using Tooba.BuildingBlocks.Results; using Tooba.Promotion.Application.Ports;
namespace Tooba.Promotion.Application.Queries.ListAdminPromotions;
public sealed record ListAdminPromotionsQuery(Guid? SellerPartyId):IRequest<Result<IReadOnlyList<PromotionReference>>>;
public sealed class ListAdminPromotionsQueryHandler(IPromotionDirectory promotions):IRequestHandler<ListAdminPromotionsQuery,Result<IReadOnlyList<PromotionReference>>>{public async Task<Result<IReadOnlyList<PromotionReference>>> Handle(ListAdminPromotionsQuery r,CancellationToken ct)=>Result.Success(await promotions.ListForAdminAsync(null,r.SellerPartyId,ct));}
