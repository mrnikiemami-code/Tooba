using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
namespace Tooba.Promotion.Application.Errors;
public static class PromotionErrorCodes
{
 public const string Missing="promotion.missing"; public const string NameRequired="promotion.name.required"; public const string CouponRequired="promotion.coupon.required"; public const string MutationRejected="promotion.mutation.rejected"; public const string ActivateRejected="promotion.activate.rejected"; public const string DeactivateRejected="promotion.deactivate.rejected"; public const string SellerAuthorizationDenied="seller.authorization.denied"; public const string AdminAuthorizationDenied="admin.authorization.denied";
}
public static class PromotionExceptionMapper
{
 private static readonly Dictionary<string,string> KnownCodes=new(StringComparer.Ordinal)
 {
  [PromotionErrorCodes.Missing]=PromotionErrorCodes.Missing,[PromotionErrorCodes.NameRequired]=PromotionErrorCodes.NameRequired,
  [PromotionErrorCodes.CouponRequired]=PromotionErrorCodes.CouponRequired,[PromotionErrorCodes.MutationRejected]=PromotionErrorCodes.MutationRejected,
  [PromotionErrorCodes.ActivateRejected]=PromotionErrorCodes.ActivateRejected,[PromotionErrorCodes.DeactivateRejected]=PromotionErrorCodes.DeactivateRejected,
  ["promotion_not_found"]=PromotionErrorCodes.Missing,["promotion_not_owned_or_missing"]=PromotionErrorCodes.Missing,
  ["seller_id_required"]=PromotionErrorCodes.MutationRejected,
  ["promotion.definition.name_required"]=PromotionErrorCodes.MutationRejected,["promotion.definition.window_invalid"]=PromotionErrorCodes.MutationRejected,
  ["promotion.definition.percent_invalid"]=PromotionErrorCodes.MutationRejected,["promotion.definition.percent_no_fixed"]=PromotionErrorCodes.MutationRejected,
  ["promotion.definition.fixed_amount_invalid"]=PromotionErrorCodes.MutationRejected,["promotion.definition.fixed_currency_required"]=PromotionErrorCodes.MutationRejected,
  ["promotion.definition.fixed_no_percent"]=PromotionErrorCodes.MutationRejected,["promotion.definition.min_subtotal_invalid"]=PromotionErrorCodes.MutationRejected,
  ["promotion.definition.active_immutable"]=PromotionErrorCodes.MutationRejected
 };
 public static bool TryMapExact(string? message,out SemanticError error){if(message is not null&&KnownCodes.TryGetValue(message,out var code)){error=new(code);return true;}error=default!;return false;}
 public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> action,string? publicCode=null){try{return Result.Success(await action());}catch(InvalidOperationException ex) when(TryMapExact(ex.Message,out var mapped)){return Result.Failure<T>(publicCode is null?mapped:new SemanticError(publicCode));}}
}
