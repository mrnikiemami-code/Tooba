# Behavior Preservation Audit — TB-TMAR-NEXT-MODULE-BATCH-004-R2

## Shipping
- Routes unchanged
- Success JSON property shapes preserved via Application DTOs matching prior Host records
- Deactivate / ensure-seed success `{ ok: true }` preserved
- Expected failures → central ApiResponseFactory / SemanticError (`shipping_service.*`)
- Language fallback preserved
- Ensure-seed idempotent

## Work queue
- Prevalidation whole-set (unsupported/empty/cross-seller/row mismatch/incompatible/shipment resolve)
- Attempted / Succeeded counts preserved on partial downstream failure
- Supported SafeBulkActionCodes unchanged
- Order operations delegated via Order.Contracts adapter (same composer underneath)

## Returns
Untouched (compile-only N/A)
