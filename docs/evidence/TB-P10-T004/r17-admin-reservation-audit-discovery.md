# R17 Admin reservation audit discovery

## Surfaces

- Orders grid: `AdminOrdersGridQueryEngine.MapPageAsync` already batches `GetStatusesAsync`. AppDataGrid `POST /v1/admin/orders/query`. Saved views: `ADMIN_ORDER_GRID_VIEW_KEY`.
- Order Detail: `admin-order-detail-screen.tsx` — R8 `OrderSupplyCard` at ~566. Insert رزرو موجودی immediately after it. Do not merge SupplyStatus card.
- Payments grid: `AdminPaymentsGridQueryEngine.MapPageAsync` already batches supply. Receipts screen `ADMIN_RECEIPT_GRID_VIEW_KEY`.
- Payment concise context: Order Detail «اطلاعات پرداخت» + receipts compact column. No standalone payment-detail page.
- Cycle projection: `IReservationCycleDirectory.GetProjectionAsync` / `GetProjectionsAsync` / `ListEventsAsync` (R15).
- Capabilities: `canConfirmDeposit` / `canRecoverInventory` already exist. Confirm already auto-reacquires. No retry-reservation button. No timer extend.

## Insertion choice

Separate column «رزرو موجودی» beside SupplyStatus (not combined cell) so LOCK-SF-092 statuses stay distinct. Filter field `reservation` on `ReservationState`.
