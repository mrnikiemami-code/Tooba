using Tooba.Cart.Domain.Aggregates;
using Tooba.Cart.Domain.Entities;
using Tooba.Cart.Infrastructure.Directories;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Offer.Contracts.Dtos;
using Xunit;

namespace Tooba.Cart.Tests.Behavior;

/// <summary>
/// TB-CART-LINE-ORDER — ترتیب خطوط سبد بخشی از قرارداد است، نه ترتیب فیزیکی ردیف‌ها.
/// بدون ORDER BY، PostgreSQL ردیف‌ها را به ترتیب heap برمی‌گرداند و هر UPDATE تعداد
/// نسخهٔ تازه را به انتهای صفحه می‌برد؛ نتیجه این بود که همان خطِ تغییرکرده در UI
/// به آخر لیست می‌پرید و ردیف‌های دیگر جابجا به‌نظر می‌رسیدند.
/// </summary>
public sealed class CartLineOrderTests
{
    [Fact]
    public async Task Lines_are_projected_in_stable_line_id_order_not_physical_order()
    {
        var cartId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var cart = ShoppingCart.CreateGuest(
            cartId,
            "guest-credential-hash",
            "IR",
            "IRR",
            SalesChannel.Marketplace,
            now,
            now.AddHours(2));

        // شناسه‌ها عمداً نامرتب اضافه می‌شوند: ترتیب درون‌حافظه‌ای (۳،۱،۲) با ترتیب قرارداد یکی نیست.
        var inserted = new[]
        {
            Guid.Parse("01a09aff-0000-7000-8000-000000000003"),
            Guid.Parse("01a09aff-0000-7000-8000-000000000001"),
            Guid.Parse("01a09aff-0000-7000-8000-000000000002"),
        };
        foreach (var lineId in inserted)
        {
            cart.AddLine(
                CartLine.Open(
                    lineId,
                    cartId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    1m,
                    null,
                    1_000m,
                    "IRR",
                    taxExclusive: true,
                    Guid.NewGuid(),
                    now),
                now);
        }

        var projector = new CartSnapshotProjector(new StubAvailability());
        var snapshot = await projector.ToSnapshotAsync(cart, CancellationToken.None);

        Assert.Equal(
            inserted.OrderBy(x => x).ToArray(),
            snapshot.Lines.Select(x => x.LineId).ToArray());
    }

    [Fact]
    public async Task A_quantity_change_does_not_move_the_changed_line_even_after_a_physical_reorder()
    {
        var cartId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var cart = ShoppingCart.CreateGuest(
            cartId,
            "guest-credential-hash",
            "IR",
            "IRR",
            SalesChannel.Marketplace,
            now,
            now.AddHours(2));

        var first = Guid.Parse("01a09aff-0000-7000-8000-000000000001");
        var second = Guid.Parse("01a09aff-0000-7000-8000-000000000002");
        var third = Guid.Parse("01a09aff-0000-7000-8000-000000000003");
        var offerByLine = new Dictionary<Guid, Guid>
        {
            [first] = Guid.NewGuid(),
            [second] = Guid.NewGuid(),
            [third] = Guid.NewGuid(),
        };
        foreach (var lineId in new[] { first, second, third })
        {
            cart.AddLine(Line(lineId, offerByLine[lineId], cartId, now), now);
        }

        var projector = new CartSnapshotProjector(new StubAvailability());
        var before = await projector.ToSnapshotAsync(cart, CancellationToken.None);
        Assert.Equal(new[] { first, second, third }, before.Lines.Select(x => x.LineId).ToArray());

        // سرور تعدادِ خط دوم را عوض می‌کند؛ UPDATE در PostgreSQL ردیف را در heap جابجا می‌کند و
        // دیگر با ترتیب قبلی برگردانده نمی‌شود. این جهش فیزیکی همان چیزی است که کاربر دید:
        // ردیفِ تغییرکرده به انتهای سبد پرید و بقیه جابجا شدند.
        var movedLine = Line(second, offerByLine[second], cartId, now);
        cart.RemoveLine(second, now);
        cart.AddLine(movedLine, now);

        var after = await projector.ToSnapshotAsync(cart, CancellationToken.None);

        Assert.Equal(
            before.Lines.Select(x => x.LineId).ToArray(),
            after.Lines.Select(x => x.LineId).ToArray());
        Assert.Equal(1, Array.IndexOf(after.Lines.Select(x => x.LineId).ToArray(), second));
        Assert.Equal(4m, after.Lines.Single(x => x.LineId == second).Quantity);
    }

    private static CartLine Line(Guid lineId, Guid offerId, Guid cartId, DateTimeOffset now) =>
        CartLine.Open(
            lineId,
            cartId,
            offerId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            4m,
            null,
            1_000m,
            "IRR",
            taxExclusive: true,
            Guid.NewGuid(),
            now);

    private sealed class StubAvailability : IInventoryAvailabilityGateway
    {
        public Task<InventoryAvailability?> GetAvailabilityAsync(Guid offerId, CancellationToken cancellationToken) =>
            Task.FromResult<InventoryAvailability?>(null);

        public Task<IReadOnlyDictionary<Guid, InventoryAvailability>> GetAvailabilityBatchAsync(
            IReadOnlyCollection<Guid> offerIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, InventoryAvailability>>(
                new Dictionary<Guid, InventoryAvailability>());
    }
}
