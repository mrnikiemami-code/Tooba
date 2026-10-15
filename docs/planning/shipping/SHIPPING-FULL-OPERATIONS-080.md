# TB-SHIPPING-FULL-OPERATIONS-080 — final operational acceptance and recovery

Task: `TB-SHIPPING-FULL-OPERATIONS-080` (task **10 of 10** — the final authorized task of program
`TOOBA-SHIPPING-FULL-OPERATIONS`) · Parent: `...-079` · Timebox: 45 minutes HARD
Expected-Base-SHA: `247f784c6a4f6d4f0cdd564467c64937be22abfd`

## Verdict in one line

**Technical program closure: PASS for the bounded scope actually delivered.**
**Commercial production go-live: NOT READY — Checkout must NOT be switched on.**

These two scores are deliberately separate. A merchant-ready launch is **not** claimed anywhere in
this document.

---

## A. What works today in Development

Independently reconciled against actual source (not worker PASS claims). Evidence folder:
`docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-080/`.

| # | Capability | Verdict | Source of truth |
|---|-----------|---------|-----------------|
| 1 | Service + option ownership | **IMPLEMENTED_AND_PROVEN** | Fulfillment owns the catalog: `Tooba.Fulfillment.Application/Shipping/ListEnabledShippingMethodsTreeQuery.cs`, `ShippingProviderMetadata.cs`; Shipping consumes the codes only. Live: 5 methods (`post`, `tipax`, `store_courier`, `in_person`; `snapp_courier` inactive). |
| 2 | Geography destination selection | **IMPLEMENTED_AND_PROVEN** | `Tooba.Shipping.Endpoints/Admin/…Geography*Endpoints` (read-only), `Tooba.Geography.Contracts/Places/IGeographyLookup`; admin picker `src/frontend/app/admin/shipping-services/shipping-destination-picker.tsx`. Live: IR/alborz/karaj ids resolve. |
| 3 | Rates + half-open weight/count tiers | **IMPLEMENTED_AND_PROVEN** | `Tooba.Shipping.Contracts/Rates/ShippingRatePolicyContracts.cs`, `ShippingRatePolicy.cs`, `ShippingAdminContracts.cs` (`ShippingNumericTierWrite`, `ShippingRateConditionWrite`); UI model `shipping-rate-tier-model.ts` (`min` inclusive, `max` exclusive, `null` = open). Live boundary proof 1999g vs 2000g in 079 evidence. |
| 4 | Packaging fee + freight payer mode | **IMPLEMENTED_AND_PROVEN** | `Tooba.Shipping.Contracts/Packaging/ShippingPackagingContracts.cs`; `ShippingPackagingPolicyEndpoints.cs`; UI `shipping-packaging-dialog.tsx` / `shipping-packaging-model.ts`. Live COD split proof in 079 evidence. |
| 5 | Weekly operating windows | **IMPLEMENTED_AND_PROVEN** | `ShippingOperatingScheduleEndpoints.cs`, schedule evaluator; UI `shipping-operating-schedule-dialog.tsx`. Live: `tipax/standard` active, `tipax/express` inactive, others absent. |
| 6 | Capacity | **CONFIGURED_ONLY** | `ShippingCapacityEnforcement` has exactly one value `NotEnforced` (guard test `OperationalQuoteCompatibilityTests`). Live: `capacity=25 enforcement=NotEnforced` in every probe. |
| 7 | Admin UI entry points | **IMPLEMENTED_AND_PROVEN (source-inspected)** | `src/frontend/app/admin/shipping-services-screen.tsx` row actions: `edit` ویرایش, `rates` نرخ و پوشش, `schedule` برنامه کاری ارسال, `packaging` هزینه بسته‌بندی و پرداخت کرایه, `quote` بررسی هزینه و شرایط ارسال, `deactivate` غیرفعال‌سازی. Dialogs in `src/frontend/app/admin/shipping-services/`. |
| 8 | Operational diagnostic | **IMPLEMENTED_AND_PROVEN** | `POST /v1/admin/shipping-services/{serviceCode}/operational-quote` — read-only, `fulfillment.view`. |
| 9 | Read-only real storefront shadow | **IMPLEMENTED_AND_PROVEN** | `Tooba.Order.Application/Storefront/Shipping/OperationalShadow/**` composed in `ProjectStorefrontShippingQuery.cs`. |
| 10 | Current checkout price authority | **LEGACY / UNCHANGED** | `StorefrontShippingService` + `ShippingMethodsOptions` + `StorefrontShippingCalculator`; `StorefrontCheckoutService` still writes `shippingAmount` from the legacy draft. No new authority. |
| 11 | Weekdays | **IMPLEMENTED_AND_PROVEN** | `ShippingOperatingWindowDefinition(Weekday, …)`, live weekday 7 (Sunday). |
| 12 | Multiple time windows | **IMPLEMENTED_AND_PROVEN** | Live `tipax/standard`: 13:00–14:00 and 16:30–18:00 on one weekday. |
| 13 | Timezone | **IMPLEMENTED_AND_PROVEN** | Explicit IANA `Asia/Tehran`; the 078 inspector refuses ambiguous/nonexistent local times rather than guessing. |
| 14 | Dispatch vs promised delivery | **CONFIGURED_ONLY (availability only)** | `ShippingScheduleEvaluation.IsConstraintSatisfied`; no promise field exists. Carrier windows are collection/dispatch availability. Owner of a real promise: Order. |
| 15 | Capacity limit / reservation | **NOT_IMPLEMENTED** | See §C.1. Owner: Order + Shipping + Settlement (joint, needs separate authorization). |
| 16 | Weight intervals | **IMPLEMENTED_AND_PROVEN** | Half-open `[min, max)` grams; integer grams on purpose. |
| 17 | Physical item-count intervals | **IMPLEMENTED_AND_PROVEN** | `itemCount` axis exists end-to-end (contract, domain, UI model, live admin dialog). |
| 18 | Rate options | **IMPLEMENTED_AND_PROVEN** | `optionCode` travels separately from `serviceCode`; live `tipax:standard` vs `tipax:express`. |
| 19 | Packaging | **IMPLEMENTED_AND_PROVEN** | Flat fee, own currency, own scope, own payer rule. |
| 20 | Prepaid | **IMPLEMENTED_AND_PROVEN** | `ShippingFreightPayerMode.Prepaid`; live `checkoutDue=freight`, `codDue=0`. |
| 21 | COD | **IMPLEMENTED_AND_PROVEN (as a rate fact)** | `CollectOnDelivery`; live `checkoutDue=packaging`, `codDue=freight`. No money moves. |
| 22 | Weight/count provenance from a real packed shipment | **NOT_IMPLEMENTED** | See §C.2 — the single largest blocker. Owner: Fulfillment + Order. |
| 23 | Final checkout pricing | **NOT_IMPLEMENTED (by design)** | Legacy authority retained; no cutover. Owner: Architect + Merchant. |
| 24 | COD financial ledger / settlement | **NOT_IMPLEMENTED** | No Payment/Settlement transaction is created by the payer-mode fact. Owner: Payment + Settlement. |
| 25 | Delivery promise | **NOT_IMPLEMENTED** | No promised/estimated date is derived from a carrier window. Owner: Order (promise) + Product. |
| 26 | Commercial tariff / coverage approval | **BUSINESS_APPROVAL_REQUIRED** | See §D. Owner: Merchant (business), Architect (gate). |

