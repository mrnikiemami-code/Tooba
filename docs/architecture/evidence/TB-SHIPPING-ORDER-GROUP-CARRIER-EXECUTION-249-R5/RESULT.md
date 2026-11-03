# RESULT — TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R5

Status: **PASS** within issued Phase A + Phase B + disposable relational + canonical deploy scope  
Decision: **DISPATCH_CLAIM_VALUE_AND_PRODUCT_PHYSICAL_SPEC_DEPLOYED_PROVIDER_UNVERIFIED**  
NOT Architect ACCEPT, NOT live Tipax ACCEPT, NOT Visual ACCEPT

Parent: `TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R4`  
Base-SHA: `a9af3f400e968113c5f09a7edee1c70f2a51f1a6`  
Code-SHA: `23f6dbd7a495159510aa3a9eede28b57b3bb4ae6`  
Evidence-SHA: `EVIDENCE_SHA_PENDING`  
Stop-State: `STOP_FOR_ARCHITECT`  
automaticNextImplementationTask: `NONE`

## Phase A — durable dispatch (R4 review defects closed)

1. **Stable intent** — `operation_key=c:{checkout:N}:{group:N}:{fp16}` from owner+group+sorted lines+weight/dims (money excluded); deterministic PackageDispatchId; client locator validated; same intent converges to one row; different payload → intent_conflict.
2. **Exclusive claim** — DispatchClaimToken + lease via atomic Npgsql update; at most one provider invocation per intent; losers see Pending without HTTP.
3. **External attempt mark** — `ExternalDispatchAttempted` persisted BEFORE provider HTTP; crash/cancel → Unknown; resume never blind-resends (lease expiry ≠ safe resend).
4. **Allocation idempotency** — Fulfillment `shipping_dispatch_key=operation_key`; resume reuses exact recorded shipment after link-write failure.
5. **Snapshot binding** — resume refuses account/config/provider drift (no silent rotation/substitution).
6. **Value/collect** — Declared = Σ(unitPrice×qty) from selected lines (capped); collect by declared share with currency scale + final residual; mixed 100/900 proven; totals cannot exceed accepted.
7. **Per-dispatch package** — partial packages require own Package weight/dims; not reuse of full-group completion.

## Phase B — product physical specification

Catalog-owned WeightGrams + Length/Width/HeightCentimeters on product (+ variant null=inherit). Typed↔legacy weight compatibility (agree / typed-only / legacy-only / disagree fail-closed). Dims typed-only (method defaults do not masquerade). Null = unspecified. Editor «مشخصات فیزیکی». `GetPhysicalSpecificationsByVariantIdsAsync`. Migration additive nullable, no backfill. Dimensions of different products are not summed.

## Disposable PostgreSQL TB249R5 (EXECUTED)

`tb249r5-pg` `127.0.0.1:15433` db/user `shipping_tb249r5`/`tb249r5`. Migrated Order+Shipping+Fulfillment+Catalog. Category TB249R5 **15/15** (barrier concurrency providerCreates=1; post-send no-resend; alloc-before-link reuse; 100/900 residuals; rotation refuse; Catalog g/cm; Order immutable). Container cleaned; port closed.

## Canonical

Private dump backup then applied Catalog/Shipping/Fulfillment `20261103120000_*`. FE3000 PID **32348** → Host5088 PID **26352** SingleStore/store-alpha/tooba_alpha. DataProtection preserved. Authorized Host Bearer: order Manual PendingPayment; capabilities Manual/`payment_locked`/`canCreate=false`. BFF product workspace physical columns present (null). Focused Shipping 49/49; Order physical+eligibility 12/12; FE 11/11; tsc 0.

## Unmet

- Live Tipax deferred
- Architect/Visual ACCEPT not claimed
- Full React DOM mount of carrier tree (payload + source-wired interaction used)

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
