Insertion points:
- Admin Orders AppDataGrid (`admin-screens.tsx` createOrderColumns) — new `supply` column
- Admin Payments AppDataGrid receiptColumns — same column
- Order detail after inventory recovery banner + payment card — `OrderSupplyCard` + confirm hints
- Operations page DTO — supplyStatus/canConfirmDeposit/canRecoverInventory/shortage lines
- Backend list: AdminOrdersGridQueryEngine / AdminPaymentsGridQueryEngine call OrderSupplyComposer.GetStatusesAsync once per page
- Confirm messages from ProjectPaymentActions; recover hidden when confirm can auto-reacquire
No new top-level Supply menu.
