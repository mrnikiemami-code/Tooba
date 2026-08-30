using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// مهاجرت دستهٔ اصلی و عضویت نمایشی (TB-P07-T036 B–F / P / Q).
/// </summary>
[Collection("PostgresSerial")]
public sealed class PrimaryCategoryMigrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_primary_category_migration")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Preview_does_not_mutate_and_migration_preserves_compatible_removes_orphans()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-prim-mig-1", "tenant-prim-mig-1"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var (phoneL3, bookL3, noteId, screenId, authorId, productId) =
            await SeedPhoneAndBookCategoriesAsync(dir);

        await dir.AssignCategoryAsync(productId, phoneL3, CancellationToken.None);
        await dir.SetProductAttributesAsync(
            productId,
            [
                new ProductAttributeValueInput(screenId, "6.1", null, false),
                new ProductAttributeValueInput(noteId, "سری A", null, false),
            ],
            CancellationToken.None);

        var valueCountBefore = await db.ProductAttributeValues.CountAsync(v => v.ProductId == productId);
        var primaryBefore = await db.ProductCategories.SingleAsync(
            x => x.ProductId == productId && x.Role == CatalogProductCategoryRole.Primary);

        var report = await dir.PreviewCategoryChangeReportAsync(
            productId, bookL3, "fa-IR", CancellationToken.None);

        Assert.Equal(phoneL3, report.CurrentCategoryId);
        Assert.Equal(bookL3, report.TargetCategoryId);
        Assert.False(string.IsNullOrWhiteSpace(report.CurrentCategoryPath));
        Assert.False(string.IsNullOrWhiteSpace(report.TargetCategoryPath));
        Assert.Equal(1, report.CompatiblePreservedCount);
        Assert.Contains(report.PreservedAttributes ?? [], l => l.Contains("یادداشت", StringComparison.Ordinal));
        Assert.True(report.OrphanCount >= 1);
        Assert.Equal(1, report.NewlyRequiredMissingCount);
        Assert.Contains(report.RequiredMissing ?? report.NewlyRequiredLabels, l => l.Contains("نویسنده", StringComparison.Ordinal));
        Assert.NotEmpty(report.ReadinessBlockers ?? []);

        Assert.Equal(valueCountBefore, await db.ProductAttributeValues.CountAsync(v => v.ProductId == productId));
        Assert.Equal(primaryBefore.CategoryId, (await db.ProductCategories.SingleAsync(
            x => x.ProductId == productId && x.Role == CatalogProductCategoryRole.Primary)).CategoryId);

        await dir.ReplaceProductPrimaryCategoryAsync(productId, bookL3, CancellationToken.None);

        Assert.True(await db.ProductAttributeValues.AnyAsync(
            v => v.ProductId == productId && v.DefinitionId == noteId));
        Assert.False(await db.ProductAttributeValues.AnyAsync(
            v => v.ProductId == productId && v.DefinitionId == screenId));
        Assert.False(await db.ProductAttributeValues.AnyAsync(
            v => v.ProductId == productId && v.DefinitionId == authorId));

        var readiness = await dir.GetProductAttributeReadinessAsync(productId, CancellationToken.None);
        Assert.False(readiness.IsComplete);

        var history = await dir.ListProductHistoryAsync(productId, ProductHistoryRules.SectionCategory, 0, 20, CancellationToken.None);
        var migration = Assert.Single(history.Items, x => x.SummaryFa == ProductHistoryRules.SummaryCategoryMigrationFa);
        Assert.Contains(">", migration.BeforeSummary ?? "");
        Assert.DoesNotContain("00000000", migration.AfterSummary ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Contains("حفظ ویژگی", migration.AfterSummary ?? "", StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task Additional_membership_promotes_to_primary_and_others_remain()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-prim-mig-2", "tenant-prim-mig-2"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var (phoneL3, bookL3, _, _, _, productId) = await SeedPhoneAndBookCategoriesAsync(dir);
        var displayL3 = await CreateSiblingL3Async(dir, "پوشاک", "کفش", "کفش ورزشی");

        await dir.AssignCategoryAsync(productId, phoneL3, CancellationToken.None);
        await dir.AddProductAdditionalCategoryAsync(productId, bookL3, CancellationToken.None);
        await dir.AddProductAdditionalCategoryAsync(productId, displayL3, CancellationToken.None);

        var preview = await dir.PreviewCategoryChangeReportAsync(productId, bookL3, "fa-IR", CancellationToken.None);
        Assert.True(preview.AdditionalMembershipPromoted);
        Assert.Equal(1, preview.OtherDisplayMembershipsRemainCount);

        await dir.ReplaceProductPrimaryCategoryAsync(productId, bookL3, CancellationToken.None);

        var links = await db.ProductCategories.Where(x => x.ProductId == productId).ToListAsync();
        Assert.Single(links, x => x.CategoryId == bookL3 && x.Role == CatalogProductCategoryRole.Primary);
        Assert.DoesNotContain(links, x => x.CategoryId == bookL3 && x.Role == CatalogProductCategoryRole.Additional);
        Assert.Single(links, x => x.CategoryId == displayL3 && x.Role == CatalogProductCategoryRole.Additional);
        Assert.DoesNotContain(links, x => x.CategoryId == phoneL3);
    }

    [SkippableFact]
    public async Task AddProductAdditionalCategory_never_creates_primary()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-prim-mig-3", "tenant-prim-mig-3"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var (phoneL3, bookL3, _, _, _, productId) = await SeedPhoneAndBookCategoriesAsync(dir);
        await dir.AssignCategoryAsync(productId, phoneL3, CancellationToken.None);
        await dir.AddProductAdditionalCategoryAsync(productId, bookL3, CancellationToken.None);

        var links = await db.ProductCategories.Where(x => x.ProductId == productId).ToListAsync();
        Assert.Single(links, x => x.Role == CatalogProductCategoryRole.Primary);
        Assert.Equal(phoneL3, links.Single(x => x.Role == CatalogProductCategoryRole.Primary).CategoryId);
        Assert.Single(links, x => x.CategoryId == bookL3 && x.Role == CatalogProductCategoryRole.Additional);
    }

    [SkippableFact]
    public async Task Published_product_unpublishes_on_structural_incompatibility()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-prim-mig-4", "tenant-prim-mig-4"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var (phoneL3, bookL3, noteId, screenId, _, productId) =
            await SeedPhoneAndBookCategoriesAsync(dir);

        await dir.AssignCategoryAsync(productId, phoneL3, CancellationToken.None);
        await dir.SetProductAttributesAsync(
            productId,
            [
                new ProductAttributeValueInput(screenId, "6.1", null, false),
                new ProductAttributeValueInput(noteId, "سری A", null, false),
            ],
            CancellationToken.None);
        await dir.AttachMediaReferenceAsync(
            productId, Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), CancellationToken.None);
        await ProductPublishPrep.EnsureMinimalSeoForPublishAsync(
            dir, productId, "توضیح سئو مهاجرت", CancellationToken.None);
        await dir.PublishProductAsync(productId, CancellationToken.None);
        Assert.Equal(
            CatalogPublicationStatus.Published,
            (await db.Products.SingleAsync(x => x.ProductId == productId)).Status);

        await dir.ReplaceProductPrimaryCategoryAsync(productId, bookL3, CancellationToken.None);

        Assert.Equal(
            CatalogPublicationStatus.Draft,
            (await db.Products.SingleAsync(x => x.ProductId == productId)).Status);
        var lifecycle = await dir.ListProductHistoryAsync(
            productId, ProductHistoryRules.SectionLifecycle, 0, 20, CancellationToken.None);
        Assert.Contains(
            lifecycle.Items,
            x => x.SummaryFa == ProductHistoryRules.SummaryUnpublishedByMigrationFa);
    }

    private static async Task<(Guid PhoneL3, Guid BookL3, Guid NoteId, Guid ScreenId, Guid AuthorId, Guid ProductId)>
        SeedPhoneAndBookCategoriesAsync(CatalogDirectory dir)
    {
        var parent = await dir.CreateCategoryAsync(
            null, new Dictionary<string, string> { ["fa-IR"] = "الکترونیک" }, CancellationToken.None);
        var mid = await dir.CreateCategoryAsync(
            parent.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "موبایل و تبلت" }, CancellationToken.None);
        var phone = await dir.CreateCategoryAsync(
            mid.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "گوشی موبایل" }, CancellationToken.None);

        var bookRoot = await dir.CreateCategoryAsync(
            null, new Dictionary<string, string> { ["fa-IR"] = "کتاب" }, CancellationToken.None);
        var bookMid = await dir.CreateCategoryAsync(
            bookRoot.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "رمان" }, CancellationToken.None);
        var book = await dir.CreateCategoryAsync(
            bookMid.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "داستان" }, CancellationToken.None);

        var screenId = await dir.CreateAttributeDefinitionAsync(
            "screen_size", CatalogAttributeValueKind.Number, false,
            new Dictionary<string, string> { ["fa-IR"] = "اندازه صفحه" }, CancellationToken.None);
        var noteId = await dir.CreateAttributeDefinitionAsync(
            "note", CatalogAttributeValueKind.Text, false,
            new Dictionary<string, string> { ["fa-IR"] = "یادداشت" }, CancellationToken.None);
        var authorId = await dir.CreateAttributeDefinitionAsync(
            "author", CatalogAttributeValueKind.Text, false,
            new Dictionary<string, string> { ["fa-IR"] = "نویسنده" }, CancellationToken.None);

        await dir.UpdateAttributeDefinitionAsync(
            screenId, "inch", isRequired: true, isFilterable: true, isComparable: true, isMultivalue: false,
            displayOrder: 1, validationMin: 4m, validationMax: 10m, validationMaxLength: null, isActive: true,
            CancellationToken.None);
        await dir.UpdateAttributeDefinitionAsync(
            noteId, null, isRequired: false, isFilterable: false, isComparable: false, isMultivalue: false,
            displayOrder: 2, null, null, 200, true, CancellationToken.None);
        await dir.UpdateAttributeDefinitionAsync(
            authorId, null, isRequired: true, isFilterable: false, isComparable: false, isMultivalue: false,
            displayOrder: 1, null, null, 200, true, CancellationToken.None);

        await dir.BindCategoryAttributeAsync(
            phone.CategoryId, screenId, 10,
            new CategoryAttributeAssignmentFlags(true, true, false, true), CancellationToken.None);
        await dir.BindCategoryAttributeAsync(
            phone.CategoryId, noteId, 20,
            new CategoryAttributeAssignmentFlags(false, false, false, false), CancellationToken.None);
        await dir.BindCategoryAttributeAsync(
            book.CategoryId, authorId, 1,
            new CategoryAttributeAssignmentFlags(true, false, false, false), CancellationToken.None);
        await dir.BindCategoryAttributeAsync(
            book.CategoryId, noteId, 2,
            new CategoryAttributeAssignmentFlags(false, false, false, false), CancellationToken.None);

        var product = await dir.CreateProductAsync(
            CatalogProductKind.PhysicalGood, "mig-phone", null,
            new Dictionary<string, string> { ["fa-IR"] = "گوشی مهاجرت" }, CancellationToken.None);

        return (phone.CategoryId, book.CategoryId, noteId, screenId, authorId, product.ProductId);
    }

    private static async Task<Guid> CreateSiblingL3Async(
        CatalogDirectory dir,
        string l1,
        string l2,
        string l3)
    {
        var root = await dir.CreateCategoryAsync(
            null, new Dictionary<string, string> { ["fa-IR"] = l1 }, CancellationToken.None);
        var mid = await dir.CreateCategoryAsync(
            root.CategoryId, new Dictionary<string, string> { ["fa-IR"] = l2 }, CancellationToken.None);
        var leaf = await dir.CreateCategoryAsync(
            mid.CategoryId, new Dictionary<string, string> { ["fa-IR"] = l3 }, CancellationToken.None);
        return leaf.CategoryId;
    }

    private static CatalogDbContext CreateCatalogDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new CatalogOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<CatalogDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, CatalogDbContext.Schema, typeof(CatalogDbContext));
        options.AddInterceptors(interceptor);
        return new CatalogDbContext(options.Options);
    }
}
