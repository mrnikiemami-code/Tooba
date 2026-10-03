using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application.Units.Commands;
using Tooba.Catalog.Application.Units.Models;
using Tooba.Catalog.Application.Units.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Localization.Contracts.Errors;
using Tooba.Localization.Contracts.Ports;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-TMAR-HOST-ADMIN-AMC-001-W3 — UnitOfMeasure CQRS Result characterization.</summary>
public sealed class UnitOfMeasureAdminTests
{
    private static readonly Guid LangA = Guid.Parse("01900000-0000-7000-8000-00000000bb01");
    private static readonly Guid LangUnknown = Guid.Parse("01900000-0000-7000-8000-00000000bb99");

    [Fact]
    public async Task Create_persists_unit_and_translation()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog, known: [LangA]);
        var result = await sender.Send(
            new CreateUnitOfMeasureCommand(Model("box", "Count", true, 1, LangA, "جعبه", "جعبه")),
            CancellationToken.None);
        Assert.True(result.IsSuccess);
        var unit = await catalog.UnitsOfMeasure.SingleAsync();
        Assert.Equal(result.Value.UnitOfMeasureId, unit.UnitOfMeasureId);
        Assert.Equal("box", unit.Code);
        Assert.Equal(UnitOfMeasureDimension.Count, unit.Dimension);
        Assert.Single(catalog.UnitOfMeasureTranslations);
    }

    [Fact]
    public async Task Duplicate_code_is_rejected()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog, known: [LangA]);
        Assert.True((await sender.Send(
            new CreateUnitOfMeasureCommand(Model("box", "Count", true, 1, LangA, "جعبه", "جعبه")),
            CancellationToken.None)).IsSuccess);
        var result = await sender.Send(
            new CreateUnitOfMeasureCommand(Model("BOX", "Count", true, 2, LangA, "ب", "ب")),
            CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrorCodes.UnitCodeDuplicate, result.FirstError.Code);
    }

    [Fact]
    public async Task Unknown_language_is_rejected()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog, known: [LangA]);
        var result = await sender.Send(
            new CreateUnitOfMeasureCommand(Model("m", "Length", true, 1, LangUnknown, "متر", "م")),
            CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrorCodes.UnitLanguageUnknown, result.FirstError.Code);
        Assert.Empty(catalog.UnitsOfMeasure);
    }

    [Fact]
    public async Task Invalid_dimension_is_rejected()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog, known: [LangA]);
        var result = await sender.Send(
            new CreateUnitOfMeasureCommand(Model("x", "Temperature", true, 1, LangA, "س", "س")),
            CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrorCodes.UnitDimensionInvalid, result.FirstError.Code);
    }

    [Fact]
    public async Task Update_and_deactivate()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog, known: [LangA]);
        var created = await sender.Send(
            new CreateUnitOfMeasureCommand(Model("ltr", "Volume", true, 3, LangA, "لیتر", "ل")),
            CancellationToken.None);
        Assert.True(created.IsSuccess);
        var id = created.Value.UnitOfMeasureId;
        Assert.True((await sender.Send(
            new UpdateUnitOfMeasureCommand(id, Model("litre", "Volume", true, 5, LangA, "لیتر", "لیتر")),
            CancellationToken.None)).IsSuccess);
        var unit = await catalog.UnitsOfMeasure.SingleAsync();
        Assert.Equal("litre", unit.Code);
        Assert.Equal(5, unit.SortOrder);
        var deactivated = await sender.Send(new DeactivateUnitOfMeasureCommand(id), CancellationToken.None);
        Assert.True(deactivated.IsSuccess);
        Assert.False(deactivated.Value.IsActive);
        Assert.False((await catalog.UnitsOfMeasure.SingleAsync()).IsActive);
    }

    [Fact]
    public void Catalog_endpoints_use_cqrs_without_dbcontext_or_savechanges()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Units/UnitOfMeasureEndpoints.cs"));
        Assert.Contains("CreateUnitOfMeasureCommand", source, StringComparison.Ordinal);
        Assert.Contains("UpdateUnitOfMeasureCommand", source, StringComparison.Ordinal);
        Assert.Contains("DeactivateUnitOfMeasureCommand", source, StringComparison.Ordinal);
        Assert.Contains("ListUnitOfMeasuresQuery", source, StringComparison.Ordinal);
        Assert.Contains("GetUnitOfMeasureQuery", source, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", source, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChangesAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ILanguageDirectory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Localization.Application", source, StringComparison.Ordinal);
    }

    private static UnitOfMeasureWriteModel Model(
        string code,
        string dimension,
        bool active,
        int sort,
        Guid lang,
        string name,
        string shortName)
        => new(code, dimension, active, sort, [new UnitOfMeasureTranslationWriteModel(lang, name, shortName)]);

    private static CatalogDbContext CreateCatalog()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new CatalogDbContext(options);
    }

    private static ISender CreateSender(CatalogDbContext catalog, IReadOnlyList<Guid> known)
    {
        var lookup = new FixedLanguageLookup(known);
        var directory = new UnitOfMeasureDirectory(catalog, new SystemUtcClock(), new UuidV7IdGenerator(), lookup);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IUnitOfMeasureDirectory>(directory);
        services.AddSingleton<ILanguageLookup>(lookup);
        services.AddValidatorsFromAssembly(typeof(CreateUnitOfMeasureCommand).Assembly);
        services.AddToobaCqrsFoundation(typeof(CreateUnitOfMeasureCommand).Assembly);
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs/architecture/TOOBA-LOCKS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private sealed class FixedLanguageLookup : ILanguageLookup
    {
        private readonly IReadOnlyList<LanguageLookupSnapshot> _langs;

        public FixedLanguageLookup(IReadOnlyList<Guid> known) =>
            _langs = known
                .Select((id, i) => new LanguageLookupSnapshot(id, $"l{i}", $"c{i}", $"u{i}", i == 0))
                .ToList();

        public Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_langs);
    }
}
