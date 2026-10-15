# Recovery handoff — Shipping 071–080 chronology (ready to merge)

**Why this is a handoff and not a master edit.** Mission C.9 authorized an append-only edit **only**
to a verified canonical master at the allowed path `docs/planning/RECOVERY.md`. That file does **not**
exist in this repository (`Test-Path docs/planning/RECOVERY.md` → `False`). The actual canonical
master is `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`, which is **outside** the task's allowed
paths, so it was **not** modified. This document is the exact text to append (or adapt) there by the
file's owner, respecting its existing style. No unrelated planning decision was overwritten.

## Suggested append target

`docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`, immediately after the existing
`## Shipping MVP — Recovery checkpoint 2026-10-14 (authoritative for Shipping, tasks 034–058)`
section, as a **new, later, superseding** Shipping checkpoint.

## Suggested heading and body

```markdown
## Shipping Full-Operations — Recovery checkpoint 2026-10-15 (authoritative for Shipping, tasks 071–080)

**CURRENT RULING: TECHNICAL PROGRAM CLOSED / NOT_READY_FOR_CHECKOUT_CUTOVER.** Last task of the
10-task program: `TB-SHIPPING-FULL-OPERATIONS-080` (final acceptance + recovery record).
Base SHA `247f784c6a4f6d4f0cdd564467c64937be22abfd`. Last Shipping production-code commit before it:
`91951b875bca970567f3a8196a6e5c89ee20708b` (079). 080 changed **documentation/evidence only**.
Verify current HEAD/origin/main before issuing any further task. This checkpoint supersedes the
2026-10-14 Shipping MVP checkpoint above, and only for Shipping; unrelated TMAR certifications stand.
Shipping and Geography remain **not** ARCH-COMPLETE-002 certified.

**Delivery lineage 071–080 (all on channel `tooba-main`, worker `tooba-worker-01`):**
- 071: Shipping-owned weekly operating schedule (schema, aggregate, admin read/create/update/
  deactivate API, module migration, focused tests).
- 072: existing `/admin/shipping-services` weekly schedule editor dialog (action «برنامه کاری ارسال»).
- 073: weight / item-count pricing foundation — half-open `[min, max)` tiers on both axes, integer
  grams on purpose, fail-closed when a measurement is absent.
- 074: tier-condition editor inside the existing rates/coverage dialog (weight in kg converted
  exactly to grams; count in items). Packaging foundation deferred, not faked.
- 075: packaging fee policy as a second, independent Shipping aggregate + persisted freight payer
  mode (`Prepaid` / `CollectOnDelivery`).
- 076: read-only deterministic operational quote seam composing freight + packaging + payer split +
  carrier schedule + honest capacity; explicit `ShippingCapacityEnforcement.NotEnforced` only.
- 077: admin packaging fee / freight payer editor dialog (action «هزینه بسته‌بندی و پرداخت کرایه»).
- 078: admin operational-quote inspector dialog (action «بررسی هزینه و شرایط ارسال») with explicit
  timezone handling, nullable money never rendered as zero, and the capacity disclosure.
- 079: read-only, fail-isolated, **separately gated** storefront operational shadow
  (`Tooba:ShippingOperationalShadow`, default OFF; the earlier `Tooba:ShippingRateAuthority` gate
  unchanged). Byte-identical legacy storefront response OFF/ON. Live Development proof of a charged
  packaging + COD split and the 1999g/2000g half-open tier boundary. Explicit decision: capacity
  remains **configuration only**; atomic capacity is **not** implemented.
- 080: final acceptance matrix, capability-by-capability verdicts, Development fixture inventory,
  OFF-gate verification, rollback notes and the truthful **NO CUTOVER** verdict.

**Still open (each with an owner; no assumed PASS):**
1. Packed weight / physical item-count provenance from a real packed shipment — `Tooba.Fulfillment.
   Contracts` exposes no measured weight and no packed-unit count, so a conditioned rate fails
   closed on the real storefront path. Owner: Fulfillment + Order; needs a separately authorized
   program. Do not derive from cart lines, quantities or arbitrary client input.
2. Atomic capacity booking (order↔window relation, transactional bound, idempotent claim/release,
   expiration/cancellation, ledger owner). Owner: Order + Shipping + Settlement. NOT READY.
3. COD settlement: the payer split is a rate fact only; no Payment/Settlement transaction exists.
   Owner: Payment + Settlement.
4. Merchant-approved commercial tariffs, packaging fees and destination coverage. Synthetic Alborz
   fixtures and the deactivated 079 tier/packaging rows are **not** merchant approved.
5. Checkout pricing cutover itself — no switch exists and none is authorized. Owner: Architect +
   Merchant. `StorefrontShippingService` / `ShippingMethodsOptions` / `StorefrontCheckoutService`
   remain the legacy price and paid-snapshot authority.
6. `snapp_courier` is configured as a Fulfillment service with no usable rate/coverage/schedule row;
   its catalog row was **not** verified in 080 (reported unverified, not fixed).

**Gates:** `Tooba:ShippingRateAuthority` and `Tooba:ShippingOperationalShadow` are both OFF by default
(no such section exists in any appsettings); both seams are observation-only and neither mutates a
price, promise, draft or paid snapshot. Rollback for the observation seam is a flag.

**Evidence:** `docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-079/` and
`docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-080/`; planning records
`docs/planning/shipping/SHIPPING-FULL-OPERATIONS-071.md` … `-080.md`.
`automaticNextImplementationTask = NONE`.
```

## Note for the merger

If the master's owner prefers a shorter form, keep at minimum: the new checkpoint date, the
`TECHNICAL PROGRAM CLOSED / NOT_READY_FOR_CHECKOUT_CUTOVER` ruling, the 071–080 one-line lineage, the
six open items with owners, the two OFF gates, and the evidence/planning paths.
