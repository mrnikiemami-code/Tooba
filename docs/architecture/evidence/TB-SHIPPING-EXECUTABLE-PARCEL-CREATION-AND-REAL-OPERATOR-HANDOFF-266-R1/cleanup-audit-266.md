# Cleanup audit — what 266 actually did, and what stays UNVERIFIED

Task: `TB-SHIPPING-EXECUTABLE-PARCEL-CREATION-AND-REAL-OPERATOR-HANDOFF-266-R1` §5
Purpose: append an exact audit of the 266 cleanup. **No cleanup was repeated in this round.** No database was
restored. No backup file was moved, rewritten or deleted.

## 1. Backups (preserved, validated)

All four private custom-format backups still exist unchanged under `D:\temp\tb266-backup\` and were validated
read-only with `docker run --rm -i postgres:18.6 pg_restore --list` (no restore, no database created):

| File | Bytes | `pg_restore --list` TOC lines |
| --- | --- | --- |
| `tooba_alpha-pre-tb266.dump` | 3,791,764 | 1425 |
| `tooba_alpha-pre-tb266-seed.dump` | 2,897,993 | 1425 |
| `tooba_alpha-pre-tb266-repair.dump` | 2,902,992 | 1425 |
| `tooba_alpha-pre-tb266-final-cleanup.dump` | 2,908,545 | 1425 |

Header of the earliest dump (read from the listing): `dbname: tooba_alpha`, `Format: CUSTOM`, `Dump Version: 1.16-0`,
archive created 2026-11-14 12:15:02 UTC. See `backup-validation.txt`.

**Restoration command (documented, NOT executed):**

```
docker run --rm -i -e PGPASSWORD=<redacted> postgres:18.6 \
  pg_restore --clean --if-exists --no-owner -h host.docker.internal -p 5432 -U <user> -d tooba_alpha \
  < D:\temp\tb266-backup\tooba_alpha-pre-tb266-final-cleanup.dump
```

Credentials are intentionally redacted in this receipt.

## 2. The private cleanup script is present (not missing)

`D:\temp\tb266-backup\cleanup.sql` (3,413 B, 2026-11-14 15:48) is intact. It is a single
`BEGIN; … COMMIT;` block with `\set ON_ERROR_STOP on`, dependency-ordered children-first:

- fulfillment → returns → order → payment → shipping execution → settlement → cart → inventory.
- **Order-independent catalog is not touched at all.** No statement in the script writes
  `offer.*`, `party.*`, `identity.*`, `fulfillment.shipping_methods`, any `shipping.authoring_*` draft/selection,
  DataProtection keys or the captured-preview setting.
- Money rows removed were synthetic only (`provider_code = fake` / Development confirmation); `settlement_entries`
  was `0` before and after and no payout row existed.
- The read/audit helpers `audit.sql`, `post.sql`, `counts-after.sql`, `stock.sql`, `inv.sql`, `fk.sql`,
  `graph.sql`, `refs.sql`, `tables.sql`, `cols*.sql` and the final scoped
  `delete-cancelled-fixture.sql`/`inspect-cancelled.sql` are also present.

## 3. What actually happened to stock — stated exactly

Two separate statements touch inventory in `cleanup.sql` (lines 68–73):

```sql
DELETE FROM inventory.reservation_consumptions;
DELETE FROM inventory.held_obligation_releases;
DELETE FROM inventory.return_restock_inbox;
DELETE FROM inventory.reservations;
UPDATE inventory.stock_positions SET reserved = 0, updated_at = now() WHERE reserved <> 0;
```

- `inventory.reservations` was **emptied in the same transaction** as every checkout and seller order, so no live
  order retained a hold.
- `inventory.stock_positions.reserved` was then set to `0` for positions whose counter was non-zero. This is a
  **manual counter write**, not an owning release command. It was only coherent because the reservation journal it
  measures was emptied in the same transaction.
- **`on_hand` was never written by the cleanup.** No `on_hand` statement exists in `cleanup.sql`. Physical stock
  quantities were not restored, increased or "returned to availability" by this cleanup.
- The physical facts of earlier fake dispatches/consumptions were removed from the *journal*
  (`reservation_consumptions`), which does not change `on_hand` either.

## 4. Before / after quantities

| Point in time | Source | Key numbers |
| --- | --- | --- |
| pre-cleanup | `tooba_alpha-pre-tb266.dump` (validated listing only) | not read row-wise — see §5 |
| pre-final-cleanup | `tooba_alpha-pre-tb266-final-cleanup.dump` | same limitation |
| after final cleanup | `…/266/cleanup-counts.json` (read-only SQL counts, 2026-11-14T16:22Z) | checkouts 4 · seller_orders 4 · order_lines 4 · order_shipping_groups **0** · fulfillments 3 · shipments 0 · receipts 0 · claims 0 · packages 0 · damaged resolutions 0 · return_requests 0 · reservations 6 (3 Held + 3 Released) · `total_reserved` 3 · payments 3 · carts 8 · `settlement_entries` 0 |
| protected, unchanged before↔after | same file | stock_positions 3577 · offers 3223 · parties 10 · identity_users 86 · shipping_methods 7 · shipping_drafts 4 |
| final scoped deletion (266's own cancelled repair fixture) | `cleanup-counts.json.finalScopedDeletion` | 1 checkout / 1 seller order / 1 line / 1 fulfillment / 1 fake refunded payment / 1 cart; `total_reserved` stayed 3, every protected count identical |

## 5. What is UNVERIFIED, explicitly

These are **not** proven and are not claimed:

1. **Per-reservation owner mapping before the wipe.** Confirming that every deleted reservation row named a
   checkout that was itself deleted would require reading the pre-cleanup dump, which means restoring it into a
   database. Restoring is forbidden for this round and no second/disposable database is allowed, so the exact
   per-row ownership of the deleted holds is **UNVERIFIED**. The available evidence is structural: the deletion
   order in `cleanup.sql` and the fact that `inventory.reservations` was emptied inside the same transaction that
   emptied `order.checkouts`.
2. **Owner-safe release cannot be inferred from the surviving sum.** The final `total_reserved = 3` equals the sum
   of the 3 `Held` rows of the re-seeded trial orders. That is a consistency check of the *current* state only.
   It says nothing about how the earlier holds were removed, and it is not presented as proof of owner-safe release.
3. **No per-statement row counts were logged** for the original cleanup run. The script text and the before/after
   counts exist; per-table affected-row counts for that historical run do not, and were not fabricated.

## 6. This round

- No cleanup, delete, reset, reseed, restore, backfill or stock-counter write was performed.
- The four original TB266 trial orders were left exactly as found; nothing was retrofitted or removed.
- Current state after this task (read-only counts, `db-counts-after-r1.txt`):
  `carts 56 · checkouts 6 · fulfillments 5 · order_shipping_groups 2 · payments 5 · reservations 9 (4 Held) ·
  seller_orders 6 · settlement_entries 0 · shipments 1 · stock_positions 3577 · total_reserved 4`.
  The delta vs. the 266 final state is exactly this task's two new coherent trial orders and their one created +
  dispatched parcel.
