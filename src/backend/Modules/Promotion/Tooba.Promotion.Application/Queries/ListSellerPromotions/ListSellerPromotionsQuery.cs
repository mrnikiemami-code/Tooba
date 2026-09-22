using MediatR; using Tooba.BuildingBlocks.Results; using Tooba.Promotion.Application.Ports;
namespace Tooba.Promotion.Application.Queries.ListSellerPromotions;
public sealed record ListSellerPromotionsQuery(Guid SellerPartyId):IRequest<Result<IReadOnlyList<PromotionReference>>>;
public sealed class ListSellerPromotionsQueryHandler(IPromotionDirectory promotions):IRequestHandler<ListSellerPromotionsQuery,Result<IReadOnlyList<PromotionReference>>>{public async Task<Result<IReadOnlyList<PromotionReference>>> Handle(ListSellerPromotionsQuery request,CancellationToken ct)=>Result.Success(await promotions.ListBySellerAsync(null,request.SellerPartyId,ct));}