**Checkout authority, stated exactly.** `ProjectStorefrontShippingHandler` still returns the legacy
`StorefrontShippingProjection`, and `StorefrontCheckoutService` still stores
`shippingAmount` from the legacy draft into the paid snapshot. The operational quote is reachable only
through the admin diagnostic route and the OFF-by-default observation seam. Nothing in the
operational path writes an order, a price, a promise or a ledger entry.

## B. Observed evidence and commands

All commands are read-only against the **positively verified** Development target
`edition=SingleStore · deploymentId=dev-singlestore · tenantId=store-alpha ·
connectionReference=tenant-alpha` → database `tooba_alpha` on `http://127.0.0.1:5088`.

```text
GET  /__platform-commerce                                  -> provenance confirmed (see log)
POST /v1/storefront/shipping/projection                     -> 200, length 2393, 5 methods, subtotal 8702542, IRR
GET  /v1/admin/shipping-services/{service}/rates            -> per-service rate + coverage inventory
GET  /v1/admin/shipping-services/{service}/packaging-policies
GET  /v1/admin/shipping-services/{service}/operating-schedules
dotnet test ... --filter FullyQualifiedName~StorefrontShippingOperationalShadow -> 26 passed / 0 failed
```

Evidence files in `docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-080/`:

- `log-final-gate-verification.txt` — provenance, current host pid/start time, current-process
  projection call, and the re-used 079 byte-identity proof.
