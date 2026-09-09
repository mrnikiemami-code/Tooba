# Performance

`MapPageAsync` still batches items, shipments, shipment lines, seller names, order numbers. No per-row hydration. Search/filter for order number uses SellerOrders Take(500) then Contains — same pattern as seller name. No N+1 added.
