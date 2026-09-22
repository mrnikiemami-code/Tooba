# TB-TMAR-ORDER-GOLDEN-001-R3B-R3 — typed fault origin

## Defect
R3B-R2 moved Message classification into adapters (`LooksLikeStableCode` / `TryMapExact(ex.Message)`).

## Repair
- Gutted `ContractOperationFault` (no Message classifier).
- Owning Domain/Directory throw `ContractOperationException(code)` for expected Admin Ops failures.
- Ops adapters (Fulfillment/Returns/Settlement/Payment/Inventory/CheckoutDirectory) no longer promote IOE via Message.
- Module ExceptionMappers catch `ContractOperationException` by `.Code` (and residual IOE by Message for non-ops paths).
- Architecture guard forbids Message promotion patterns on touched adapter paths.

## SoT
- Order: INCOMPLETE_REFERENCE_REPAIR
- nextTask: TB-TMAR-ORDER-GOLDEN-001-R4
- Checkout: PAUSED_AT_SAFE_W5_CHECKPOINT
- Slice: ADMIN_ORDER_OPERATIONS_TYPED_FAULT_SOURCE_R3B_R3
