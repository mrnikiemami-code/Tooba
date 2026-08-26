using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.ProductQnA.Domain;

namespace Tooba.ProductQnA.Infrastructure.Persistence;

/// <summary>DbContext مالک schema مستقل product_qna.</summary>
public sealed class ProductQnADbContext : DbContext
{
    /// <summary>schema اختصاصی ProductQnA.</summary>
    public const string Schema = "product_qna";

    /// <summary>DbContext را می‌سازد.</summary>
    public ProductQnADbContext(DbContextOptions<ProductQnADbContext> options) : base(options) { }

    /// <summary>پرسش‌های محصول.</summary>
    public DbSet<ProductQuestion> Questions => Set<ProductQuestion>();

    /// <summary>پاسخ‌های پرسش.</summary>
    public DbSet<ProductAnswer> Answers => Set<ProductAnswer>();

    /// <summary>Outbox ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<ProductQuestion>(entity =>
        {
            entity.ToTable("product_questions");
            entity.HasKey(x => x.QuestionId);
            entity.Property(x => x.QuestionId).ValueGeneratedNever();
            entity.Property(x => x.AuthorDisplayName).HasMaxLength(ProductQuestion.AuthorDisplayNameMaxLength);
            entity.Property(x => x.Body).HasMaxLength(ProductQuestion.BodyMaxLength);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.ModerationReason).HasMaxLength(500);
            entity.HasIndex(x => new { x.ProductId, x.Status, x.CreatedAt });
        });
        modelBuilder.Entity<ProductAnswer>(entity =>
        {
            entity.ToTable("product_answers");
            entity.HasKey(x => x.AnswerId);
            entity.Property(x => x.AnswerId).ValueGeneratedNever();
            entity.Property(x => x.AuthorDisplayName).HasMaxLength(ProductAnswer.AuthorDisplayNameMaxLength);
            entity.Property(x => x.Body).HasMaxLength(ProductAnswer.BodyMaxLength);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => x.QuestionId).IsUnique();
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ design-time مهاجرت ProductQnA.</summary>
public sealed class ProductQnADbContextFactory : IDesignTimeDbContextFactory<ProductQnADbContext>
{
    /// <inheritdoc />
    public ProductQnADbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ProductQnADbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, ToobaNpgsql.DesignTimeConnectionString(), ProductQnADbContext.Schema, typeof(ProductQnADbContext));
        return new ProductQnADbContext(options.Options);
    }
}
