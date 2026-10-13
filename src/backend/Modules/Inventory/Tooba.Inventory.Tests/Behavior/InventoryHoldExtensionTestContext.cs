using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Inventory.Application.Ports;
using Tooba.Inventory.Infrastructure.Directories;
using Tooba.Inventory.Infrastructure.Persistence;

namespace Tooba.Inventory.Tests.Behavior;

/// <summary>
/// Builds an <see cref="InventoryDirectory"/> for the hold-extension surface. Only the DbContext is
/// real; the guard is a pass-through seam and the Offer/Catalog lookups are unused by the extension
/// path, so they are deliberately not materialized.
/// </summary>
internal static class InventoryHoldExtensionTestContext
{
    public static InventoryDirectory CreateDirectory(InventoryDbContext db) =>
        new(
            db,
            new PassThroughInventoryUseCaseGuard(),
            offers: null!,
            catalog: null!,
            clock: new SystemUtcClock(),
            ids: new UuidV7IdGenerator(),
            tracer: new ModuleCallTracer());

    private sealed class PassThroughInventoryUseCaseGuard : IInventoryUseCaseGuard
    {
        public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
