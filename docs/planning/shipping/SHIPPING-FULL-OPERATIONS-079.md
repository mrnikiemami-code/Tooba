# TB-SHIPPING-FULL-OPERATIONS-079 — planning

## Mission

Establish the **real, safe integration path** between the live storefront shipping projection and the
new Shipping operational quote, and record a **credible decision** about the remaining commercial
commitment constraints (capacity, promised slots) before the final 080 reconciliation. Nothing in
this task changes a customer price, a delivery promise, or a financial ledger.

## Mission A — real storefront read-only operational shadow (done)

A **separately gated, default-OFF, fail-isolated** observation seam now sits inside the existing
storefront projection read path (`ProjectStorefrontShippingHandler`) next to the earlier simple-rate
shadow.

- `StorefrontShippingOperationalShadowOptions` binds section `Tooba:ShippingOperationalShadow` with
  two independent flags (`Enabled`, `PrerequisitesConfirmed`). Both default `false`. The earlier
  simple-rate gate `Tooba:ShippingShadow` keeps exactly its old meaning; it was **not** broadened.
- The shared fail-closed policy is reused through a new overload
  `StorefrontShippingRateAuthorityPolicy.Evaluate(bool, bool)` that adds **no** new behaviour.
- `StorefrontShippingOperationalShadowObserver` resolves the canonical destination through
  `IGeographyLookup` (Contracts-only), then asks `IShippingOperationalQuoteLookup` for the
  operational quote of every legacy candidate. Both are Contracts seams; there is no foreign
  `DbContext`, no Host business logic, no `Results.Json`, no privilege bypass.
- The observer **never throws to the checkout caller**: any exception becomes
  `ShadowUnavailable`/`ShippingUnavailable` and the legacy response is returned untouched.
- The observer's return value is discarded by the handler. No monetary field, no promise field and
  no response byte of the storefront projection is derived from it.

### Measurement provenance (honest, not fabricated)

The storefront request today carries **no trusted physical measurement** of the shipment. The seam
therefore sends **no** weight and **no** item count. It never derives grams from cart subtotal, line
count or a caller-supplied arbitrary number. When a tier-conditioned policy would need a measurement,
the result is reported explicitly as a measurement gap and tier-conditioned parity is **skipped**, not
invented. Verified live: an identical request without a measurement returns
`FreightNotQuoted` / `shipping.quote.measurement_required` — it is never silently priced against a
tier.

### Observation content

Structured, low-cardinality: service/option, legacy amount, new freight/packaging/total,
payable-at-checkout vs collect-on-delivery, payer mode, schedule availability, capacity enforcement
state, shadow outcome and carrier reason code. No personal data, no cart contents, no destination
free text beyond what the legacy path already received, and no log line when the gate is off.

## Mission B — capacity & commercial cutover decision (done)

### Capacity: CONFIGURATION ONLY — atomic capacity is **NOT** implemented

- `ShippingCapacityEnforcement` still has exactly **one** declared value, `NotEnforced`, enforced by
  the type system and a guard test. A configured window number can never be read as a remaining
  reservation.
- Inspected the authoritative Order placement/cancellation transitions and the allowed shipment
  promise lifecycle: there is **no** unique order↔window relation, no transactional window bound, no
  expiration/replay strategy, and no settlement owner for a sold slot. An atomic, idempotent
  per-window claim/release would therefore be a **new** commitment ledger spanning Order, Shipping
  and Settlement — well beyond a bounded change inside this task and outside its allowed write paths.
- Decision: **do not fake enforcement**. Capacity remains configuration only. The system **cannot**
  sell guaranteed booking windows, and no number may be presented to a customer as remaining
  capacity.
- `ShippingOperatingSchedule` describes **carrier collection/dispatch availability**. It is **not** a
  customer delivery-slot reservation, and the seam never converts it into a promised delivery date.
  Verified live: the same quote returns `Available`, `Unavailable` (`outside_windows`) and
  `NotEvaluated` for the same amounts, i.e. schedule affects eligibility only and never the price.

