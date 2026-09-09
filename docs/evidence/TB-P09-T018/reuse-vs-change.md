# Reuse vs Change

Reuse:
- T008 work-queue endpoint, row DTO, AppDataGrid, quick filters
- Order Detail operations composer / dialogs / error map
- Exact `pack_selected` + shipment allocation from T010–T015
- T016 dispatch cancel guard

Change (defect-only):
- Human shipment/order/seller labels; quantity formatter
- Hide kebab when `availableActionCodes.length === 0`
- Order-number filter/sort
- Cancelled projects no forward codes; `correct_tracking` on Created+tracking
- Map stale dispatch/void exceptions to stable FA codes
- LOCK-OPS-006..008
