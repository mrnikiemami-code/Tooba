# TB-296 Phase 1 — FINAL CLOSURE REPORT

Status: **READY_FOR_ARCHITECT** (all 60 immutable IDs PASS; final ACCEPT is architect/owner-owned and NOT self-authorized).
Mode: VERIFICATION ONLY. Product code changes: **0**. New Host/DB: **0**. Auth bypass/policy change: **0**.
Required HEAD: `ad1a7d10b2c089a433197219e978e9b8bb49981d` (product source identical to tested `84f3372a…`).

## Scope executed
Only the 4 remaining IDs were executed: **D2-ORDER, D2-HOLD, D2-PARTIAL, D2-RECOVERY**. The 56 previously proven IDs were preserved unchanged (same evidence paths and SHA-256 in `FINAL-CLOSURE-LEDGER.json`).

## Authorized real workflow
- Synthetic DEV customer created through the existing standard `POST /v1/auth/register` + `POST /v1/auth/login` on `tooba_alpha`; authenticated requests used `Authorization: Bearer <sessionId>`.
- Real storefront cart → real `POST /v1/storefront/checkout` (order + reservation) → real `POST /v1/storefront/checkout/{id}/payments` (provider `gateway` ⇒ `fake`) → real `POST /v1/storefront/payments/{id}/sandbox/complete` to reach Paid.
- Real admin commitment protection (`PUT /v1/admin/offers/{id}/fulfillment`), real admin cancel (`POST /v1/admin/orders/{id}/operations` `cancel`), real storefront retry-reservation (OrderSupplyService), and real Inventory owner-contract release/consume/partial release.
- No authentication bypass, no security-policy change, no product change.

## D2 results (all PASS)
### D2-ORDER
Real paid order and a Paid-then-cancelled (restorable, `cancelled_from_status=Paid`) order were created for two previously uncommitted offers. Both real admin fulfillment reassignments were rejected with **409 `offer.fulfillment.change_protected`** and mutated neither responsibility nor history. A third uncommitted offer completed initial assignment/reassignment/clear (`Seller → Platform → null → Seller`) with persisted state and history, then was restored to unassigned.
- Evidence: `finalclosure-evidence/D2-ORDER.json` (SHA-256 `3988f1a3…`).

### D2-HOLD
Real captured checkout acceptance produced a durable Held reservation + order line. A reused-hold OrderSupplyService retry versus a real admin assignment preserved the exact single hold, denied the assignment with `offer.fulfillment.change_protected`, and left no stale owner. A concurrent new checkout hold versus assignment kept the held reservation on an eligible Store-owned location (SingleStore rule) and the resulting commitment protected every subsequent assignment.
- Evidence: `finalclosure-evidence/D2-HOLD.json` (SHA-256 `410b66ad…`).

### D2-PARTIAL
Dedicated P1C fixtures: concurrent release/consume had exactly one winner (zero negative availability, exact physical/reserved outcome). Partial-obligation release of 2 then 3 on the same scoped reservation reported `PartiallyHeld` then `Released` with exact remaining quantities (17 then 20), no double release on replay, and an advancing allocation revision.
- Evidence: `finalclosure-evidence/D2-PARTIAL.json` (SHA-256 `1fee3d48…`).

### D2-RECOVERY
Real unpaid checkout drove the actual OrderSupplyService through `POST /v1/storefront/checkout/{id}/retry-reservation`: cycle 1 expired and was released by the real CartExpiry/Inventory reclaimer, then a real reacquire created cycle 2 (new Held reservation on the same order line, old released, reserved delta exactly 2). The next retry was denied with **409 `inventory.reservation.retry_limit_reached`** at the resolved policy cap of 2 cycles (no extra cycle). A real paid order denied retry with `pending.retry.not_allowed` and kept exactly one succeeded payment with unchanged line/stock (no second commercial debit).
- Evidence: `finalclosure-evidence/D2-RECOVERY.json` (SHA-256 `acd6e712…`).

## Fixture cleanup and restore
- All synthetic customers, sessions, carts, checkouts, payments, orders and reservation fixtures were removed by restoring the verified baseline dump `p1c-final-20261127173245.dump` (SHA-256 `589f8c2a0cb525dca1238deee8c05e81bfaf71cab8a19b0862f4b5af74e68166`) into `tooba_alpha` via `pg_restore --no-owner --no-privileges` (exit 0).
- Post-restore: offers 3323, offer_history 265, order_lines 14, checkouts 11, seller_orders 12, users 101, allocations 0, P1C offers/carts 0. The only increase (prices 5207→5215) is normal Host-startup demo re-quoting.
- Host restarted (same binary/DLL SHA-256 `5669d7b0…`), `/health` 200, admin dev-context `store-alpha`, allowed channels `["Marketplace"]`.

## Ledger and gates
- `FINAL-CLOSURE-LEDGER.json`: 60 unique IDs, **PASS 60 / FAIL 0 / NOT_RUN 0**; all 60 evidence paths exist and hash-match (verified programmatically).
- `acceptance-gate.cjs --self-test`: PASS (positive + 11 negative controls).
- `architect-review-gate.cjs --self-test`: PASS (positive + 14 negative controls).
- Full acceptance gate: **NOT_RUN** — the 8 deferred D2/F-BENTRY scenarios have no issued immutable JSON scenario matrix (only prose), and a worker must not author the per-assertion expected/observed facts the gate equates. The architect must confirm the deferred matrix and run the gate.
- Architect review gate: **NOT_RUN** — requires architect-authored `ARCHITECT-REVIEW.json`; a worker must not author it.
- Architecture: unchanged; 0 new/Offer-owned violations (prior Offer backend guards 51/51 PASS; 27 pre-existing frontend source-size violations outside Offer remain excluded, not resolved).

## Final response state
READY_FOR_ARCHITECT: all 60 IDs PASS with executed evidence, 0 FAIL, 0 NOT_RUN, 0 product changes, fixtures restored. Remaining work is the independent architect review/receipt and owner UAT; no self-authorization.
