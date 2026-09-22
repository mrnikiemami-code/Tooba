using MediatR; using Tooba.BuildingBlocks; using Tooba.BuildingBlocks.Results; using Tooba.Promotion.Application.Errors; using Tooba.Promotion.Application.Ports;
namespace Tooba.Promotion.Application.Queries.GetAdminPromotion;
public sealed record GetAdminPromotionQuery(Guid PromotionId):IRequest<Result<PromotionReference>>;
public sealed class GetAdminPromotionQueryHandler(IPromotionDirectory promotions):IRequestHandler<GetAdminPromotionQuery,Result<PromotionReference>>{public async Task<Result<PromotionReference>> Handle(GetAdminPromotionQuery r,CancellationToken ct){var row=await promotions.GetForAdminAsync(null,r.PromotionId,ct);return row is null?Result.Failure<PromotionReference>(new SemanticError(PromotionErrorCodes.Missing)):Result.Success(row);}}

