using Tooba.Reviews.Application;
using Tooba.Reviews.Domain;
using Tooba.Reviews.Infrastructure.Persistence;
using Tooba.Host.Reviews;
using Tooba.Host.Storefront;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.Reviews.Infrastructure;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>پوشش قواعد خالص، قرارداد عمومی و schema ماژول Reviews.</summary>
public sealed class ReviewsFoundationTests
{
    /// <summary>مرزهای یک و پنج پذیرفته و صفر و شش رد می‌شوند.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Rating_boundaries_are_accepted(int rating)
    {
        var review = Create(rating);
        Assert.Equal(rating, review.Rating);
        Assert.Equal(ReviewStatus.Pending, review.Status);
    }

    /// <summary>مقادیر بیرون دامنه رد می‌شوند.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Invalid_ratings_are_rejected(int rating) =>
        Assert.Throws<InvalidOperationException>(() => Create(rating));

    /// <summary>چرخهٔ تعدیل فقط از Pending یک‌بار عبور می‌کند و دلیل رد اجباری است.</summary>
    [Fact]
    public void Moderation_lifecycle_is_safe()
    {
        var published = Create(5);
        published.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(ReviewStatus.Published, published.Status);
        Assert.Throws<InvalidOperationException>(() => published.Reject(Guid.NewGuid(), "دلیل", DateTimeOffset.UtcNow));

        var rejected = Create(1);
        Assert.Throws<InvalidOperationException>(() => rejected.Reject(Guid.NewGuid(), "", DateTimeOffset.UtcNow));
        rejected.Reject(Guid.NewGuid(), "محتوای نامناسب", DateTimeOffset.UtcNow);
        Assert.Equal(ReviewStatus.Rejected, rejected.Status);
    }

    /// <summary>میانگین، تعداد و همهٔ خانه‌های توزیع قطعی هستند.</summary>
    [Fact]
    public void Published_summary_is_correct()
    {
        var summary = ReviewSummaryCalculator.Calculate([(5, 2L), (3, 1L), (1, 1L)]);
        Assert.Equal(4, summary.Count);
        Assert.Equal(3.5m, summary.Average);
        Assert.Equal(new long[] { 1, 0, 1, 0, 2 }, Enumerable.Range(1, 5).Select(x => summary.Distribution[x]));
    }

    /// <summary>DTO عمومی شناسهٔ Actor، Party و یادداشت تعدیل ندارد.</summary>
    [Fact]
    public void Public_contract_does_not_leak_internal_identity()
    {
        var names = typeof(PublishedReview).GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain("AuthorUserId", names);
        Assert.DoesNotContain("PartyId", names);
        Assert.DoesNotContain("ModerationReason", names);
        Assert.Equal("reviews", ReviewsDbContext.Schema);
    }

    /// <summary>قرارداد گروهی مانع نیاز Composer به فراخوانی جداگانه برای هر محصول است.</summary>
    [Fact]
    public void Batch_summary_contract_and_storefront_fields_are_explicit()
    {
        var method = typeof(IReviewDirectory).GetMethod(nameof(IReviewDirectory.GetPublishedSummariesAsync));
        Assert.NotNull(method);
        Assert.Equal(typeof(IReadOnlyCollection<Guid>), method!.GetParameters()[0].ParameterType);
        var card = typeof(StorefrontProductCard).GetProperties().Select(x => x.Name).ToArray();
        Assert.Contains("AverageRating", card);
        Assert.Contains("ReviewCount", card);
    }

    /// <summary>شکل پاسخ عمومی top-level است و هیچ شناسهٔ داخلی Actor ندارد.</summary>
    [Fact]
    public void Public_host_response_shape_is_frontend_compatible_and_private()
    {
        var response = typeof(PublicReviewsResponse).GetProperties().Select(x => x.Name).ToArray();
        Assert.Equal(
            ["AverageRating", "ReviewCount", "RatingDistribution", "Reviews", "Page", "PageSize", "TotalCount"],
            response);
        var item = typeof(PublicReviewItem).GetProperties().Select(x => x.Name).ToArray();
        Assert.Contains("VerifiedPurchase", item);
        Assert.DoesNotContain("AuthorUserId", item);
        Assert.DoesNotContain("ModerationReason", item);
    }

