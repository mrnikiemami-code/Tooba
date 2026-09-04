using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Content.Domain;
using Tooba.Persistence;

namespace Tooba.Content.Infrastructure.Persistence;

/// <summary>DbContext مالک schema مستقل content.</summary>
public sealed class ContentDbContext : DbContext
{
    /// <summary>schema اختصاصی Content.</summary>
    public const string Schema = "content";

    /// <summary>DbContext را می‌سازد.</summary>
    public ContentDbContext(DbContextOptions<ContentDbContext> options) : base(options) { }

    /// <summary>مقالات تحریری.</summary>
    public DbSet<ContentArticle> Articles => Set<ContentArticle>();

    /// <summary>دسته‌بندی‌های مقاله.</summary>
    public DbSet<ContentCategory> Categories => Set<ContentCategory>();

    /// <summary>نویسندگان مقاله.</summary>
    public DbSet<ContentAuthor> Authors => Set<ContentAuthor>();

    /// <summary>برچسب‌های محتوا.</summary>
    public DbSet<ContentTag> Tags => Set<ContentTag>();

    /// <summary>انتساب برچسب به مقاله.</summary>
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();

    /// <summary>گالری رسانهٔ مقالات.</summary>
    public DbSet<ContentArticleMediaItem> ArticleMedia => Set<ContentArticleMediaItem>();

    /// <summary>تاریخچهٔ چرخهٔ عمر مقالات.</summary>
    public DbSet<ContentArticleHistoryEntry> ArticleHistory => Set<ContentArticleHistoryEntry>();

    /// <summary>نظرات مقالات.</summary>
    public DbSet<ArticleComment> ArticleComments => Set<ArticleComment>();

