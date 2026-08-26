using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BulkInquiry.Domain;
using Tooba.BulkInquiry.Infrastructure.Persistence;
using Tooba.Host.ProductQnA;
using Tooba.Persistence;
using Tooba.ProductQnA.Application;
using Tooba.ProductQnA.Domain;
using Tooba.ProductQnA.Infrastructure;
using Tooba.ProductQnA.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>پوشش قرارداد، حریم خصوصی و فیلتر Published برای ProductQnA و BulkInquiry.</summary>
public sealed class ProductQnAAndBulkInquiryTests
{
    /// <summary>DTO عمومی شناسهٔ Actor ندارد.</summary>
    [Fact]
    public void Public_qa_contract_does_not_leak_internal_identity()
    {
        var names = typeof(PublishedQaItem).GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain("AuthorUserId", names);
        Assert.DoesNotContain("ModerationReason", names);
        Assert.Equal("product_qna", ProductQnADbContext.Schema);
        Assert.Equal("bulk_inquiry", BulkInquiryDbContext.Schema);
    }

    /// <summary>ثبت پرسش فقط ProductId و Body می‌پذیرد.</summary>
    [Fact]
    public void Submission_contract_has_no_actor_authority()
    {
        var names = typeof(SubmitProductQuestion).GetProperties().Select(x => x.Name).ToArray();
        Assert.Equal(["ProductId", "Body"], names);
        Assert.DoesNotContain("ActorUserId", names);
        Assert.DoesNotContain("AuthorDisplayName", names);
    }

    /// <summary>Entity درخواست عمده قیمت یا تخفیف ندارد.</summary>
    [Fact]
    public void Bulk_inquiry_entity_has_no_price_fields()
    {
        var names = typeof(BulkPurchaseInquiry).GetProperties().Select(x => x.Name).ToArray();
        Assert.Equal(
            [
                "InquiryId", "ProductId", "FullName", "Phone", "Email", "CompanyName",
                "Address", "Quantity", "Notes", "Status", "CreatedAt",
            ],
            names);
        Assert.DoesNotContain("Price", names);
        Assert.DoesNotContain("Discount", names);
        Assert.DoesNotContain("UnitPrice", names);
    }

    /// <summary>مقدار بیرون دامنه ۱۰..۱۰۰۰ رد می‌شود.</summary>
    [Theory]
    [InlineData(9)]
    [InlineData(1001)]
    public void Invalid_quantity_is_rejected(int quantity) =>
        Assert.Throws<InvalidOperationException>(() => BulkPurchaseInquiry.Create(
            Guid.NewGuid(), "علی رضایی", "09121234567", null, null,
            "تهران، خیابان نمونه شماره ۱۲", quantity, null, DateTimeOffset.UtcNow));

    /// <summary>مرز HTTP ثبت پرسش در production بدون نشست 401 می‌دهد.</summary>
    [Fact]
    public void Submit_question_endpoint_requires_actor()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "ProductQnA", "ProductQnAEndpoints.cs"));
        Assert.Contains("session.IsAuthenticated", source, StringComparison.Ordinal);
        Assert.Contains("customer.session.required", source, StringComparison.Ordinal);
        Assert.Contains("environment.IsDevelopment()", source, StringComparison.Ordinal);
        Assert.Contains("/v1/customer/product-questions", source, StringComparison.Ordinal);
        Assert.Equal("X-Tooba-Dev-Actor-User-Id", ProductQnAEndpoints.DevActorHeader);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "AGENTS.md"))) current = current.Parent;
        return current?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}

/// <summary>اثبات PostgreSQL برای فیلتر Published و ذخیرهٔ درخواست عمده.</summary>
[Collection("PostgresSerial")]
public sealed class ProductQnAAndBulkInquiryPostgresTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _available;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder().WithImage("postgres:16-alpine").WithDatabase("tooba_product_qna")
                .WithUsername("tooba").WithPassword("dev-placeholder").Build();
            await _container.StartAsync();
            _available = true;
        }
        catch (Exception) { _available = false; }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }

    /// <summary>Pending و Rejected در خواندن عمومی شرکت نمی‌کنند.</summary>
    [SkippableFact]
    public async Task Published_questions_only_are_returned()
    {
        Skip.If(!_available || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var qnaBuilder = new DbContextOptionsBuilder<ProductQnADbContext>();
        ToobaNpgsql.ConfigureModuleContext(qnaBuilder, _container!.GetConnectionString(), ProductQnADbContext.Schema, typeof(ProductQnADbContext));
        await using var qnaDb = new ProductQnADbContext(qnaBuilder.Options);
        await qnaDb.Database.MigrateAsync();

        var productId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var moderator = Guid.NewGuid();

        foreach (var (status, body) in new[]
        {
            (ProductQuestionStatus.Published, "سوال منتشرشده"),
            (ProductQuestionStatus.Pending, "سوال در انتظار"),
            (ProductQuestionStatus.Rejected, "سوال ردشده"),
        })
        {
            var question = ProductQuestion.Create(productId, Guid.NewGuid(), "مشتری", body, now);
            if (status == ProductQuestionStatus.Published) question.Publish(moderator, now);
            if (status == ProductQuestionStatus.Rejected) question.Reject(moderator, "رد", now);
            qnaDb.Questions.Add(question);
        }
        await qnaDb.SaveChangesAsync();

        var directory = new ProductQaDirectory(qnaDb, null!);
        var count = await directory.CountPublishedAsync(productId, CancellationToken.None);
        Assert.Equal(1, count);
    }

    /// <summary>درخواست عمده بدون فیلد قیمت در schema bulk_inquiry ذخیره می‌شود.</summary>
    [SkippableFact]
    public async Task Bulk_inquiry_persists_without_price_fields()
    {
        Skip.If(!_available || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var builder = new DbContextOptionsBuilder<BulkInquiryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(builder, _container!.GetConnectionString(), BulkInquiryDbContext.Schema, typeof(BulkInquiryDbContext));
        await using var db = new BulkInquiryDbContext(builder.Options);
        await db.Database.MigrateAsync();

        var inquiry = BulkPurchaseInquiry.Create(
            Guid.NewGuid(), "سارا محمدی", "09129876543", "sara@example.com", "شرکت نمونه",
            "تهران، خیابان ولیعصر، پلاک ۱۰", 50, "یادداشت تست", DateTimeOffset.UtcNow);
        db.Inquiries.Add(inquiry);
        await db.SaveChangesAsync();

        var stored = await db.Inquiries.AsNoTracking().SingleAsync(x => x.InquiryId == inquiry.InquiryId);
        Assert.Equal(50, stored.Quantity);
        Assert.Equal(BulkInquiryStatus.Submitted, stored.Status);
        Assert.DoesNotContain("Price", typeof(BulkPurchaseInquiry).GetProperties().Select(x => x.Name));
    }
}
