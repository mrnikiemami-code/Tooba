using Tooba.BuildingBlocks;
using Tooba.Tax.Contracts.Errors;

namespace Tooba.Tax.Domain;

/// <summary>
/// طبقهٔ مالیاتی مات برای ارجاع Catalog/Offer؛ نرخ روی کالا ذخیره نمی‌شود.
/// </summary>
public sealed class TaxCategory
{
    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private TaxCategory()
    {
    }

    /// <summary>
    /// شناسهٔ طبقه.
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// کد پایدار پیکربندی.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// توضیح عملیاتی؛ متن قانون نیست.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// زمان ایجاد UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// طبقه می‌سازد. نرخ مالیات اینجا نیست.
    /// </summary>
    public static TaxCategory Create(Guid categoryId, string code, string displayName, DateTimeOffset now)
    {
        if (categoryId == Guid.Empty)
        {
            throw new SemanticException(new SemanticError(TaxErrorCodes.CategoryIdRequired));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new SemanticException(new SemanticError(TaxErrorCodes.CategoryCodeRequired));
        }

        return new TaxCategory
        {
            CategoryId = categoryId,
            Code = code.Trim(),
            DisplayName = displayName.Trim(),
            CreatedAt = now,
        };
    }
}
