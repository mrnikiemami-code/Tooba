using Tooba.BuildingBlocks;

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
    public static TaxCategory Create(string code, string displayName, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("کد طبقهٔ مالیاتی خالی نیست.");
        }

        return new TaxCategory
        {
            CategoryId = UuidV7.New(),
            Code = code.Trim(),
            DisplayName = displayName.Trim(),
            CreatedAt = now,
        };
    }
}
