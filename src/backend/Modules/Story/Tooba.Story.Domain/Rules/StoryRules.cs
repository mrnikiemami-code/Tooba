using System.Security.Cryptography;
using System.Text;
using Tooba.BuildingBlocks;
using Tooba.Story.Contracts.Errors;

namespace Tooba.Story.Domain.Rules;

/// <summary>ثابت‌ها و اعتبارسنجی CTA و رسانهٔ استوری.</summary>
public static class StoryRules
{
    /// <summary>حداکثر طول عنوان.</summary>
    public const int TitleMaxLength = 120;
    /// <summary>حداکثر طول locale.</summary>
    public const int LocaleMaxLength = 16;
    /// <summary>حداکثر طول market.</summary>
    public const int MarketMaxLength = 32;
    /// <summary>حداکثر طول URL رسانه.</summary>
    public const int MediaUrlMaxLength = 512;
    /// <summary>حداکثر طول نوع CTA.</summary>
    public const int CtaTypeMaxLength = 32;
    /// <summary>حداکثر طول هدف CTA.</summary>
    public const int CtaTargetMaxLength = 512;
    /// <summary>حداکثر طول caption آیتم.</summary>
    public const int CaptionMaxLength = 200;
    /// <summary>حداکثر طول نوع رسانه.</summary>
    public const int MediaTypeMaxLength = 16;
    /// <summary>حداکثر طول دلیل رد بازبینی.</summary>
    public const int RejectionReasonMaxLength = 500;

    /// <summary>نوع CTA بدون لینک.</summary>
    public const string CtaNone = "none";
    /// <summary>نوع رسانه تصویر.</summary>
    public const string MediaImage = "image";
    /// <summary>نوع رسانه ویدیو.</summary>
    public const string MediaVideo = "video";

    private static readonly HashSet<string> AllowedCtaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        CtaNone,
        "product",
        "category",
        "article",
        "internal",
        "external",
    };

    private static readonly HashSet<string> AllowedMediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MediaImage,
        MediaVideo,
    };

    private static readonly string[] ForbiddenCtaSchemes =
    [
        "javascript:",
        "data:",
        "vbscript:",
    ];

    /// <summary>نوع و هدف CTA را اعتبارسنجی و نرمال می‌کند.</summary>
    public static (string CtaType, string? CtaTarget) ValidateCta(string? ctaType, string? ctaTarget)
    {
        var normalizedType = string.IsNullOrWhiteSpace(ctaType) ? CtaNone : ctaType.Trim().ToLowerInvariant();
        if (normalizedType.Length > CtaTypeMaxLength || !AllowedCtaTypes.Contains(normalizedType))
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));

        if (string.Equals(normalizedType, CtaNone, StringComparison.Ordinal))
            return (CtaNone, null);

        if (string.IsNullOrWhiteSpace(ctaTarget))
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));

        var normalizedTarget = ctaTarget.Trim();
        if (normalizedTarget.Length > CtaTargetMaxLength)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));

        foreach (var scheme in ForbiddenCtaSchemes)
        {
            if (normalizedTarget.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
        }

        return (normalizedType, normalizedTarget);
    }

    /// <summary>نوع رسانه را اعتبارسنجی می‌کند.</summary>
    public static string ValidateMediaType(string mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType) || !AllowedMediaTypes.Contains(mediaType.Trim()))
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
        return mediaType.Trim().ToLowerInvariant();
    }

    /// <summary>آیا locale استوری با locale درخواست عمومی سازگار است.</summary>
    public static bool MatchesLocale(string? storyLocale, string? requestLocale)
    {
        if (string.IsNullOrWhiteSpace(storyLocale))
            return true;
        if (string.IsNullOrWhiteSpace(requestLocale))
            return true;

        var story = storyLocale.Trim();
        var request = requestLocale.Trim();
        if (string.Equals(story, request, StringComparison.OrdinalIgnoreCase))
            return true;

        static string Language(string value)
        {
            var dash = value.IndexOf('-');
            return dash < 0 ? value : value[..dash];
        }

        return string.Equals(Language(story), Language(request), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>آیا market استوری با فیلتر درخواست سازگار است.</summary>
    public static bool MatchesMarket(string? storyMarket, string? requestMarket)
    {
        if (string.IsNullOrWhiteSpace(requestMarket))
            return true;
        if (string.IsNullOrWhiteSpace(storyMarket))
            return true;
        return string.Equals(storyMarket.Trim(), requestMarket.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
