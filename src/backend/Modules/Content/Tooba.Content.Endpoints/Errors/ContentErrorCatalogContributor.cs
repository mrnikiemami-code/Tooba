using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Endpoints.Errors;

/// <summary>Canonical Content error catalog — exactly one descriptor owner per Content code.</summary>
public sealed class ContentErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(ContentErrorCodes.AuthorizationDenied, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden, "Content authorization denied."),
        D(ContentErrorCodes.AuthorizationUnavailable, ErrorClassification.Platform, StatusCodes.Status503ServiceUnavailable, "Content authorization unavailable."),
        D(ContentErrorCodes.ArticleMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Article was not found."),
        D(ContentErrorCodes.SlugDuplicate, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Article slug already exists."),
        D(ContentErrorCodes.CreateRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Article create was rejected."),
        D(ContentErrorCodes.UpdateRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Article update was rejected."),
        D(ContentErrorCodes.DeleteRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Article delete was rejected."),
        D(ContentErrorCodes.ArticleMediaRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Article media change was rejected."),
        D(ContentErrorCodes.CommentRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Comment operation was rejected."),
        D(ContentErrorCodes.LocaleLocked, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Article locale is locked."),
        D(ContentErrorCodes.UnsafeBodyMedia, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Article body contains unsafe media."),
        D(ContentErrorCodes.MediaNotFound, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Media asset was not found."),
        D(ContentErrorCodes.DeleteNotAllowed, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Article delete is not allowed."),
        D(ContentErrorCodes.AlreadyArchived, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Article is already archived."),
        D(ContentErrorCodes.ArchiveNotAllowed, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Article archive is not allowed."),
        D(ContentErrorCodes.GalleryItemMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Gallery item was not found."),
        D(ContentErrorCodes.PublishNotReady, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Article is not ready to publish."),
        D(ContentErrorCodes.PublishInvalidSchedule, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Publish schedule is invalid."),
        D(ContentErrorCodes.PublishForbidden, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden, "Publish is forbidden."),
        D(ContentErrorCodes.UnpublishInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Unpublish is invalid for current state."),
        D(ContentErrorCodes.PreviewUnavailable, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Preview is unavailable."),
        D(ContentErrorCodes.CategoryNotFound, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Category was not found."),
        D(ContentErrorCodes.CategorySlugDuplicate, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Category slug already exists."),
        D(ContentErrorCodes.CategoryCycleDetected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Category cycle detected."),
        D(ContentErrorCodes.CategoryCrossLanguageParent, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Category parent language mismatch."),
        D(ContentErrorCodes.CategorySelfParent, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Category cannot be its own parent."),
        D(ContentErrorCodes.CategoryDescendantParent, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Category parent is a descendant."),
        D(ContentErrorCodes.CategoryMaxDepthExceeded, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Category max depth exceeded."),
        D(ContentErrorCodes.CategoryInvalidParent, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Category parent is invalid."),
        D(ContentErrorCodes.CategoryInactive, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Category is inactive."),
        D(ContentErrorCodes.CategoryLanguageMismatch, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Category language mismatch."),
        D(ContentErrorCodes.CategoryHasArticles, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Category has articles."),
        D(ContentErrorCodes.CategoryHasChildren, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Category has children."),
        D(ContentErrorCodes.CategoryInvalidLanguage, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Category language is invalid."),
        D(ContentErrorCodes.CategoryInvalidName, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Category name is invalid."),
        D(ContentErrorCodes.CategoryInvalidSlug, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Category slug is invalid."),
        D(ContentErrorCodes.CategoryInvalidShortDescription, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Category short description is invalid."),
        D(ContentErrorCodes.CategoryInvalidDescription, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Category description is invalid."),
        D(ContentErrorCodes.CategoryInvalidSeoTitle, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Category SEO title is invalid."),
        D(ContentErrorCodes.CategoryInvalidSeoDescription, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Category SEO description is invalid."),
        D(ContentErrorCodes.CategoryInvalidField, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Category field is invalid."),
        D(ContentErrorCodes.AuthorNotFound, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Author was not found."),
        D(ContentErrorCodes.AuthorSlugDuplicate, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Author slug already exists."),
        D(ContentErrorCodes.AuthorInactive, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Author is inactive."),
        D(ContentErrorCodes.AuthorRequiredForPublish, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Author is required to publish."),
        D(ContentErrorCodes.AuthorInvalidDisplayName, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Author display name is invalid."),
        D(ContentErrorCodes.AuthorInvalidSlug, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Author slug is invalid."),
        D(ContentErrorCodes.AuthorInvalidShortBio, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Author short bio is invalid."),
        D(ContentErrorCodes.AuthorInvalidFullBio, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Author full bio is invalid."),
        D(ContentErrorCodes.AuthorInvalidUrl, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Author URL is invalid."),
        D(ContentErrorCodes.AuthorInvalidField, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Author field is invalid."),
        D(ContentErrorCodes.TagNotFound, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Tag was not found."),
        D(ContentErrorCodes.TagDuplicateName, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Tag name already exists."),
        D(ContentErrorCodes.TagLanguageMismatch, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Tag language mismatch."),
        D(ContentErrorCodes.TagInactive, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Tag is inactive."),
        D(ContentErrorCodes.TagInvalidLanguage, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Tag language is invalid."),
        D(ContentErrorCodes.TagInvalidName, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Tag name is invalid."),
        D(ContentErrorCodes.TagArticleNotFound, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Tag article was not found."),
        D(ContentErrorCodes.CommentNotFound, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Comment was not found."),
        D(ContentErrorCodes.CommentArticleNotFound, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Comment article was not found."),
        D(ContentErrorCodes.CommentInvalidTransition, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Comment transition is invalid."),
        D(ContentErrorCodes.CommentInvalidPayload, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Comment payload is invalid."),
        D(ContentErrorCodes.CommentForbidden, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden, "Comment moderation is forbidden."),
    ];

    private static ErrorDescriptor D(
        string code,
        ErrorClassification classification,
        int status,
        string fallback) =>
        new(
            Code: code,
            Classification: classification,
            HttpStatus: status,
            LocalizationKey: code,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: fallback);
}