    /// <summary>Outbox ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<ContentArticle>(entity =>
        {
            entity.ToTable("articles");
            entity.HasKey(x => x.ArticleId);
            entity.Property(x => x.ArticleId).ValueGeneratedNever();
            entity.Property(x => x.Slug).HasMaxLength(ContentArticle.SlugMaxLength).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(ContentArticle.TitleMaxLength).IsRequired();
            entity.Property(x => x.Excerpt).HasMaxLength(ContentArticle.ExcerptMaxLength).IsRequired();
            entity.Property(x => x.Body).HasMaxLength(ContentArticle.BodyMaxLength).IsRequired();
            entity.Property(x => x.Locale).HasMaxLength(ContentArticle.LocaleMaxLength).IsRequired();
            entity.Property(x => x.SeoTitle).HasMaxLength(ContentArticle.SeoTitleMaxLength);
            entity.Property(x => x.SeoDescription).HasMaxLength(ContentArticle.SeoDescriptionMaxLength);
            entity.Property(x => x.Category).HasMaxLength(ContentArticle.CategoryMaxLength);
            entity.Property(x => x.CategoryId).HasColumnName("category_id");
            entity.Property(x => x.AuthorId).HasColumnName("author_id");
            entity.Property(x => x.AuthorDisplayName).HasMaxLength(ContentArticle.AuthorDisplayNameMaxLength).IsRequired();
            entity.Property(x => x.TagsCsv).HasMaxLength(256);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => new { x.Slug, x.Locale }).IsUnique();
            entity.HasIndex(x => new { x.Locale, x.Slug });
            entity.HasIndex(x => new { x.Status, x.PublishDate });
            entity.HasIndex(x => new { x.Status, x.Category, x.PublishDate });
            entity.HasIndex(x => x.CategoryId);
            entity.HasIndex(x => x.AuthorId);
            entity.Property(x => x.SeoImageMediaAssetId).HasColumnName("seo_image_media_asset_id");
        });
        modelBuilder.Entity<ContentArticleMediaItem>(entity =>
        {
            entity.ToTable("article_media");
            entity.HasKey(x => new { x.ArticleId, x.MediaAssetId });
            entity.Property(x => x.ArticleId).HasColumnName("article_id");
            entity.Property(x => x.MediaAssetId).HasColumnName("media_asset_id");
            entity.Property(x => x.DisplayOrder).HasColumnName("display_order");
            entity.Property(x => x.AltText).HasMaxLength(ContentArticleMediaItem.AltTextMaxLength).HasColumnName("alt_text");
            entity.Property(x => x.Caption).HasMaxLength(ContentArticleMediaItem.CaptionMaxLength).HasColumnName("caption");
            entity.HasIndex(x => x.MediaAssetId);
            entity.HasIndex(x => new { x.ArticleId, x.DisplayOrder });
        });
        modelBuilder.Entity<ContentAuthor>(entity =>
        {
            entity.ToTable("authors");
            entity.HasKey(x => x.AuthorId);
            entity.Property(x => x.AuthorId).ValueGeneratedNever();
            entity.Property(x => x.DisplayName).HasMaxLength(ContentAuthor.DisplayNameMaxLength).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(ContentAuthor.SlugMaxLength).IsRequired();
            entity.Property(x => x.ShortBio).HasMaxLength(ContentAuthor.ShortBioMaxLength);
            entity.Property(x => x.FullBio).HasMaxLength(ContentAuthor.FullBioMaxLength);
            entity.Property(x => x.WebsiteUrl).HasMaxLength(ContentAuthor.UrlMaxLength);
            entity.Property(x => x.InstagramUrl).HasMaxLength(ContentAuthor.UrlMaxLength);
            entity.Property(x => x.TwitterUrl).HasMaxLength(ContentAuthor.UrlMaxLength);
            entity.Property(x => x.LinkedInUrl).HasMaxLength(ContentAuthor.UrlMaxLength);
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => new { x.IsActive, x.DisplayName });
        });
        modelBuilder.Entity<ContentCategory>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(x => x.CategoryId);
            entity.Property(x => x.CategoryId).ValueGeneratedNever();
            entity.Property(x => x.LanguageCode).HasMaxLength(ContentCategory.LanguageCodeMaxLength).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(ContentCategory.NameMaxLength).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(ContentCategory.SlugMaxLength).IsRequired();
            entity.Property(x => x.ShortDescription).HasMaxLength(ContentCategory.ShortDescriptionMaxLength);
            entity.Property(x => x.Description).HasMaxLength(ContentCategory.DescriptionMaxLength);
            entity.Property(x => x.SeoTitle).HasMaxLength(ContentCategory.SeoTitleMaxLength);
            entity.Property(x => x.SeoDescription).HasMaxLength(ContentCategory.SeoDescriptionMaxLength);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => new { x.LanguageCode, x.Slug }).IsUnique();
            entity.HasIndex(x => new { x.LanguageCode, x.ParentCategoryId, x.SortOrder });
        });
        modelBuilder.Entity<ContentTag>(entity =>
        {
            entity.ToTable("tags");
            entity.HasKey(x => x.TagId);
            entity.Property(x => x.TagId).ValueGeneratedNever();
            entity.Property(x => x.LanguageCode).HasMaxLength(ContentTag.LanguageCodeMaxLength).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(ContentTag.NameMaxLength).IsRequired();
            entity.Property(x => x.NormalizedName).HasMaxLength(ContentTag.NormalizedNameMaxLength).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(ContentTag.SlugMaxLength);
            entity.HasIndex(x => new { x.LanguageCode, x.NormalizedName }).IsUnique();
            entity.HasIndex(x => new { x.LanguageCode, x.IsActive, x.Name });
        });
        modelBuilder.Entity<ArticleTag>(entity =>
        {
            entity.ToTable("article_tags");
            entity.HasKey(x => new { x.ArticleId, x.TagId });
            entity.Property(x => x.ArticleId).HasColumnName("article_id");
            entity.Property(x => x.TagId).HasColumnName("tag_id");
            entity.Property(x => x.AssignedAt).HasColumnName("assigned_at");
            entity.HasIndex(x => x.TagId);
            entity.HasOne<ContentArticle>().WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ContentTag>().WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ContentArticleHistoryEntry>(entity =>
        {
            entity.ToTable("article_history");
            entity.HasKey(x => x.HistoryId);
            entity.Property(x => x.HistoryId).ValueGeneratedNever().HasColumnName("history_id");
            entity.Property(x => x.ArticleId).HasColumnName("article_id");
            entity.Property(x => x.EventType).HasMaxLength(64).IsRequired().HasColumnName("event_type");
            entity.Property(x => x.SummaryFa).HasMaxLength(512).IsRequired().HasColumnName("summary_fa");
            entity.Property(x => x.SummaryEn).HasMaxLength(512).IsRequired().HasColumnName("summary_en");
            entity.Property(x => x.PreviousState).HasMaxLength(64).HasColumnName("previous_state");
            entity.Property(x => x.NewState).HasMaxLength(64).HasColumnName("new_state");
            entity.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(x => x.ActorDisplayName).HasMaxLength(120).HasColumnName("actor_display_name");
            entity.Property(x => x.OccurredAt).HasColumnName("occurred_at");
            entity.HasIndex(x => new { x.ArticleId, x.OccurredAt });
            entity.HasOne<ContentArticle>().WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ArticleComment>(entity =>
        {
            entity.ToTable("article_comments");
            entity.HasKey(x => x.CommentId);
            entity.Property(x => x.CommentId).ValueGeneratedNever().HasColumnName("comment_id");
            entity.Property(x => x.ArticleId).HasColumnName("article_id");
            entity.Property(x => x.AuthorPartyId).HasColumnName("author_party_id");
            entity.Property(x => x.DisplayName).HasMaxLength(ArticleComment.DisplayNameMaxLength).IsRequired().HasColumnName("display_name");
            entity.Property(x => x.Body).HasMaxLength(ArticleComment.BodyMaxLength).IsRequired().HasColumnName("body");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).HasColumnName("status");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.ModeratedAt).HasColumnName("moderated_at");
            entity.Property(x => x.ModeratedByUserId).HasColumnName("moderated_by_user_id");
            entity.Property(x => x.ModerationNote).HasMaxLength(ArticleComment.NoteMaxLength).HasColumnName("moderation_note");
            entity.HasIndex(x => new { x.ArticleId, x.CreatedAt });
            entity.HasIndex(x => new { x.ArticleId, x.Status, x.CreatedAt });
            entity.HasOne<ContentArticle>().WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.Cascade);
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ design-time مهاجرت Content.</summary>
public sealed class ContentDbContextFactory : IDesignTimeDbContextFactory<ContentDbContext>
{
    /// <inheritdoc />
    public ContentDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, ToobaNpgsql.DesignTimeConnectionString(), ContentDbContext.Schema, typeof(ContentDbContext));
        return new ContentDbContext(options.Options);
    }
}
