using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Host.Reviews;
using Tooba.Host.Seller;
using Tooba.Host.Storefront;
using Tooba.Identity.Application;
using Tooba.Persistence;
using Tooba.Reviews.Application;
using Tooba.Reviews.Domain;
using Tooba.Reviews.Infrastructure;
using Tooba.Reviews.Infrastructure.Persistence;
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

    /// <summary>مرز فروشنده فقط لیست دارد؛ پاسخ فروشنده و تعدیل در دامنه نیست.</summary>
    [Fact]
    public void Seller_host_list_exists_without_seller_response_or_moderation_routes()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Reviews", "ReviewEndpoints.cs"));
        Assert.Contains("/v1/seller/reviews", source, StringComparison.Ordinal);
        Assert.Contains("SellerPanelAccess.RequireAuthorizedAsync", source, StringComparison.Ordinal);
        Assert.Contains("ListOwnedProductIdsAsync", source, StringComparison.Ordinal);
        Assert.Contains("SellerResponseSupported: false", source, StringComparison.Ordinal);
        Assert.DoesNotContain("/v1/seller/reviews/", source, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/reviews", source, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/reviews/{reviewId:guid}/publish", source, StringComparison.Ordinal);

        var page = typeof(SellerReviewsResponse).GetProperties().Select(x => x.Name).ToArray();
        Assert.Equal(
            ["Reviews", "Page", "PageSize", "TotalCount", "PublishedCount", "PendingCount", "RejectedCount", "SellerResponseSupported"],
            page);
        var item = typeof(SellerReviewItem).GetProperties().Select(x => x.Name).ToArray();
        Assert.Contains("ProductTitle", item);
        Assert.Contains("StatusLabel", item);
        Assert.DoesNotContain("AuthorUserId", item);
        Assert.DoesNotContain("ProductId", item);
        Assert.DoesNotContain("SellerReply", item);

        Assert.NotNull(typeof(IReviewDirectory).GetMethod(nameof(IReviewDirectory.ListForProductsAsync)));
    }

    /// <summary>فروشندهٔ خارجی از طریق SellerPanelAccess به Party دیگری دسترسی ندارد.</summary>
    [Fact]
    public async Task Foreign_seller_party_header_is_denied_by_seller_panel_access()
    {
        var auth = CreateInMemoryAuth();
        var actorA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0001");
        var sellerA = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
        var sellerB = Guid.Parse("01a030d1-40db-7000-b90c-a0705133f0eb");
        await auth.Writer.WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(actorA),
                Resource = new AuthorizationResource { Type = AuthorizationObjectTypes.Party, Id = sellerA.ToString("D") },
                Relation = AuthorizationRelations.Member,
            },
            CancellationToken.None);

        var request = new DefaultHttpContext().Request;
        request.Headers[SellerPanelAccess.SellerPartyHeader] = sellerB.ToString("D");
        request.Headers[SellerPanelAccess.DevActorHeader] = actorA.ToString("D");
        var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            SellerPanelAccess.RequireAuthorizedAsync(
                request,
                new CurrentAuthenticatedSession(),
                auth.Guard,
                new ReviewsStubHostEnvironment(),
                CancellationToken.None));
        Assert.Equal(403, denied.StatusCode);
        Assert.Equal("seller.authorization.denied", denied.ErrorCode);
    }

    private static ProductReview Create(int rating) => ProductReview.Create(
        Guid.NewGuid(), Guid.NewGuid(), "نویسنده", rating, null, "متن بررسی معتبر",
        false, null, DateTimeOffset.UtcNow);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }

    private static (IAuthorizationTupleWriter Writer, IAuthorizationGuard Guard) CreateInMemoryAuth()
    {
        var telemetry = new AuthorizationInstrumentation();
        var audit = new InMemoryAuthorizationSecurityEventSink();
        var adapter = new InMemoryAuthorizationAdapter(telemetry, audit);
        return (adapter, new AuthorizationGuard(adapter));
    }

    private sealed class ReviewsStubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tooba.Host.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
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

    /// <summary>فهرست فروشنده فقط ProductIdهای مالک را می‌بیند؛ محصول فروشندهٔ دیگر حذف می‌شود.</summary>
    [SkippableFact]
    public async Task Seller_scoped_list_includes_own_products_and_excludes_foreign()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var builder = new DbContextOptionsBuilder<ReviewsDbContext>();
        ToobaNpgsql.ConfigureModuleContext(builder, _container!.GetConnectionString(), ReviewsDbContext.Schema, typeof(ReviewsDbContext));
        await using var db = new ReviewsDbContext(builder.Options);
        await db.Database.MigrateAsync();

        var ownProduct = Guid.NewGuid();
        var foreignProduct = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var ownPublished = ProductReview.Create(ownProduct, Guid.NewGuid(), "مالک", 5, null, "نظر محصول خودم", false, null, now);
        ownPublished.Publish(Guid.NewGuid(), now);
        var ownPending = ProductReview.Create(ownProduct, Guid.NewGuid(), "مالک۲", 3, null, "نظر در انتظار خودم", false, null, now);
        var foreign = ProductReview.Create(foreignProduct, Guid.NewGuid(), "خارجی", 4, null, "نظر محصول دیگری", false, null, now);
        foreign.Publish(Guid.NewGuid(), now);
        db.Reviews.AddRange(ownPublished, ownPending, foreign);
        await db.SaveChangesAsync();

        var directory = new ReviewDirectory(db, null!, null!);
        var ownPage = await directory.ListForProductsAsync([ownProduct], null, 1, 20, CancellationToken.None);
        Assert.Equal(2, ownPage.TotalCount);
        Assert.Equal(1, ownPage.PublishedCount);
        Assert.Equal(1, ownPage.PendingCount);
        Assert.Equal(0, ownPage.RejectedCount);
        Assert.All(ownPage.Items, item => Assert.Equal(ownProduct, item.ProductId));
        Assert.DoesNotContain(ownPage.Items, item => item.ProductId == foreignProduct);

        var foreignPage = await directory.ListForProductsAsync([foreignProduct], null, 1, 20, CancellationToken.None);
        Assert.Equal(1, foreignPage.TotalCount);
        Assert.Contains(foreignPage.Items, item => item.ProductId == foreignProduct);
        Assert.DoesNotContain(foreignPage.Items, item => item.ProductId == ownProduct);

        var empty = await directory.ListForProductsAsync([], null, 1, 20, CancellationToken.None);
        Assert.Equal(0, empty.TotalCount);
        Assert.Empty(empty.Items);
    }
}