    /// <summary>ثبت فقط ProductId و محتوا می‌پذیرد؛ Actor و نام عمومی ورودی کاربر نیستند.</summary>
    [Fact]
    public void Submission_contract_has_no_actor_or_display_name_authority()
    {
        var names = typeof(SubmitProductReview).GetProperties().Select(x => x.Name).ToArray();
        Assert.Equal(["ProductId", "Rating", "Title", "Body"], names);
        Assert.DoesNotContain("ActorUserId", names);
        Assert.DoesNotContain("AuthorDisplayName", names);
    }

    /// <summary>صف مدیر شمارش، عنوان محصول، وضعیت و alias خرید تأییدشده دارد.</summary>
    [Fact]
    public void Admin_host_response_is_a_titled_page()
    {
        var page = typeof(AdminReviewsResponse).GetProperties().Select(x => x.Name).ToArray();
        Assert.Equal(["Reviews", "Page", "PageSize", "TotalCount"], page);
        var item = typeof(AdminReviewItem).GetProperties().Select(x => x.Name).ToArray();
        Assert.Contains("ProductTitle", item);
        Assert.Contains("Status", item);
        Assert.Contains("VerifiedPurchase", item);
        Assert.DoesNotContain("ProductId", item);
    }

    /// <summary>بدنهٔ رد اختیاری است تا POST بدون JSON نیز معتبر بماند.</summary>
    [Fact]
    public void Reject_body_parameter_is_optional()
    {
        var method = typeof(ReviewEndpoints).GetMethod("RejectAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var body = method!.GetParameters().Single(x => x.ParameterType == typeof(RejectReviewRequest));
        Assert.True(body.HasDefaultValue);
        Assert.Null(body.DefaultValue);
    }

    private static ProductReview Create(int rating) => ProductReview.Create(
        Guid.NewGuid(), Guid.NewGuid(), "نویسنده", rating, null, "متن بررسی معتبر",
        false, null, DateTimeOffset.UtcNow);
}

/// <summary>آزمون PostgreSQL برای فیلتر Published و گروه‌بندی یک‌مرحله‌ای خلاصه‌ها.</summary>
[Collection("PostgresSerial")]
public sealed class ReviewsPostgresTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder().WithImage("postgres:16-alpine").WithDatabase("tooba_reviews")
                .WithUsername("tooba").WithPassword("dev-placeholder").Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception) { _dockerAvailable = false; }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }

    /// <summary>Pending و Rejected در خلاصهٔ گروهی چند محصول شرکت نمی‌کنند.</summary>
    [SkippableFact]
    public async Task Batch_summary_counts_published_only_and_omits_zero_review_products()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var builder = new DbContextOptionsBuilder<ReviewsDbContext>();
        ToobaNpgsql.ConfigureModuleContext(builder, _container!.GetConnectionString(), ReviewsDbContext.Schema, typeof(ReviewsDbContext));
        await using var db = new ReviewsDbContext(builder.Options);
        await db.Database.MigrateAsync();
        var product = Guid.NewGuid();
        var zeroProduct = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        foreach (var (rating, status) in new[] { (5, ReviewStatus.Published), (3, ReviewStatus.Published), (1, ReviewStatus.Pending), (2, ReviewStatus.Rejected) })
        {
            var review = ProductReview.Create(product, Guid.NewGuid(), "مشتری", rating, null, "متن معتبر", false, null, now);
            if (status == ReviewStatus.Published) review.Publish(Guid.NewGuid(), now);
            if (status == ReviewStatus.Rejected) review.Reject(Guid.NewGuid(), "رد", now);
            db.Reviews.Add(review);
        }
        await db.SaveChangesAsync();

        var directory = new ReviewDirectory(db, null!, null!);
        var summaries = await directory.GetPublishedSummariesAsync([product, zeroProduct], CancellationToken.None);
        Assert.Equal(2, summaries[product].ReviewCount);
        Assert.Equal(4m, summaries[product].AverageRating);
        Assert.DoesNotContain(zeroProduct, summaries.Keys);
    }
}