- `final-current-projection.json` — the exact current-process legacy response body (2393 bytes).
- `log-final-dev-inventory.txt` — full read-only rate/coverage/packaging/schedule inventory for all
  five service codes, including active/inactive flags.
- `verify-final-gates.ps1`, `inventory-dev-fixtures.ps1`, `claim-task.js`, `task-receipt.json`, `task.md`.

**Gate-OFF verification on the CURRENT process** (no restart, no override): the repository carries no
`Tooba:ShippingRateAuthority` and no `Tooba:ShippingOperationalShadow` section in any appsettings, so
both gates run on their safe defaults `false/false`. The running process (pid 33548, started
13:31:13) produced **0** `StorefrontShippingOperationalShadowObserver` lines and **0**
`StorefrontShippingShadowObserver` lines while serving the projection request. The earlier simple-rate
gate `Tooba:ShippingRateAuthority` was **not** renamed or broadened.

**Legacy response parity** is re-used from the 079 byte-level capture rather than re-running the whole
matrix: `payloads-OFF.json == payloads-ON.json` at 8553 bytes (all three request shapes equal at
2393 bytes), and the Shipping admin surfaces byte-identical OFF/ON (2636 / 1337 / 1055 bytes). The
current-process body captured here is the same 2393-byte shape.

**Admin UI**: **source-inspected, not interactively re-verified in this task.** The five row actions
and their dialogs are confirmed present in `shipping-services-screen.tsx` and
`src/frontend/app/admin/shipping-services/`. A real interactive browser roundtrip was NOT performed in
080 (timebox), so this is reported as source-inspection, not live UI proof. 078 performed a real
browser roundtrip for the operational-quote inspector and its evidence remains authoritative for that
one dialog.

## C. What remains blocked and who owns it

### C.1 Atomic capacity booking — **NOT READY**

Missing, concretely: a unique order↔window relation; a transactional bound that claims a slot inside
the order-placement transaction; an idempotent claim/release pair with a replay strategy; an
expiration/cancellation path that returns a slot; and a ledger owner that can settle a sold slot.
`ConfiguredCapacityTotal` is a **configuration number** and must never be read as remaining capacity.
Implementing this correctly is a new commitment ledger spanning Order, Shipping and Settlement — a
separate, explicitly authorized program. **Owner: Order + Shipping + Settlement.**

### C.2 Packed weight / physical item-count provenance — **NOT READY** (largest blocker)

Inspected both sides:

- Shipping's input contract `ShippingMeasuredParcel(long? WeightGrams, int? ItemCount)` exists and is
  deliberately fail-closed.
- **Fulfillment Contracts expose no measured packed weight and no packed-unit count.**
  `Tooba.Fulfillment.Contracts` has no weight/grams/parcel member at all; the only package-count
  concept is a shipment-metadata field in the admin create-shipment modal
  (`src/frontend/app/admin/admin-create-shipment-modal.tsx`), which is operator-entered provider
  metadata, not an authoritative measured shipment aggregate.
- Catalog has no weight/grams member either.

Therefore the real storefront path **cannot** supply a trustworthy measurement today, and the 079
observer correctly reports `MeasurementUnavailable` / `FreightNotQuoted` instead of inventing one. No
weight is derived from cart subtotal, cart lines, product count or arbitrary client input, and no
placeholder default was added. A conditioned active rate therefore fails closed rather than silently
pricing the wrong tier. **Owner: Fulfillment (measurement source) + Order (packing aggregation),
scoped to a future separately authorized program.**

### C.3 COD settlement — **NOT READY**

The payer split is a **rate fact** only. `CollectOnDelivery` creates no Payment/Settlement
transaction, and this task deliberately did not touch those modules. **Owner: Payment + Settlement.**

### C.4 Other carried gaps

- Destination identity: the storefront projection still receives province/city text; canonical
  Geography ids are optional and unresolved text falls back to `DestinationNotComparable` rather than
  a guess. Owner: Order + Geography.
- `snapp_courier` is still configured as a Fulfillment service with no usable rate/coverage/schedule
  row (live inventory: empty). Its catalog row was not verified in this task — reported as
  **unverified**, not fixed. Owner: Fulfillment (catalog row) + Architect (decision).
- Pre-existing Host architecture guard issue (`HostAdminCanon001GuardTests…` referencing an absent
  former Party path) is unrelated to Shipping and was not touched or weakened.

