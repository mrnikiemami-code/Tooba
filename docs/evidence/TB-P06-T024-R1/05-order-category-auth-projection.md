# 05 — Order category authorization projection

Task: TB-P06-T024-R1

## Problem

Seller order list/detail/action must filter by Category scope without Order→Catalog SQL JOIN or N+1 Catalog calls per list row.

## Architecture — snapshot + batch backfill

```
Checkout (QuoteSellerOrdersAsync)
  └─ batch GetPrimaryCategoryIdsByVariantIdsAsync(variantIds)
  └─ OrderLine.FromCheckout(…, categoryIdSnapshot: categoryByVariant[variantId])

Order DB (order_lines.category_id_snapshot)
  └─ indexed (migration 20260827180000_AddOrderLineCategoryIdSnapshot)

Runtime read (SellerPanelComposer / FulfillmentEndpoints)
  └─ prefer line.CategoryIdSnapshot
  └─ else single batch ResolveLineCategoriesAsync → GetPrimaryCategoryIdsByVariantIdsAsync
```

## Key types / files

| Artifact | Location |
|----------|----------|
| `OrderLine.CategoryIdSnapshot` | `OrderDomain.cs` |
| Column + index | `OrderDbContext.cs`, migration `20260827180000` |
| Snapshot at checkout | `CheckoutDirectory.QuoteSellerOrdersAsync` — one batch catalog call per checkout quote |
| Batch backfill helper | `SellerPanelComposer.ResolveLineCategoriesAsync` |
| Catalog contract | `ICatalogLookupGateway.GetPrimaryCategoryIdsByVariantIdsAsync` |

## Constraints satisfied

| Constraint | Status |
|------------|--------|
| No Order→Catalog SQL JOIN | YES — gateway call at Host boundary only |
| No foreign repository/table access from Order module | YES — snapshot on write; lookup via injected gateway |
| No runtime N+1 across order lists | YES — one batch resolve per list/detail for lines missing snapshot |
| Extraction-ready | YES — snapshot is denormalized auth projection on order line |

## Mixed-order policy (data layer)

Each line carries its own `CategoryIdSnapshot`. Authorization evaluates per line category; list includes order if **any** line matches allowed categories; detail returns **only authorized lines** and recomputes totals from those lines.

## Legacy lines

Pre-migration lines with `CategoryIdSnapshot = null` use batch variant→category resolve once per request — not persisted back automatically in this task.
