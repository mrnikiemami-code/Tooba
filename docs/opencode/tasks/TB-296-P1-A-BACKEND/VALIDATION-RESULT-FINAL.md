# P1-A final operational validation

Status: BLOCKED — build/guards/source regressions fixed and most provider scenarios executed; mandatory A3 hold/consume lifecycle and A4 fault-injection remain NOT_RUN.
Base/target: db60582121aee320ba1fb6fadd8251ac03e6a2b0; product b1b2113e27ecdb6244764923bae27cc5e91efaae.
No business/domain/contract changes. Added only the authorized structural split of existing reservation lifecycle methods into `InventoryDirectory.Reservations.cs` and updated source guards to read that partial; Offer guard invariant updated to the candidate's owner-scoped eligibility source.

## Source/build gates
- Host build PASS.
- Offer 133/133 PASS; Pricing 13/13 PASS; Inventory 53/53 PASS; Cart 29/29 PASS; Returns 191/191 PASS.
- Host architecture: candidate 82 failures vs pre-task baseline 82 failures, 0 new, 0 fixed; unrelated baseline failures untouched. `host-arch-comparison.json`, baseline/candidate TRX.
- OpenSpec strict exit 0 with existing warnings.
- Exhaustive A1/A2/A4 source guards included in Offer/Inventory tests PASS.

## Exact candidate DEV provider (SingleStore/tooba_alpha)
- Host candidate build published and health ready; migration 20261126190000 applied. HTTP/PostgreSQL applicable checks 14/14 PASS: owner scopes, unknown/inactive/foreign denial, no failed-write positions/facts, physical concurrent one-winner 110 + one 409 with winner-only facts, missing/stale/replay/changed payload, price existing/first-create/concurrent winner facts, legacy real price revision, stable history IDs/locations/actor/old/new, admin vs seller capabilities, duplicate active denial.
- Fixtures cleaned; Host restored old PID/hash.

## Authorized isolated Marketplace validation (tooba_marketplace_tb239)
- Platform offer 100 physical/no allocation: availability 0 PASS.
- Set offered quantity 20: availability 20, offeredQuantity 20, allocation revision returned; seller capabilities physical false/offered true PASS.
- Set 101: 409 `inventory.allocation.exceeds_physical` PASS.
- Held=5 state: availability 15, allocationHeld 5 PASS.
- Set 4 below held: 409 `inventory.allocation.below_held` PASS.
- Seller-owned location positive: seller-fulfilled offer physical 0→10 at own Seller location PASS; platform seller physical 403 PASS.
- History from price/allocation facts: eventId/targetId/actor/old/new present PASS.
- NOT_RUN: actual reserve/hold→release→20 and hold→consume(95/15/0) over HTTP; guest checkout returns 401 authentication required and there is no direct reservation HTTP endpoint. Held state was inserted by scoped SQL only for allocation read/update checks, not claimed as reserve-path proof.
- NOT_RUN: first-allocation concurrency and multi-position shared cap (fixture attempt did not yield a clean HTTP identity after host switch).
- NOT_RUN: A4 failure-injection before/after owner commit (no authorized fault seam).
- Marketplace admin capability route returned 403 via available dev actors; admin-vs-seller capabilities were verified on SingleStore instead.
- Marketplace fixtures cleaned (0 P1A locations/offers/cart/allocation); temporary DB `tooba_p1a_mkt_validate` dropped; Host restored to SingleStore.

## Disposable relational DBs
- Created `order_tb231`, `order_tb253r3`, `fulfillment_tb196`, `fulfillment_tb196_model`, roles tb231/tb253r3/tb196, TCP forwards 15433/55436 to existing 5432.
- Candidate Order: 39 failed/730 passed/2 skipped; baseline 55de: identical 39/730/2. Candidate Fulfillment: 28 failed/482 passed; baseline: identical 28/482. No task-introduced relational regression; baseline failures remain (first example `fulfillment.order.not_found`).
- DBs/roles dropped, proxies stopped.

## Remaining blocker
A3 actual reservation lifecycle and A4 owner-commit fault injection are NOT_RUN; no alternative HTTP/authenticated checkout/fault seam was available without changing product or tests. No PASS claim for those mandatory scenarios.
