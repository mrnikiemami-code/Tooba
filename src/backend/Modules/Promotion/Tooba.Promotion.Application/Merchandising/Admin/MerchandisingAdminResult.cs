using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Promotion.Application.Composition;

namespace Tooba.Promotion.Application.Merchandising.Admin;

/// <summary>
/// Shared result shaping for Admin campaign use cases. The "missing" outcome and every typed Promotion
/// fault are mapped by declared stable code only — never by message text — and unexpected exceptions
/// propagate untouched to the canonical global exception boundary.
/// </summary>
internal static class MerchandisingAdminResult
{
    /// <summary>کمپین تهی را به <c>merchandising.campaign.missing</c> نگاشت می‌کند.</summary>
    public static Result<T> OrMissing<T>(T? value) where T : class =>
        value is null ? Result.Failure<T>(new SemanticError(MerchandisingAdminErrorCodes.Missing)) : Result.Success(value);

    /// <summary>کمپین تهی را به نتیجهٔ Missing و خطای نوع‌دار Promotion را به کد پایدار نگاشت می‌کند.</summary>
    public static Task<Result<T>> ExecuteOrMissingAsync<T>(Func<Task<T?>> action) where T : class
    {
        ArgumentNullException.ThrowIfNull(action);
        return PromotionOperation.ExecuteAsync(
            async () => await action() ?? throw new ContractOperationException(MerchandisingAdminErrorCodes.Missing));
    }

    /// <summary><c>false</c> را به نتیجهٔ Missing و خطای نوع‌دار Promotion را به کد پایدار نگاشت می‌کند.</summary>
    public static Task<Result> ExecuteFlagAsync(Func<Task<bool>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return PromotionOperation.ExecuteAsync(async () =>
        {
            if (!await action())
            {
                throw new ContractOperationException(MerchandisingAdminErrorCodes.Missing);
            }
        });
    }
}
