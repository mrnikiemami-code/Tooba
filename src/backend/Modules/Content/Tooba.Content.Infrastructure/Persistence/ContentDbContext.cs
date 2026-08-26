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
            entity.Property(x => x.AuthorDisplayName).HasMaxLength(ContentArticle.AuthorDisplayNameMaxLength).IsRequired();
            entity.Property(x => x.TagsCsv).HasMaxLength(256);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => new { x.Status, x.PublishDate });
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
