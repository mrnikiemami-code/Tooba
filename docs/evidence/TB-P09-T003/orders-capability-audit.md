# TB-P09-T003 — Orders capability audit

## Grid
- Server grid: `AdminOrdersScreen` + `ServerGridPage` (`admin-screens.tsx`)
- View action: `orderRowActions` id=`view` → `/admin/orders/{checkoutId}`
- Contextual ops: `AdminOrderOperationsMenu`
- List API: `/v1/admin/orders` (Host grid query)

## Detail
- `admin-order-detail-screen.tsx`: read-only summary/sellers/payments, `عملیات سفارش`, notes, operational history, invoice/receipt
- Completeness APIs: `/notes`, `/operational-history`, `/invoice.html`, `/receipt.html`

## Operations (Host)
- `AdminOrderOperationsComposer` / `/v1/admin/orders/{id}/operations`
- cancel, mark_processing, mark_packed, create_shipment, assign_tracking, dispatch_shipment, deliver_shipment, request/approve/reject_return, retry_refund
- Permission + lifecycle projection; direct invalid POST → 400 `order.operation.invalid` / domain guards

## Finance
- Detail includes sellerFinancials / financialEvents / financialSummary
- Settlement accrual/adjust paths retained (T001/R1 evidence); completed settlement immutable

## Notes/History/Invoice
- Append-only notes; human actors (T002-R1); snapshot invoice/receipt
