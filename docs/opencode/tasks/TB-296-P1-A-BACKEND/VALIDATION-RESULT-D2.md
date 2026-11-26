# P1-A D2 candidate validation

Status: BLOCKED — no product/test modifications; Host and adjacent suites built, but Offer.Tests does not compile and concurrent/captured/recovery D2 scenarios remain NOT_RUN.
Candidate: 9a1d03653e7985bbdaeaaf940e133b50866bf905; D2 product f7f8b38adc6f5a0a1456ff9e3598b5c84614c496.

## Build/tests
- Host, Pricing, Inventory, Cart, Order, Fulfillment, Returns, Host.Tests build PASS.
- Offer.Tests build FAIL:
  - `OfferHistoryTests.cs(3,13)`: namespace `Tooba.Order` missing.
  - `OfferHistoryTests.cs(310,67)`: `IOrderOfferCommitmentReader` missing.
  - Offer.Tests project has no `Tooba.Order.Contracts` reference; focused CQRS test cannot run.
- Pricing 13/13, Inventory 53/53, Cart 29/29, Returns 191/191 PASS.
- Order 39 failed/730 passed/2 skipped; Fulfillment 28 failed/482 passed — identical counts to pre-task baseline on disposable DBs; no task-introduced regression. Baseline failures untouched.
- Host architecture 82 vs baseline 82, 0 new/0 fixed. `d2-host-arch-comparison.json`.
- Strict OpenSpec exit 0 with existing warnings.
- Localized Inventory error/resource focused tests 2/2 PASS.

## D2 provider source/HTTP (candidate SingleStore/tooba_alpha)
PASS:
- No-commit initial assign Seller, reassign Platform, reassign Seller, clear null, restore Seller all 200.
- Existing Held reservation -> responsibility Platform denied 409 `offer.fulfillment.change_protected`; no new history.
- Existing Consumed reservation -> denied 409.
- Explicit allocation row quantity 0 -> denied 409.
- D2 actual Inventory owner-contract lifecycle on new 9a1 source: allocate20, hold5→15, release→20, hold5→consume95/15/0, consume replay no double, expiry release, paid commit/consume, cross-position cap, damaged replacement bypass, pre/post-commit allocator fault injection; raw `d2-harness-lifecycle.log` + prior recovery logs.
NOT_RUN:
- Existing Order line / restorable canceled/paid order assignment denial (no safe test-only order fixture was created).
- Concurrent new hold and assignment, concurrent allocation and assignment, physical write after owner change, concurrent checkout captured-owner/reused-hold and assignment, partial release/consume, first-allocation concurrency.
- Offered/history HTTP capability matrix and actual new inventory-history target/location (admin inventory mutation on chosen seed returned 403 in prior run).
- Full frontend/registry proof remains P1-B/C; no frontend files changed here.

## Cleanup
- D2 offer/location/positions/reservation/allocation/history removed; P1A locations/offers = 0 in tooba_alpha and tb239.
- Candidate Host stopped; baseline SingleStore Host restored, DLL SHA-256 1A9604BD1889294542854B31A0DECB0D6F285A8788E6028A03B2EE63A32B23C8.
- Disposable DBs/roles/proxies dropped; tb239 harness fixtures removed.

No product, test, guard or contract modification. Exact unresolved test-project reference is reported, not repaired.
