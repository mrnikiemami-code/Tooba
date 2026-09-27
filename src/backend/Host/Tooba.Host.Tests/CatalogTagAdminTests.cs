using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application.Tags.Commands;
using Tooba.Catalog.Application.Tags.Models;
using Tooba.Catalog.Application.Tags.Ports;
using Tooba.Catalog.Application.Tags.Queries;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-TMAR-HOST-ADMIN-AMC-001-W4 — Catalog Tag CQRS Result characterization.</summary>
public sealed class CatalogTagAdminTests
{
    [Fact]
    public async Task Create_list_get_generated_code_and_duplicate_explicit_code()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog);

        var created = await sender.Send(
            new CreateTagCommand(new CreateTagWriteModel(
                null,
                null,
                "fa-IR",
                new Dictionary<string, string> { ["fa-IR"] = "پرفروش", ["en"] = "Bestseller" })),
            CancellationToken.None);
        Assert.True(created.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(created.Value.Code));
        Assert.Equal("پرفروش", created.Value.Name);

        var listed = await sender.Send(new ListTagsQuery("fa-IR", "فروش"), CancellationToken.None);
        Assert.True(listed.IsSuccess);
        Assert.Contains(listed.Value, t => t.TagId == created.Value.TagId);

        var got = await sender.Send(new GetTagQuery(created.Value.TagId, "en"), CancellationToken.None);
        Assert.True(got.IsSuccess);
        Assert.Equal("Bestseller", got.Value.Name);

        var missing = await sender.Send(new GetTagQuery(Guid.NewGuid(), null), CancellationToken.None);
        Assert.True(missing.IsFailure);
        Assert.Equal(CatalogErrorCodes.TagMissing, missing.FirstError.Code);

        var dup = await sender.Send(
            new CreateTagCommand(new CreateTagWriteModel(
                created.Value.Code,
                null,
                "fa-IR",
                new Dictionary<string, string> { ["fa-IR"] = "دیگر" })),
            CancellationToken.None);
        Assert.True(dup.IsFailure);
        Assert.Equal(CatalogErrorCodes.TagCodeDuplicate, dup.FirstError.Code);

        var invalid = await sender.Send(
            new CreateTagCommand(new CreateTagWriteModel(null, null, "fa-IR", new Dictionary<string, string>())),
            CancellationToken.None);
        Assert.True(invalid.IsFailure);
        Assert.Equal(CatalogErrorCodes.TagInvalid, invalid.FirstError.Code);
    }

    [Fact]
    public async Task Product_and_category_assign_duplicate_remove_list()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog);
        var dir = new CatalogDirectory(catalog, new OpenCatalogUseCaseGuard());

        var tag = await sender.Send(
            new CreateTagCommand(new CreateTagWriteModel(
                "bestseller",
                null,
                "fa-IR",
                new Dictionary<string, string> { ["fa-IR"] = "پرفروش" })),
            CancellationToken.None);
        Assert.True(tag.IsSuccess);

        var l1 = await dir.CreateCategoryAsync(
            null, new Dictionary<string, string> { ["fa-IR"] = "کالای دیجیتال" }, CancellationToken.None);
        var l2 = await dir.CreateCategoryAsync(
            l1.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "موبایل" }, CancellationToken.None);
        var l3 = await dir.CreateCategoryAsync(
            l2.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "گوشی" }, CancellationToken.None);
        var product = await dir.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            "phone-tag-w4",
            null,
            new Dictionary<string, string> { ["fa-IR"] = "گوشی تست برچسب" },
            CancellationToken.None);
        await dir.AssignCategoryAsync(product.ProductId, l3.CategoryId, CancellationToken.None);

        var assigned = await sender.Send(
            new AssignProductTagCommand(product.ProductId, tag.Value.TagId), CancellationToken.None);
        Assert.True(assigned.IsSuccess);
        Assert.Single(assigned.Value);

        var dupProduct = await sender.Send(
            new AssignProductTagCommand(product.ProductId, tag.Value.TagId), CancellationToken.None);
        Assert.True(dupProduct.IsFailure);
        Assert.Equal(CatalogErrorCodes.TagAssignDuplicate, dupProduct.FirstError.Code);

        var productList = await sender.Send(
            new ListProductTagsQuery(product.ProductId, "fa-IR"), CancellationToken.None);
        Assert.True(productList.IsSuccess);
        Assert.Single(productList.Value);

        var removed = await sender.Send(
            new RemoveProductTagCommand(product.ProductId, tag.Value.TagId), CancellationToken.None);
        Assert.True(removed.IsSuccess);
        Assert.Empty(removed.Value);

        var idempotent = await sender.Send(
            new RemoveProductTagCommand(product.ProductId, tag.Value.TagId), CancellationToken.None);
        Assert.True(idempotent.IsSuccess);

        var catAssigned = await sender.Send(
            new AssignCategoryTagCommand(l3.CategoryId, tag.Value.TagId), CancellationToken.None);
        Assert.True(catAssigned.IsSuccess);
        Assert.Single(catAssigned.Value);

        var dupCategory = await sender.Send(
            new AssignCategoryTagCommand(l3.CategoryId, tag.Value.TagId), CancellationToken.None);
        Assert.True(dupCategory.IsFailure);
        Assert.Equal(CatalogErrorCodes.TagAssignDuplicate, dupCategory.FirstError.Code);

        var catRemoved = await sender.Send(
            new RemoveCategoryTagCommand(l3.CategoryId, tag.Value.TagId), CancellationToken.None);
        Assert.True(catRemoved.IsSuccess);
        Assert.Empty(catRemoved.Value);
    }

    [Fact]
    public void Catalog_tag_endpoints_use_cqrs_without_directory_or_dbcontext()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Tags/CatalogTagEndpoints.cs"));
        Assert.Contains("CreateTagCommand", source, StringComparison.Ordinal);
        Assert.Contains("ListTagsQuery", source, StringComparison.Ordinal);
        Assert.Contains("GetTagQuery", source, StringComparison.Ordinal);
        Assert.Contains("AssignProductTagCommand", source, StringComparison.Ordinal);
        Assert.Contains("AssignCategoryTagCommand", source, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", source, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChangesAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", source, StringComparison.Ordinal);
    }

    private static CatalogDbContext CreateCatalog()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new CatalogDbContext(options);
    }

    private static ISender CreateSender(CatalogDbContext catalog)
    {
        var directory = new TagDirectory(catalog, new OpenCatalogUseCaseGuard());
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITagDirectory>(directory);
        services.AddValidatorsFromAssembly(typeof(CreateTagCommand).Assembly);
        services.AddToobaCqrsFoundation(typeof(CreateTagCommand).Assembly);
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
