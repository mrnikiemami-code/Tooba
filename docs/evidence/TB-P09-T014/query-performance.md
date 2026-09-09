# Query performance — TB-P09-T014

Admin order list quantity/filter/sort reads `SellerOrder.TotalQuantity` (header), not Line JOIN+SUM.

Invoice HTML uses header aggregates. Drill-down still reads lines.
