# TB-P04-T008 repair — reservation root cause

## Root cause

1. **Replace-hold then failed restore.** `ChangeLineCore` released the existing Held row, then reserved the new quantity. Over-availability failed the second reserve. Restore could fail or be swallowed. The cart row still pointed at a **Released** reservation. The next decrease/remove called `Release` again → `آزادسازی رزرو با موجودی هم‌خوان نبود` / `فقط رزرو Held قابل آزادسازی یا مصرف است.`

2. **Expiry order.** `ExpireDueCarts` used to run `ReleaseExpiredHolds` first, then `ReleaseAll` on the same ids (second release).

3. **No server worker.** `ExpireDueCartsAsync` existed but was only invoked from tests. Abandoned guest carts kept Held stock until someone returned.

## Affected paths

- `CartDirectory.ChangeLineCore` / `RemoveLine` / `ExpireDueCarts`
- `InventoryDirectory.Release` / `ReleaseExpiredHolds`
- Storefront cart error `detail` (raw exception text)

## Invariants restored

- Second `Release` of an already Released reservation is a no-op.
- Cart expiry releases line holds first, then orphan inventory holds.
- `CartExpiryHostedService` runs expiry per tenant without the browser.
- Customer `detail` uses `cart.inventory.*` codes, not Held vocabulary.

## Why tests missed it

Happy-path add/increase/decrease/remove never failed the re-reserve step, so the stale Released id was not reused. Expiry was called in-process, not as a host worker.

## Chosen repair

Idempotent inventory release, expiry order + hosted worker, customer-safe error map, focused tests for over-max then decrease, expiry restock, and double release.
