using Tooba.Content.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P08-T014: قوانین واحد آمادگی انتشار (بدون Docker).</summary>
public sealed class ArticlePublicationReadinessRulesTests
{
    [Fact]
    public void Incomplete_draft_blocks_publish_with_required_missing()
    {
        var readiness = ArticlePublicationReadinessRules.Evaluate(new ArticlePublicationReadinessInput(
            Title: "عنوان",
            Excerpt: "چکیده",
            Body: "",
            Slug: "sample-guide",
            Locale: "fa-IR",
            AuthorId: null,
            CategoryId: null,
            CoverMediaAssetId: null,
            SeoImageMediaAssetId: null,
            SeoTitle: null,
            SeoDescription: null,
            Status: ContentPublicationStatus.Draft,
            PublishDate: DateTimeOffset.UtcNow,
            LanguageIsActive: true,
            UtcNow: DateTimeOffset.UtcNow));

        Assert.False(readiness.CanPublish);
        Assert.Contains(readiness.RequiredMissing, c => c.Key == ArticlePublicationCodes.Body);
        Assert.Contains(readiness.RequiredMissing, c => c.Key == ArticlePublicationCodes.Author);
        Assert.Contains(readiness.RecommendedMissing, c => c.Key == ArticlePublicationCodes.Category);
        Assert.Contains(readiness.RecommendedMissing, c => c.Key == ArticlePublicationCodes.FeaturedImage);
    }

    [Fact]
    public void Complete_mandatory_fields_allow_publish()
    {
        var readiness = ArticlePublicationReadinessRules.Evaluate(new ArticlePublicationReadinessInput(
            Title: "Title",
            Excerpt: "Excerpt",
            Body: "<p>Hello body</p>",
            Slug: "en-guide",
            Locale: "en-US",
            AuthorId: Guid.NewGuid(),
            CategoryId: Guid.NewGuid(),
            CoverMediaAssetId: Guid.NewGuid(),
            SeoImageMediaAssetId: null,
            SeoTitle: "SEO",
            SeoDescription: "Desc",
            Status: ContentPublicationStatus.Draft,
            PublishDate: DateTimeOffset.UtcNow.AddHours(2),
            LanguageIsActive: true,
            UtcNow: DateTimeOffset.UtcNow));

        Assert.True(readiness.CanPublish);
        Assert.Empty(readiness.RequiredMissing);
        Assert.Empty(readiness.RecommendedMissing);
        Assert.NotNull(readiness.Score);
    }

    [Fact]
    public void Inactive_language_and_archived_block_publish()
    {
        var inactive = ArticlePublicationReadinessRules.Evaluate(Base(languageActive: false));
        Assert.False(inactive.CanPublish);
        Assert.Contains(inactive.RequiredMissing, c => c.Key == ArticlePublicationCodes.LanguageActive);

        var archived = ArticlePublicationReadinessRules.Evaluate(
            Base(status: ContentPublicationStatus.Archived));
        Assert.False(archived.CanPublish);
        Assert.Contains(archived.RequiredMissing, c => c.Key == ArticlePublicationCodes.NotArchived);
    }

    [Fact]
    public void Empty_html_body_is_not_meaningful()
    {
        Assert.False(ArticlePublicationReadinessRules.HasMeaningfulBody("<p> </p>"));
        Assert.True(ArticlePublicationReadinessRules.HasMeaningfulBody("<p>متن</p>"));
    }

    private static ArticlePublicationReadinessInput Base(
        bool languageActive = true,
        ContentPublicationStatus status = ContentPublicationStatus.Draft) =>
        new(
            "عنوان",
            "چکیده",
            "<p>بدنه</p>",
            "ready-slug",
            "fa-IR",
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            status,
            DateTimeOffset.UtcNow,
            languageActive,
            DateTimeOffset.UtcNow);
}