### Cutover checklist (functional readiness vs commercial acceptance)

| # | Gate | State today | Owner |
|---|------|-------------|-------|
| 1 | Destination provenance (canonical Geography ids) | present for validated-id requests; legacy text-only requests are `DestinationNotComparable` and skipped | Shipping + Geography |
| 2 | Service + option identity parity | proven for all 5 live methods | Fulfillment |
| 3 | Real parcel weight/count provenance | **missing** — no trusted shipment packing aggregation in the storefront path | Fulfillment + Order |
| 4 | Fee + currency parity per method | proven byte-identical OFF/ON | Shipping |
| 5 | Unpaid vs COD ledgers; Payment/Settlement owner for the COD leg | **not implemented**; payer split is a fact only, no ledger entry exists | Payment/Settlement |
| 6 | Customer-facing pickup vs promised delivery date | carrier windows are availability, not a promise; no promise field is emitted | Product + Shipping |
| 7 | Merchant-approved tariffs and destinations | merchant must approve before any amount reaches a customer | Merchant |
| 8 | Schedule gating: absent / inactive / outside-window behaviour | fail-open for absent/inactive, constraint only for an active non-matching window; documented and tested | Shipping |
| 9 | Capacity enforcement or explicit out-of-scope decision | **explicitly out of scope** (configuration only) | Architect |
| 10 | Flag-safe rollback | gate OFF restores today's exact bytes; no config file, no migration, no schema change | Shipping |
| 11 | Packaging fee in the live checkout amount | **not allowed** without human pricing cutover approval, even for prepaid | Architect + Merchant |

**Functional technical readiness ≠ commercial acceptance.** The seam is technically ready to observe;
it is not authorized to price a paid order, to charge packaging, to collect COD, or to promise a slot.

## Mission C — focused Development parity (done)

Target positively verified as `dev-singlestore` → `tenant-alpha` → `tooba_alpha` on `127.0.0.1:5088`.
No production, no staging, no scratch database, no migration runner.

- **OFF/ON byte identity**: storefront projection payloads are byte-identical (`payloads-OFF.json` ==
  `payloads-ON.json`, 8553 bytes each, all three request shapes equal to each other at 2393 bytes), and
  the Shipping admin surfaces (`rates`, `packaging-policies`, `operating-schedules`) are byte-identical
  too. OFF produced **0** shadow log lines, ON produced the expected structured lines.
- **Charged packaging + CollectOnDelivery**: a task-owned disposable city-level packaging policy
  (fee 25000 IRR, `CollectOnDelivery`) produced `freight=75000 / packaging=25000 / total=100000 /
  checkoutDue=25000 / codDue=75000 / payer=CollectOnDelivery`, i.e. the split is a fact and the
  packaging leg stays on the checkout side.
- **Tier boundary 1999g / 2000g**: half-open tiers `[0,2000)` → 75000 and `[2000,5000)` → 95000
  resolved as 75000 at 1999g and 95000 at 2000g, with packaging 25000 added on both. An identical
  identity **without** a measurement returned `FreightNotQuoted` — the tier was never fabricated.
- **Cleanup**: every task-owned fixture deactivated through the authorized admin routes, with the
  before/after inventory recorded and a post-cleanup re-check showing the pre-existing area-level rule
  answering again. The 068/069 legacy parity fixture and all 071–078 records are preserved.
- The Development host was restored to the repository's safe defaults (gate OFF) and left running.

## Deliberately not claimed

- No customer shipping price or checkout cutover; no packaging amount in any live checkout.
- No Order/Payment/Settlement write; no COD ledger entry.
- No capacity enforcement, reservation, or remaining-capacity number.
- No delivery promise derived from a carrier operating window.
- No weight or item count manufactured for a tier comparison.
- No Task 080 work.

## Evidence

`docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-079/` — matrix logs (OFF/ON), payload and
baseline byte captures, tier/packaging/COD fixture log, controlled relaunch logs, host stdout/stderr
for both gate states, and the probe/relaunch/fixture scripts.
