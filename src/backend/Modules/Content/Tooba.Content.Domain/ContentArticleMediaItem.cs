namespace Tooba.Content.Domain;

/// <summary>ارجاع گالری مقاله به دارایی DAM — بدون باینری.</summary>
public sealed class ContentArticleMediaItem
{
    /// <summary>حداکثر طول alt.</summary>
    public const int AltTextMaxLength = 200;
    /// <summary>حداکثر طول caption.</summary>
    public const int CaptionMaxLength = 500;

    private ContentArticleMediaItem() { }

    /// <summary>شناسهٔ مقاله.</summary>
    public Guid ArticleId { get; init; }
    /// <summary>شناسهٔ دارایی DAM.</summary>
    public Guid MediaAssetId { get; init; }
    /// <summary>ترتیب نمایش.</summary>
    public int DisplayOrder { get; private set; }
    /// <summary>متن جایگزین سطح استفادهٔ مقاله.</summary>
    public string? AltText { get; private set; }
    /// <summary>زیرنویس سطح استفادهٔ مقاله.</summary>
    public string? Caption { get; private set; }

    /// <summary>ردیف گالری جدید می‌سازد.</summary>
    public static ContentArticleMediaItem Create(
        Guid articleId,
        Guid mediaAssetId,
        int displayOrder,
        string? altText,
        string? caption)
    {
        ValidateMetadata(altText, caption);
        return new ContentArticleMediaItem
        {
            ArticleId = articleId,
            MediaAssetId = mediaAssetId,
            DisplayOrder = displayOrder,
            AltText = NormalizeOptional(altText, AltTextMaxLength),
            Caption = NormalizeOptional(caption, CaptionMaxLength),
        };
    }

    /// <summary>متادیتای سطح استفاده را به‌روزرسانی می‌کند.</summary>
    public void UpdateMetadata(string? altText, string? caption, int displayOrder)
    {
        ValidateMetadata(altText, caption);
        AltText = NormalizeOptional(altText, AltTextMaxLength);
        Caption = NormalizeOptional(caption, CaptionMaxLength);
        DisplayOrder = displayOrder;
    }

    private static void ValidateMetadata(string? altText, string? caption)
    {
        if (altText is not null && altText.Trim().Length > AltTextMaxLength)
            throw new InvalidOperationException("متن جایگزین گالری مقاله معتبر نیست.");
        if (caption is not null && caption.Trim().Length > CaptionMaxLength)
            throw new InvalidOperationException("زیرنویس گالری مقاله معتبر نیست.");
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? throw new InvalidOperationException("متادیتای گالری مقاله معتبر نیست.") : trimmed;
    }
}

/// <summary>قواعد امنیتی بدنهٔ HTML مقاله.</summary>
public static class ContentArticleBodyRules
{
    /// <summary>از نگهداری base64 یا data URI در بدنه جلوگیری می‌کند.</summary>
    public static void EnsureNoEmbeddedBinary(string body)
    {
        if (string.IsNullOrEmpty(body)) return;
        if (body.Contains("data:image", StringComparison.OrdinalIgnoreCase)
            || body.Contains("data:application", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(ContentArticleErrorCodes.UnsafeBodyMedia);
        }
    }
}
