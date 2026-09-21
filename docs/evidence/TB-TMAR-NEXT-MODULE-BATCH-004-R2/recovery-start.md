# Recovery Start — TB-TMAR-NEXT-MODULE-BATCH-004-R2

- Claim: `a847b957-2ca8-4c4d-ae7a-552eb9e5115d` (held)
- Baseline HEAD: `0f95c8d4f4770d880846042c621301a7fe6baddf` == `origin/main`
- Channel: `tooba-main` / Worker: `tooba-worker-01`
- Mode: FAST-SAFE Silent Bridge repair
- Parent: TB-TMAR-NEXT-MODULE-BATCH-004-R1 (PASS REOPENED as R2)

## Scope (R2 only)

1. Close Shipping Service admin HTTP/presentation/application boundary
   (`ShippingServiceEndpoints` → Host thin + Fulfillment Application CQRS + ApiResponseFactory).
2. Move Fulfillment work-queue bulk ownership out of Host
   (`AdminFulfillmentWorkQueueComposer` → `ExecuteAdminFulfillmentBulkCommand` + Order.Contracts seam).

## Out of scope

- BATCH-005
- Tax / Pricing / Checkout / frontend
- Returns behavior (compile-only fallout only)

## Verified defects at start

- `src/backend/Host/Tooba.Host/Admin/ShippingServiceEndpoints.cs` — Host projection, manual `{ title, errorCode }`, PlatformHttpException mapping, `ex.Message`, Localization.Application
- `src/backend/Host/Tooba.Host/Admin/AdminFulfillmentWorkQueueComposer.cs` — bulk validation/orchestration + AdminOrderOperationsComposer
