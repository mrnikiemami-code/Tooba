using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Reviews.Domain;

namespace Tooba.Reviews.Infrastructure.Persistence;

/// <summary>DbContext مالک schema مستقل reviews.</summary>
public sealed class ReviewsDbContext : DbContext
{
    /// <summary>schema اختصاصی Reviews.</summary>
    public const string Schema = "reviews";
    /// <summary>DbContext را می‌سازد.</summary>
    public ReviewsDbContext(DbContextOptions<ReviewsDbContext> options) : base(options) { }
    /// <summary>بررسی‌های محصول.</summary>
    public DbSet<ProductReview> Reviews => Set<ProductReview>();
    /// <summary>Outbox ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.ToTable("product_reviews");
            entity.HasKey(x => x.ReviewId);
            entity.Property(x => x.ReviewId).ValueGeneratedNever();
            entity.Property(x => x.AuthorDisplayName).HasMaxLength(100);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.Body).HasMaxLength(4000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.ModerationReason).HasMaxLength(500);
            entity.HasIndex(x => new { x.ProductId, x.AuthorUserId }).IsUnique();
            entity.HasIndex(x => new { x.ProductId, x.Status, x.CreatedAt });
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ design-time مهاجرت Reviews.</summary>
public sealed class ReviewsDbContextFactory : IDesignTimeDbContextFactory<ReviewsDbContext>
{
    /// <inheritdoc />
    public ReviewsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ReviewsDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, ToobaNpgsql.DesignTimeConnectionString(), ReviewsDbContext.Schema, typeof(ReviewsDbContext));
        return new ReviewsDbContext(options.Options);
    }
}
