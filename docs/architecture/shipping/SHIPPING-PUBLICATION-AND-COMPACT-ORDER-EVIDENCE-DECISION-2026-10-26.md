# Shipping publication, separate owner surfaces and compact order evidence — 2026-10-26

## CURRENT READ STATE — 2026-10-27 (TB-229)

`StorefrontShippingService.ProjectAsync` now composes the real captured path **read-only** behind
`Tooba:StorefrontCapturedPreview:Enabled` (default OFF): real cart → ONE `IOfferFulfillmentAssignmentLookup`
batch → explicit responsibility/type/seller partition → catalog bindings → ONE
`IShippingSelectedCapturedRevisionLookup.ReadAsync` per distinct owner+binding → ONE 218 `ComposeAsync`.
Additive nullable `CapturedPreview` on the projection; `null` when OFF. `20261027120000_AddOfferFulfillmentAssignment`
is APPLIED on local `tooba_alpha`/store-alpha. With the option ON, `SaveSelectionAsync`/`CommitAsync` refuse
first with `storefront.captured_preview.not_ready`; no order/reservation/payment path is open yet.
See `docs/architecture/evidence/TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229/RESULT.md` (PARTIAL: no live
browser trial, no positive composition executed). The compact per-order-group evidence rule below still applies
to the NEXT selection/commit slice and is still unimplemented.

User decision after TB222. Supersedes the temporary removal of publication UI, not the immutable revision architecture.

## Owner surfaces
Use a distinct marketplace admin entry/page and the existing store/seller surfaces, reusing the SAME authoring feature/form. Do not duplicate form logic or add a client owner-switch combo. Routes are UI boundaries only: existing server View/Manage authorization and edition/owner resolution remain mandatory on every read/write. Marketplace page is unavailable in SingleStore; never change edition to demonstrate it. SellerPartyId remains business identity, not actor identity; no StoreId schema redesign. Cross-owner administrative impersonation is not implicitly authorized by a new page.

## Publication
Restore a compact publication action/status in clean VIEW mode using the actual authorized publication API. Preserve exact revision + selection-version tokens, confirmation, synchronous duplicate gate, stale/unknown explicit-status-read recovery and no editor reseed. Do not restore snapshot GUIDs/technical version counters/large explanatory cards in the normal UI; diagnostics may be collapsed. Ordinary Apply edits only the mutable draft; it does not publish. Publishing captures one immutable revision then selects it. Withdrawal clears selection, not historical snapshots. Current publication is selection intent, NOT customer activation: no live Storefront consumer yet. State this briefly and honestly until connected.

## Mandatory order integration invariant — compact, not full-copy per order
Before enabling the captured path in real projection/selection/commit, each persisted ORDER SHIPPING GROUP must retain:
- stable method/owner reference and immutable captured snapshot id (source revision if needed by existing contract);
- matched Shipping area-group/rate-row identity, distinct from Order shipping-group identity;
- authoritative final freight/packaging and currency, plus existing checkout/COD and funder allocations required by the accepted financial model;
- chosen customer receiving date/interval and timezone sufficient to interpret it.
Reuse existing Order/Settlement shipping-group columns/contracts/allocations when they already express these facts. Do not introduce redundant parallel totals or duplicate columns without checking actual storage.
Do NOT duplicate the full authored JSON, all regions/rates/translations/week into every order. ONE immutable snapshot per owner+draft+source revision is shared by many orders. Only compact scalar references and final transactional facts belong to each order group.
Historical order amounts are immutable transaction facts; never recalculate an old order using current draft, latest pointer or current catalog price. At checkout commit revalidate selected capture, cart/version, trusted membership and authoritative quote; changed price/availability requires a new customer confirmation before payment. Persist coherent quote provenance with order creation under existing atomic boundaries; no UI-only guarantee.
Retain any immutable snapshot referenced by an order; no cascading deletion or unsafe cleanup. Cross-module references use Contracts and logical ids, no foreign DbContext/cross-schema FK. Pending physical facts remain unknown, never guessed.
Keep reads set-based/batched where supported, no per-line snapshot reads/N+1 and no repeat whole-cart financial quote per candidate. Inspect actual access patterns before adding an index; no generic performance framework or full snapshot-per-order storage.
No claim that this invariant is IMPLEMENTED by this document. No new schema or data write in this docs commit.

## Current task
TB-SHIPPING-ADMIN-PUBLICATION-OWNER-PAGES-223 restores visible publication and correct list entry separation first. Then the shortest actual Storefront projection/selection/commit connection must implement the invariant above, with focused persistence proof and measured query counts. No new calculator, speculative audit or re-opening resolved §29 product rules.
