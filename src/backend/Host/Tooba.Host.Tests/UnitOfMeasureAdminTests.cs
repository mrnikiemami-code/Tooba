using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-TMAR-HOST-W5 — characterization for UnitOfMeasure write via CQRS Directory.</summary>
public sealed class UnitOfMeasureAdminTests
{
    private static readonly Guid LangA = Guid.Parse("01900000-0000-7000-8000-00000000bb01");
    private static readonly Guid LangUnknown = Guid.Parse("01900000-0000-7000-8000-00000000bb99");

    [Fact]
    public async Task Create_persists_unit_and_translation()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog, known: [LangA]);
        var id = await sender.Send(new CreateUnitOfMeasureCommand(Model("box", "Count", true, 1, LangA, "جعبه", "جعبه")), CancellationToken.None);
        var unit = await catalog.UnitsOfMeasure.SingleAsync();
        Assert.Equal(id, unit.UnitOfMeasureId);
        Assert.Equal("box", unit.Code);
        Assert.Equal(UnitOfMeasureDimension.Count, unit.Dimension);
        Assert.Single(catalog.UnitOfMeasureTranslations);
    }

    [Fact]
    public async Task Duplicate_code_is_rejected()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog, known: [LangA]);
        await sender.Send(new CreateUnitOfMeasureCommand(Model("box", "Count", true, 1, LangA, "جعبه", "جعبه")), CancellationToken.None);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.Send(new CreateUnitOfMeasureCommand(Model("BOX", "Count", true, 2, LangA, "ب", "ب")), CancellationToken.None));
        Assert.Equal("unit.code.duplicate", ex.Message);
    }

    [Fact]
    public async Task Unknown_language_is_rejected()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog, known: [LangA]);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.Send(new CreateUnitOfMeasureCommand(Model("m", "Length", true, 1, LangUnknown, "متر", "م")), CancellationToken.None));
        Assert.Equal("unit.language.unknown", ex.Message);
        Assert.Empty(catalog.UnitsOfMeasure);
    }

    [Fact]
    public async Task Invalid_dimension_is_rejected()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog, known: [LangA]);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.Send(new CreateUnitOfMeasureCommand(Model("x", "Temperature", true, 1, LangA, "س", "س")), CancellationToken.None));
        Assert.Equal("unit.dimension.invalid", ex.Message);
    }

    [Fact]
    public async Task Update_and_deactivate()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog, known: [LangA]);
        var id = await sender.Send(new CreateUnitOfMeasureCommand(Model("ltr", "Volume", true, 3, LangA, "لیتر", "ل")), CancellationToken.None);
        await sender.Send(new UpdateUnitOfMeasureCommand(id, Model("litre", "Volume", true, 5, LangA, "لیتر", "لیتر")), CancellationToken.None);
        var unit = await catalog.UnitsOfMeasure.SingleAsync();
        Assert.Equal("litre", unit.Code);
        Assert.Equal(5, unit.SortOrder);
        var deactivated = await sender.Send(new DeactivateUnitOfMeasureCommand(id), CancellationToken.None);
        Assert.False(deactivated.IsActive);
        Assert.False((await catalog.UnitsOfMeasure.SingleAsync()).IsActive);
    }

    [Fact]
    public void Admin_write_endpoints_use_cqrs_without_savechanges()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Admin/UnitOfMeasureEndpoints.cs"));
        Assert.Contains("CreateUnitOfMeasureCommand", source, StringComparison.Ordinal);
        Assert.Contains("UpdateUnitOfMeasureCommand", source, StringComparison.Ordinal);
        Assert.Contains("DeactivateUnitOfMeasureCommand", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChangesAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Add(", source, StringComparison.Ordinal);
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
        var gate = new FixedLanguageGate(known);
        var directory = new UnitOfMeasureDirectory(catalog, new SystemUtcClock(), new UuidV7IdGenerator(), gate);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IUnitOfMeasureDirectory>(directory);
        services.AddSingleton<IUnitOfMeasureLanguageGate>(gate);
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

    private sealed class FixedLanguageGate : IUnitOfMeasureLanguageGate
    {
        private readonly HashSet<Guid> _known;

        public FixedLanguageGate(IReadOnlyList<Guid> known) => _known = known.ToHashSet();

        public Task EnsureKnownAsync(IReadOnlyList<Guid> languageIds, CancellationToken cancellationToken)
        {
            if (languageIds.Any(id => !_known.Contains(id)))
            {
                throw new InvalidOperationException("unit.language.unknown");
            }

            return Task.CompletedTask;
        }
    }
}
