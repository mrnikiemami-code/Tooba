using System.Security.Cryptography;
using System.Text;
using Tooba.BuildingBlocks;
using Tooba.Story.Contracts.Errors;

using Tooba.Story.Domain.Rules;

namespace Tooba.Story.Domain.Aggregates;

/// <summary>یک اسلاید رسانه در استوری.</summary>
public sealed class StoryItem
{
    private StoryItem() { }

    /// <summary>شناسهٔ پایدار آیتم.</summary>
    public Guid StoryItemId { get; init; }
    /// <summary>شناسهٔ استوری والد.</summary>
    public Guid StoryId { get; init; }
    /// <summary>ترتیب نمایش.</summary>
    public int DisplayOrder { get; private set; }
    /// <summary>نوع رسانه image یا video.</summary>
    public string MediaType { get; private set; } = StoryRules.MediaImage;
    /// <summary>مرجع مات رسانه.</summary>
    public Guid? MediaAssetId { get; private set; }
    /// <summary>URL ایستا یا سرو شده.</summary>
    public string? MediaUrl { get; private set; }
    /// <summary>زیرنویس اختیاری.</summary>
    public string? Caption { get; private set; }
    /// <summary>مدت نمایش اختیاری به میلی‌ثانیه.</summary>
    public int? DurationMs { get; private set; }
    /// <summary>نوع CTA آیتم.</summary>
    public string CtaType { get; private set; } = StoryRules.CtaNone;
    /// <summary>هدف CTA آیتم.</summary>
    public string? CtaTarget { get; private set; }
    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>زمان آخرین به‌روزرسانی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>آیتم جدید می‌سازد.</summary>
    internal static StoryItem Create(
        Guid storyId,
        string mediaType,
        int displayOrder,
        DateTimeOffset now,
        Guid? mediaAssetId,
        string? mediaUrl,
        string? caption,
        int? durationMs,
        string? ctaType,
        string? ctaTarget)
    {
        var normalizedMediaType = StoryRules.ValidateMediaType(mediaType);
        ValidateMediaUrl(mediaUrl);
        ValidateCaption(caption);
        ValidateDuration(durationMs);
        var (normalizedCtaType, normalizedCtaTarget) = StoryRules.ValidateCta(ctaType, ctaTarget);
        return new StoryItem
        {
            StoryItemId = UuidV7.New(),
            StoryId = storyId,
            DisplayOrder = displayOrder,
            MediaType = normalizedMediaType,
            MediaAssetId = mediaAssetId,
            MediaUrl = NormalizeOptional(mediaUrl),
            Caption = NormalizeOptional(caption),
            DurationMs = durationMs,
            CtaType = normalizedCtaType,
            CtaTarget = normalizedCtaTarget,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>فیلدهای آیتم را به‌روزرسانی می‌کند.</summary>
    internal void Update(
        string mediaType,
        Guid? mediaAssetId,
        string? mediaUrl,
        string? caption,
        int? durationMs,
        string? ctaType,
        string? ctaTarget,
        DateTimeOffset now)
    {
        MediaType = StoryRules.ValidateMediaType(mediaType);
        ValidateMediaUrl(mediaUrl);
        ValidateCaption(caption);
        ValidateDuration(durationMs);
        var (normalizedCtaType, normalizedCtaTarget) = StoryRules.ValidateCta(ctaType, ctaTarget);
        MediaAssetId = mediaAssetId;
        MediaUrl = NormalizeOptional(mediaUrl);
        Caption = NormalizeOptional(caption);
        DurationMs = durationMs;
        CtaType = normalizedCtaType;
        CtaTarget = normalizedCtaTarget;
        UpdatedAt = now;
    }

    /// <summary>ترتیب نمایش را تنظیم می‌کند.</summary>
    internal void SetDisplayOrder(int displayOrder, DateTimeOffset now)
    {
        DisplayOrder = displayOrder;
        UpdatedAt = now;
    }

    private static void ValidateMediaUrl(string? mediaUrl)
    {
        if (mediaUrl is not null && (mediaUrl.Trim().Length == 0 || mediaUrl.Trim().Length > StoryRules.MediaUrlMaxLength))
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
    }

    private static void ValidateCaption(string? caption)
    {
        if (caption is not null && caption.Trim().Length > StoryRules.CaptionMaxLength)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
    }

    private static void ValidateDuration(int? durationMs)
    {
        if (durationMs is < 0)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
