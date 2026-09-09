# Header semantics — TB-P09-T014-R1

Reuse existing fields. No synonym columns.

- `TotalItemCount` / API `LineCount` = integer number of distinct order lines.
- `TotalQuantity` = decimal mechanical sum of line quantities.
- Example: lines 1.25, 2.00, 1 → LineCount = 3, TotalQuantity = 4.25.
- `InvoiceHeaderSemantics` is the single Host mapper. Primary list/detail/filter/sort read header aggregates, not Line JOIN+SUM.
