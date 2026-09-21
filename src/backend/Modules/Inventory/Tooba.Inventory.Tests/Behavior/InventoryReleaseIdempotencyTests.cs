using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.ValueObjects;
using Xunit;

namespace Tooba.Inventory.Tests.Behavior;

public sealed class InventoryReleaseIdempotencyTests
{
    [Fact]
    public void Domain_move_to_released_rejects_already_terminal_status()
    {
        var now = DateTimeOffset.Parse("2026-06-01T00:00:00Z");
        var reservation = StockReservation.Hold(
            Guid.Parse("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaa1"),
            Guid.Parse("bbbbbbbb-bbbb-7bbb-8bbb-bbbbbbbbbbb1"),
            2m,
            "ext",
            "idem-1",
            now,
            now.AddMinutes(30));

        reservation.MoveTo(StockReservationStatus.Released, now.AddMinutes(1));
        Assert.Equal(StockReservationStatus.Released, reservation.Status);
        Assert.Throws<InvalidOperationException>(() =>
            reservation.MoveTo(StockReservationStatus.Released, now.AddMinutes(2)));
        Assert.Throws<InvalidOperationException>(() =>
            reservation.MoveTo(StockReservationStatus.Consumed, now.AddMinutes(2)));
    }

    [Fact]
    public void Directory_release_source_treats_released_and_consumed_as_successful_noop()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null
               && !File.Exists(Path.Combine(dir.FullName, "docs", "ai", "TOOBA-PIPELINE-PROTOCOL.md"))
               && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var path = Path.Combine(
            dir!.FullName,
            "src",
            "backend",
            "Modules",
            "Inventory",
            "Tooba.Inventory.Infrastructure",
            "Directories",
            "InventoryDirectory.cs");
        var text = File.ReadAllText(path);
        var start = text.IndexOf("public async Task ReleaseAsync(Guid reservationId", StringComparison.Ordinal);
        var end = text.IndexOf("public async Task ConsumeAsync", start, StringComparison.Ordinal);
        var body = text[start..end];
        Assert.Contains(
            "if (reservation.Status is StockReservationStatus.Released or StockReservationStatus.Consumed)",
            body,
            StringComparison.Ordinal);
        Assert.Contains("return;", body, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (", body, StringComparison.Ordinal);
    }
}
