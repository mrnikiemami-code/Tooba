# Recovery Start — TB-TMAR-NEXT-MODULE-BATCH-004-R3

- Claim: `e6f4db14-2872-4cf0-b610-e6a3634ab40a` (held)
- Baseline HEAD: `02978a2d4b353c2457a72895d9b8a6f892841925` == `origin/main`
- Channel: `tooba-main` / Worker: `tooba-worker-01`
- Mode: FAST-SAFE Silent Bridge repair
- Parent: TB-TMAR-NEXT-MODULE-BATCH-004-R2 (PASS REOPENED as R3)

## Scope (R3 only)

1. Move `IAdminOrderFulfillmentOperations` implementation to Order-owned assembly;
   delete `HostAdminOrderFulfillmentOperations.cs`; no Host delegate/callback.
2. Move shipping-methods tree projection (`ListEnabledMethodsTreeAsync` /
   `DefaultColor` / `DefaultOptions`) into Fulfillment.Application query ownership;
   Host callers thin transport only.

## Out of scope

- BATCH-005
- Tax / Pricing / Checkout / frontend
- Returns behavior (compile-only fallout only)
- Broad Order recovery

## Verified defects at start

- `src/backend/Host/Tooba.Host/Admin/HostAdminOrderFulfillmentOperations.cs` — Host implements Order.Contracts ops via AdminOrderOperationsComposer
- `src/backend/Host/Tooba.Host/Admin/ShippingServiceEndpoints.cs` — `ListEnabledMethodsTreeAsync` + DefaultColor/DefaultOptions Host projection
