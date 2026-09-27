namespace Tooba.Content.Contracts.Errors;

/// <summary>Stable Content semantic error codes — single catalog owner for HTTP presentation.</summary>
public static class ContentErrorCodes
{
    public const string AuthorizationDenied = "content.authorization.denied";
    public const string AuthorizationUnavailable = "content.authorization.unavailable";

    public const string ArticleMissing = "content.article.missing";
    public const string SlugDuplicate = "content.slug.duplicate";
    public const string CreateRejected = "content.create.rejected";
    public const string UpdateRejected = "content.update.rejected";
    public const string DeleteRejected = "content.delete.rejected";
    public const string ArticleMediaRejected = "content.article.media.rejected";
    public const string CommentRejected = "content.comment.rejected";

    public const string LocaleLocked = "content.article.locale_locked";
    public const string UnsafeBodyMedia = "content.article.unsafe_body_media";
    public const string MediaNotFound = "content.article.media_not_found";
    public const string DeleteNotAllowed = "content.article.delete_not_allowed";
    public const string AlreadyArchived = "content.article.already_archived";
    public const string ArchiveNotAllowed = "content.article.archive_not_allowed";
    public const string GalleryItemMissing = "content.article.gallery_item_missing";

    public const string PublishNotReady = "content.publish.not_ready";
    public const string PublishInvalidSchedule = "content.publish.invalid_schedule";
    public const string PublishForbidden = "content.publish.forbidden";
    public const string UnpublishInvalid = "content.unpublish.invalid_state";
    public const string PreviewUnavailable = "content.preview.unavailable";

    public const string CategoryNotFound = "content.category.not_found";
    public const string CategorySlugDuplicate = "content.category.slug_duplicate";
    public const string CategoryCycleDetected = "content.category.cycle_detected";
    public const string CategoryCrossLanguageParent = "content.category.cross_language_parent";
    public const string CategorySelfParent = "content.category.self_parent";
    public const string CategoryDescendantParent = "content.category.descendant_parent";
    public const string CategoryMaxDepthExceeded = "content.category.max_depth_exceeded";
    public const string CategoryInvalidParent = "content.category.invalid_parent";
    public const string CategoryInactive = "content.category.inactive";
    public const string CategoryLanguageMismatch = "content.category.language_mismatch";
    public const string CategoryHasArticles = "content.category.has_articles";
    public const string CategoryHasChildren = "content.category.has_children";
    public const string CategoryInvalidLanguage = "content.category.invalid_language";
    public const string CategoryInvalidName = "content.category.invalid_name";
    public const string CategoryInvalidSlug = "content.category.invalid_slug";
    public const string CategoryInvalidShortDescription = "content.category.invalid_short_description";
    public const string CategoryInvalidDescription = "content.category.invalid_description";
    public const string CategoryInvalidSeoTitle = "content.category.invalid_seo_title";
    public const string CategoryInvalidSeoDescription = "content.category.invalid_seo_description";
    public const string CategoryInvalidField = "content.category.invalid_field";

    public const string AuthorNotFound = "content.author.not_found";
    public const string AuthorSlugDuplicate = "content.author.slug_duplicate";
    public const string AuthorInactive = "content.author.inactive";
    public const string AuthorRequiredForPublish = "content.author.required_for_publish";
    public const string AuthorInvalidDisplayName = "content.author.invalid_display_name";
    public const string AuthorInvalidSlug = "content.author.invalid_slug";
    public const string AuthorInvalidShortBio = "content.author.invalid_short_bio";
    public const string AuthorInvalidFullBio = "content.author.invalid_full_bio";
    public const string AuthorInvalidUrl = "content.author.invalid_url";
    public const string AuthorInvalidField = "content.author.invalid_field";

    public const string TagNotFound = "content.tag.not_found";
    public const string TagDuplicateName = "content.tag.duplicate_name";
    public const string TagLanguageMismatch = "content.tag.language_mismatch";
    public const string TagInactive = "content.tag.inactive";
    public const string TagInvalidLanguage = "content.tag.invalid_language";
    public const string TagInvalidName = "content.tag.invalid_name";
    public const string TagArticleNotFound = "content.tag.article_not_found";

    public const string CommentNotFound = "content.comment.not_found";
    public const string CommentArticleNotFound = "content.comment.article_not_found";
    public const string CommentInvalidTransition = "content.comment.invalid_transition";
    public const string CommentInvalidPayload = "content.comment.invalid_payload";
    public const string CommentForbidden = "content.comment.forbidden";

    /// <summary>Exact-equality set for mapping InvalidOperationException.Message when directories still throw code-as-message.</summary>
    public static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        AuthorizationDenied, AuthorizationUnavailable,
        ArticleMissing, SlugDuplicate, CreateRejected, UpdateRejected, DeleteRejected,
        ArticleMediaRejected, CommentRejected,
        LocaleLocked, UnsafeBodyMedia, MediaNotFound, DeleteNotAllowed, AlreadyArchived, ArchiveNotAllowed,
        GalleryItemMissing,
        PublishNotReady, PublishInvalidSchedule, PublishForbidden, UnpublishInvalid, PreviewUnavailable,
        CategoryNotFound, CategorySlugDuplicate, CategoryCycleDetected, CategoryCrossLanguageParent,
        CategorySelfParent, CategoryDescendantParent, CategoryMaxDepthExceeded, CategoryInvalidParent,
        CategoryInactive, CategoryLanguageMismatch, CategoryHasArticles, CategoryHasChildren,
        CategoryInvalidLanguage, CategoryInvalidName, CategoryInvalidSlug, CategoryInvalidShortDescription,
        CategoryInvalidDescription, CategoryInvalidSeoTitle, CategoryInvalidSeoDescription, CategoryInvalidField,
        AuthorNotFound, AuthorSlugDuplicate, AuthorInactive, AuthorRequiredForPublish,
        AuthorInvalidDisplayName, AuthorInvalidSlug, AuthorInvalidShortBio, AuthorInvalidFullBio,
        AuthorInvalidUrl, AuthorInvalidField,
        TagNotFound, TagDuplicateName, TagLanguageMismatch, TagInactive, TagInvalidLanguage, TagInvalidName,
        TagArticleNotFound,
        CommentNotFound, CommentArticleNotFound, CommentInvalidTransition, CommentInvalidPayload, CommentForbidden,
        "localization.language.inactive",
        "localization.language.not_found",
    };

    /// <summary>True when message is exactly a known stable Content (or consumed Localization) code.</summary>
    public static bool IsKnownCode(string? message) =>
        !string.IsNullOrWhiteSpace(message) && KnownCodes.Contains(message);
}
