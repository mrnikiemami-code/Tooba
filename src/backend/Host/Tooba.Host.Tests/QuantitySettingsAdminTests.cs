using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.Quantity.Commands;
using Tooba.Catalog.Application.Settings.Quantity.Ports;
using Tooba.Catalog.Application.Settings.Quantity.Queries;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>Quantity-rounding settings via Catalog CQRS (Admin AMC W2).</summary>
public sealed class QuantitySettingsAdminTests
{
    [Fact]
    public async Task Default_missing_row_reads_as_nearest()
    {
        await using var catalog = CreateCatalog();
        var mode = await new StoreQuantitySettingsDirectory(catalog, new SystemUtcClock())
            .GetAsync(CancellationToken.None);
        Assert.Equal(QuantityRoundingMode.Nearest, mode);
    }

    [Fact]
    public async Task Valid_floor_persists()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog);
        var result = await sender.Send(new SaveStoreQuantitySettingsCommand("Floor"), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal("Floor", result.Value.GlobalRoundingMode);
        var row = await catalog.StoreQuantitySettings.SingleAsync();
        Assert.Equal(QuantityRoundingMode.Floor, row.RoundingMode);
    }

    [Fact]
    public async Task Valid_ceiling_case_insensitive()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog);
        var result = await sender.Send(new SaveStoreQuantitySettingsCommand("ceiling"), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal("Ceiling", result.Value.GlobalRoundingMode);
    }

    [Fact]
    public async Task Invalid_mode_is_rejected_without_write()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog);
        var result = await sender.Send(new SaveStoreQuantitySettingsCommand("RoundHalfEven"), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == CatalogErrorCodes.QuantityRoundingInvalid);
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
    public async Task Get_query_returns_view()
    {
        await using var catalog = CreateCatalog();
        var sender = CreateSender(catalog);
        await sender.Send(new SaveStoreQuantitySettingsCommand("Floor"), CancellationToken.None);
        var result = await sender.Send(new GetStoreQuantitySettingsQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal("Floor", result.Value.GlobalRoundingMode);
        Assert.Equal("رو به پایین", result.Value.LabelFa);
    }

    [Fact]
    public void Module_endpoints_own_quantity_routes_not_host()
    {
        var root = FindRepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/QuantitySettingsEndpoints.cs")));
        var source = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Settings/QuantitySettingsEndpoints.cs"));
        Assert.Contains("/v1/admin/settings/quantity-rounding", source, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", source, StringComparison.Ordinal);
        Assert.Contains("SaveStoreQuantitySettingsCommand", source, StringComparison.Ordinal);
        Assert.Contains("GetStoreQuantitySettingsQuery", source, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogLookupGateway", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminPanelAccess", source, StringComparison.Ordinal);
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
