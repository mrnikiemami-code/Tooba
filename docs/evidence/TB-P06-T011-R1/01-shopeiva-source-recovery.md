# 01 — Shopeiva source recovery (TB-P06-T011-R1)

Task: `TB-P06-T011-R1`
Date: 2026-08-27

## Problem (T011 gap)

TB-P06-T011 Result reported Shopeiva fidelity mapped from T010-R1 evidence because the Shopeiva reference tree was **not verified on disk** during implementation.

## Recovery

| Check | Result |
| --- | --- |
| Expected path | `SarvNewVerRequirment/reference/shopeiva` |
| Absolute path | `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva` |
| Relative from Tooba repo | `../SarvNewVerRequirment/reference/shopeiva` |
| Customer return modal | `src/components/dashboard/orders/returnFormModal.jsx` ✅ |
| Seller return review modal | `src/components/vendor/panel/orders/returnDetailModal.jsx` ✅ |

## Prior cross-reference

`docs/evidence/TB-P06-T010-R1/02-exact-shopeiva-source-map.md` — fulfillment map; return modals were not in T010-R1 scope but same reference root applies.

## Repair action

Return UI in `src/frontend/app/returns/return-ui.tsx` re-mapped against the recovered files:

- `RETURN_REASONS` dropdown + min-10-char description + amber eligibility banner + success step (customer)
- Sticky header, backdrop blur, two-step approve/reject with mandatory reject reason (seller)
- Accepted accent deviation: Tooba `#2563EB` vs Shopeiva `#E53935` (consistent with P05/P06 visual contract)

## Inventory restock gap

T011 shipped `IReturnInventoryGateway` as log-only/no-op. R1 implements `IInventoryReturnGateway` / `InventoryReturnGateway` with `return_restock_inbox` idempotency dedup in schema `inventory`.