## D. May Checkout be switched on?

**NO.** Absent all of the following, a cutover would sell prices and promises the platform cannot
honestly honour:

1. merchant-approved commercial tariffs and destination coverage (not synthetic fixtures);
2. an authoritative packed weight/count provenance (§C.2);
3. an explicit, human-approved pricing cutover decision for packaging, including the prepaid case;
4. a Payment/Settlement owner for the COD leg (§C.3);
5. an explicit decision on capacity (enforce, or formally sell no guaranteed slots);
6. a separate Architect/user authorization of the switch itself — **no cutover switch exists**.

### Minimum merchant data + approval actions (business gate)

Synthetic Alborz fixtures and the deactivated 079 tier/packaging rows are **not** merchant approved
and must not be activated by an agent. A merchant must, at minimum:

- confirm the real tariff per active service/option per destination scope (country / area / city /
  service area) and deactivate anything not commercially approved;
- confirm the real packaging fee per service/option/scope and who pays it, including whether the
  checkout side may ever carry it;
- confirm collection duties (who collects what, when) and seller preparation promises
  (`preparationDays`) and lead days per service;
- confirm which services are actually offered (including whether `snapp_courier` is in or out);
- approve the destination coverage grants that are supposed to be sellable.

## E. Development fixture inventory and restored OFF gates

Read-only inventory captured in `log-final-dev-inventory.txt`. Summary:

| Service | Active rates | Active grants | Packaging | Schedules |
|---------|--------------|---------------|-----------|-----------|
| `post` | 1 (area scope, 120000 IRR, prep 1 / lead 4) | 1 (area) | none | none |
| `tipax` | 2 (area scope: `standard` 180000, `express` 260000) | 1 (area) | 3 rows, **all inactive** | 2 rows: `standard` **active** (Sun 13:00–14:00, 16:30–18:00, cap 25), `express` inactive |
| `store_courier` | 1 active (area, 75000) + 2 **inactive** 079 tiers (city, 75000/95000) | 1 (area) | 1 row, **inactive** (079 COD fixture) | none |
| `in_person` | 1 active (area, 0 IRR) | 1 (area) | none | none |
| `snapp_courier` | none | none | none | none |

Every task-owned 079 fixture (two `store_courier` tiers + one COD packaging policy) is **inactive**,
and the pre-existing area-level rules answer again. The 068/069 legacy parity fixture and all 071–078
records are preserved. No test policy was activated by this task.

**Gates restored OFF**: `Tooba:ShippingRateAuthority` and `Tooba:ShippingOperationalShadow` both run
on defaults `false/false`; the Development host is running the repository build with no override.

## F. Rollback and exact SHA references

- Base (verified before any write): `247f784c6a4f6d4f0cdd564467c64937be22abfd`.
- 079 code: `91951b875bca970567f3a8196a6e5c89ee20708b`; 079 evidence: `247f784c6a4f6d4f0cdd564467c64937be22abfd`.
- 080: documentation/evidence only — **no source code changed in this task** (no severe defect was
  demonstrated, so no speculative edit was made).
- Rollback for the observation seam is a **flag**: leaving both gates false restores today's exact
  behaviour; no migration, no schema change, no config file was introduced by 079 or 080.
- The only runtime action available is stopping/starting the Development host; no data migration was
  applied and none needs reverting.

## G. Final score

- Task budget: **10 / 10 consumed** (071–080).
- **Technical program closure: PASS** for the bounded scope delivered (scheduling, rates/tiers,
  packaging + payer mode, operational quote, admin surfaces, read-only storefront observation).
- **Commercial completion: NOT 100 %.** Checkout cutover, packed-measurement provenance, COD
  settlement and capacity enforcement remain open, each with a named owner.
- **Checkout cutover verdict: NO** — technical observation does not make customer pricing
  commercially ready.

## H. Next steps (no next Bridge task is generated)

1. Architect review of this record and an explicit decision on the four open owners.
2. A separately authorized program for packed measurement provenance (Fulfillment + Order).
3. A separately authorized program for COD settlement ownership (Payment + Settlement).
4. An explicit capacity decision (enforce atomically, or formally sell no guaranteed slots).
5. Merchant tariff/coverage approval session against real destinations and services.
6. Only then: a human-approved pricing cutover with a rollback flag.

`automaticNextImplementationTask: NONE`
