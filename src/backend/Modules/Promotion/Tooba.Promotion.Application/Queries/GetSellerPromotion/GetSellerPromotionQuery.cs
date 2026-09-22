using MediatR; using Tooba.BuildingBlocks; using Tooba.BuildingBlocks.Results; using Tooba.Promotion.Application.Errors; using Tooba.Promotion.Application.Ports;
namespace Tooba.Promotion.Application.Queries.GetSellerPromotion;
public sealed record GetSellerPromotionQuery(Guid SellerPartyId,Guid PromotionId):IRequest<Result<PromotionReference>>;
public sealed class GetSellerPromotionQueryHandler(IPromotionDirectory promotions):IRequestHandler<GetSellerPromotionQuery,Result<PromotionReference>>{public async Task<Result<PromotionReference>> Handle(GetSellerPromotionQuery r,CancellationToken ct){var row=await promotions.GetForSellerAsync(null,r.SellerPartyId,r.PromotionId,ct);return row is null?Result.Failure<PromotionReference>(new SemanticError(PromotionErrorCodes.Missing)):Result.Success(row);}}

