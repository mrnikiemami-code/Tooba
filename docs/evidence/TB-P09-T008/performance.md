# Performance

Work-queue MapPageAsync batch-loads items, shipments, shipment lines, seller display names, order numbers for the page only — no per-row party/order queries.

Search seller/order number uses bounded Take(200) id lists then Contains filter.
