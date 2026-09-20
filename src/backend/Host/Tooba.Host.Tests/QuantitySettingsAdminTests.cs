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

/// <summary>TB-TMAR-HOST-W4 — characterization for quantity-rounding settings write via CQRS Directory.</summary>
public sealed class QuantitySettingsAdminTests
{
    [Fact]
    public async Task Default_missing_row_reads_as_nearest()
    {
        await using var catalog = CreateCatalog();
        var mode = await CreateLookup(catalog).GetGlobalRoundingModeAsync(CancellationToken.None);
        Assert.Equal(QuantityRoundingMode.Nearest, mode);
    }

    [Fact]
    public async Task Valid_floor_persists()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog);
        var mode = await sender.Send(new SaveStoreQuantitySettingsCommand("Floor"), CancellationToken.None);
        Assert.Equal(QuantityRoundingMode.Floor, mode);
        var row = await catalog.StoreQuantitySettings.SingleAsync();
        Assert.Equal(QuantityRoundingMode.Floor, row.RoundingMode);
    }

    [Fact]
    public async Task Valid_ceiling_case_insensitive()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog);
        var mode = await sender.Send(new SaveStoreQuantitySettingsCommand("ceiling"), CancellationToken.None);
        Assert.Equal(QuantityRoundingMode.Ceiling, mode);
    }

    [Fact]
    public async Task Invalid_mode_is_rejected_without_write()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog);
        var error = await Assert.ThrowsAsync<PlatformHttpException>(
            () => sender.Send(new SaveStoreQuantitySettingsCommand("RoundHalfEven"), CancellationToken.None));
        Assert.Equal(400, error.StatusCode);
        Assert.Equal("quantity.rounding.invalid", error.ErrorCode);
        Assert.Empty(catalog.StoreQuantitySettings);
    }

    [Fact]
    public async Task Second_save_updates_same_singleton()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog);
        await sender.Send(new SaveStoreQuantitySettingsCommand("Floor"), CancellationToken.None);
        await sender.Send(new SaveStoreQuantitySettingsCommand("Nearest"), CancellationToken.None);
        Assert.Equal(1, await catalog.StoreQuantitySettings.CountAsync());
        Assert.Equal(QuantityRoundingMode.Nearest, (await catalog.StoreQuantitySettings.SingleAsync()).RoundingMode);
    }

    [Fact]
    public void Admin_endpoints_reuse_authorization_and_cqrs()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Admin/QuantitySettingsEndpoints.cs"));
        Assert.Contains("/v1/admin/settings/quantity-rounding", source, StringComparison.Ordinal);
        Assert.Contains("AdminPanelAccess.RequireAuthorizedAsync", source, StringComparison.Ordinal);
        Assert.Contains("SaveStoreQuantitySettingsCommand", source, StringComparison.Ordinal);
        Assert.Contains("ICatalogLookupGateway", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChangesAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", source, StringComparison.Ordinal);
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
        var directory = new StoreQuantitySettingsDirectory(catalog, new SystemUtcClock());
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IStoreQuantitySettingsDirectory>(directory);
        services.AddValidatorsFromAssembly(typeof(SaveStoreQuantitySettingsCommand).Assembly);
        services.AddToobaCqrsFoundation(typeof(SaveStoreQuantitySettingsCommand).Assembly);
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private static ICatalogLookupGateway CreateLookup(CatalogDbContext catalog)
        => new CatalogDirectory(catalog, new OpenCatalogUseCaseGuard());

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
}
