# Query / reporting — TB-P09-T015

Admin Orders grid:

- `lines` filter/sort: `SellerOrders.Sum(o => o.TotalItemCount)` (header, no line JOIN+SUM)
- `amount` filter/sort: `SellerOrders.Sum(o => o.GrandTotalSnapshot)`

Customer list:

- `ItemCount` = `orders.Sum(x => x.TotalItemCount)`
- List load: `Include(SellerOrders)` only; lines not loaded for list

Invoice print aggregates come from persisted header + `InvoiceHeaderSemantics` for count vs quantity labels.

Seller panel composer still has no `.Join(` / `FromSql` (composition tests).

No N+1 on the list primary path: one checkout query + header sums.

Export / FE mappings:

- Admin `lineCount` ← `LineCount` / `ItemCount` integer
- Customer `itemCount` ← integer header count
- Quantity remains on line / `TotalQuantity` separately
