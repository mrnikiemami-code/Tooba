# ARCHITECTURE-PLAN — TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283

Protocol: TOOBA-DELIVERY-001
Base-SHA: 33c69baffdd8d0280de0c8a6edd1c625e0c4396e
Task-SHA256: 22b8d393d6c582021f80b4a15147c47b7fdcb2123992a9a47da68757906586df
Prepared-before-production-edit: yes; OpenSpec `seller-offer-safe-edit-283` created and validated before the first production edit.
OpenSpec: `openspec/changes/seller-offer-safe-edit-283/`

## Outcome and fixed requirements

| REQ | Actor / entry | Trigger / expected effect | Owner and source | Invariants | Acceptance scenarios |
|---|---|---|---|---|---|
| REQ-01 | Seller → `/vendor-panel/products/{offerId}` from list | Change SKU only on offer with positions 3 and 7 | Offer `PATCH /v1/seller/offers/{id}`; Inventory read | One patch; no price/stock write; new SKU, same price/reservation/3+7 | S-01/S-02 |
| REQ-02 | Seller edit | Draft SKU edit remains Draft; both limits clear | Offer patch/domain | Draft not transitioned; explicit null limits; unrelated fields preserved | S-03/S-04 |
| REQ-03 | Two seller sessions | First saves price, second stale form saves SKU only | Offer + Pricing independent commands | New price and SKU both survive; price write touches only price | S-05 |
| REQ-04 | Seller edit | Change only second position 7→8 | Inventory seller write with target StockItemId | 3+8 total 11; first position, SKU, price, reservation unchanged; ambiguous/foreign/below-reserved refused with zero change | S-06/S-07 |
| REQ-05 | Seller edit | 200 other offer, stale value, post-write disconnect | `seller-api` validation + independent GET | No false success; no automatic second write; GET decides | S-08/S-09 |
| REQ-06 | Seller edit | Cancel and 360×640 open dirty form | Existing page/Dialog controls | Zero writes on cancel; controls readable/reachable/clickable; overflowX 0 | S-10/S-11 |
| REQ-07 | Seller edit | Two sections changed; no-change save | Independent command dispatch | First save/failure does not clear/commit second; no-change = zero writes; reload both | S-12/S-13 |

## Architecture and real route

`vendor-panel/products/[offerId]/page.tsx` → `seller-api.ts` → seller headers (fixed seller scope) → `OfferSellerEndpoints` (`GET`, `PATCH`, `PUT price`, `PUT inventory`) → Offer CQRS handlers → Offering/Inventory owner directories; detail is composed by `OfferReadModelComposer` from Catalog/Pricing/Inventory/Party. The browser reads the same detail after save.

Inventory detail is extended with named positions from `InventoryAvailability.Locations`. The stock write accepts an optional target `StockItemId`; it must resolve to a position of the same offer and seller. With more than one position and no target, it fails with `inventory.position.id_required`. One-position legacy callers retain the old behavior.

## Contract and lifecycle

- Patch fields stay optional; absent preserves. Add explicit `ClearOrderQuantityLimits` for both-null. Send `Status` only when changed, so Draft stays Draft.
- Price write remains `Pricing`-owned; inventory write remains `Inventory`-owned. SKU-only must never invoke either.
- Stock write body adds `StockItemId`; response must contain the target position's new `OnHand`.
- Client validates expected offer, seller, and effect. 200 with another identity/stale value is `response_mismatch`; transport loss is uncertain. No blind retry.
- Cancel sends no command. Re-opening reloads authoritative values.

## Scenario matrix

| ID | REQ | Entry/role/provider | Action | Expected + side effects | Proof level | Before-fix observation |
|---|---|---|---|---|---|---|
| S-01 | REQ-01 | Browser seller, controlled HTTP | SKU-only save | One PATCH; no price/inventory requests | Browser transport | Existing form sent price and aggregate stock |
| S-02 | REQ-01 | PostgreSQL rollback-only | Apply SKU patch; read positions | New SKU, 3/7 unchanged, adjustment count 0 | PostgreSQL | Source: aggregate 10 would Set first to 10 |
| S-03 | REQ-02 | Focused Offer tests/API | Draft SKU patch | Remains Draft; no transition requirement | Backend test | Validator rejected Draft status |
| S-04 | REQ-02 | PostgreSQL rollback-only | Clear both limits | Both null after reread | PostgreSQL | Handler skipped nulls |
| S-05 | REQ-03 | PostgreSQL rollback-only/two contexts | Price then SKU-only | Both new values survive | PostgreSQL | Edit resent stale price/stock |
| S-06 | REQ-04 | PostgreSQL rollback-only | Target second 7→8 | 3+8, total 11, target first unchanged | PostgreSQL | Set first position with 8 |
| S-07 | REQ-04 | PostgreSQL/focused | Missing/foreign target; below reserve | Typed refusal, zero rows | PostgreSQL | First position chosen silently |
| S-08 | REQ-05 | Browser controlled HTTP | 200 wrong/stale | No success; mismatch error | Browser transport | `offerId` only was checked |
| S-09 | REQ-05 | Browser controlled HTTP | Disconnect after write | Uncertain message; no second write; GET shows truth | Browser transport | Success/lack of reconciliation not proven |
| S-10 | REQ-06 | Browser Chromium | Cancel dirty | Zero writes; reopen old | Browser | Existing cancel route not tested |
| S-11 | REQ-06 | Browser Chromium 360×640 | Open dirty form | Position title, save, cancel reachable; overflow 0 | Browser geometry | Desktop-only edit form |
| S-12 | REQ-07 | Browser controlled HTTP | Two sections / first failure | Other input preserved, not committed | Browser | Single monolithic save |
| S-13 | REQ-07 | Browser controlled HTTP | No-change save | Zero writes | Browser | Always sent three commands |

## Internal waves, same task

| Wave | Depends on | REQs | Change | Checks | Exit |
|---|---|---|---|---|---|
| W1 | none | REQ-01/04 | Inventory positions + named target write | Inventory/Offer focused tests | 3+7 preserved; 7→8 targeted |
| W2 | W1 | REQ-01/02/03/07 | Sectional save + patch semantics | Offer tests + client tests | SKU-only/limits/no-change independent |
| W3 | W2 | REQ-05/06 | Response validation, uncertainty, mobile form | Browser tests + tsc/build | UI no false success, 360×640 usable |
| W4 | W3 | all | PostgreSQL rollback-only fixtures | Real PG tests | Data invariants proven |
| W5 | W4 | all | Evidence/review gates | check-readiness | PASS or honest PARTIAL |

## Controlled data and evidence

- Browser: existing Next `3000` and Host `5088`; for deterministic response tests use Playwright controlled HTTP. The main save/re-read also needs owning backend/PostgreSQL; if an isolated HTTP backend cannot be safely provided, record the browser limitation and use rollback-only PostgreSQL for data proof.
- PostgreSQL: connection to canonical `tooba_alpha`, caller-owned transaction rolled back; unique `TB283-*` fixture rows; no commit, reset, seed, grant or live business write.
- No live offer/price/stock/reservation mutation, no migration/DDL, no provider/payment.
- Frontend focused tests, `tsc`, `next build`; backend focused tests/build because contracts/endpoints change. No full-suite loop.

## Checkpoint log

- W0 complete: repo/base verified; OpenSpec change/proposal/spec/design/tasks valid; plan and manifest prepared before production edits.
- W1..W5 pending; one repair round maximum after first acceptance run.
