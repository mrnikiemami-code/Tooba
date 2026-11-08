# RECEIPTS — TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R6

Human-readable exact receipts for the R6 correction slice. Times are local `+03:30`.
Canonical target: `D:\Users\User\source\repos\SarvNewVer`, FE `http://127.0.0.1:3000` → Host
`http://127.0.0.1:5088`, SingleStore / `store-alpha` / `tooba_alpha`.

## 0. Envelope SHAs

| Field | SHA |
| --- | --- |
| Expected base | `567a531b0c27be281405ffe9da523c06c5f31f8d` |
| Base | `567a531b0c27be281405ffe9da523c06c5f31f8d` |
| Code commit | `e9b135df9abec1543c2aa0aa6fab9552513c01cc` |
| Evidence commit | `0a61f9fe17add3df4394bfb27d8205a2ad120c86` |

## 1. Source inventory

Modified (frontend only, no backend change):

```text
src/frontend/app/admin/operational-group-occurrence-store.ts
src/frontend/app/admin/admin-order-operational-delivery-section.tsx
src/frontend/app/admin/admin-order-items-shipping-panel.tsx
src/frontend/app/admin/admin-order-operational-delivery.mount.test.ts
```

Docs:

```text
docs/ai/tasks/TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R6/TASK.md
docs/architecture/evidence/TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R5/ARCHITECT-REVIEW.md
docs/architecture/evidence/TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R6/{TASK.md,RESULT.md,RECEIPTS.md}
AGENTS.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md
```

No migration, no schema change, no business-data change.

## 2. Removed mechanism

Deleted from `operational-group-occurrence-store.ts`:

```text
OPERATIONAL_GROUP_NO_CLAIM_REFUSAL_CODES
isOperationalGroupNoClaimRefusal(...)
releaseOperationalGroupAttemptAfterNoClaimRefusal(...)
```

Deleted from `admin-order-operational-delivery-section.tsx`:

```text
outcomeNoClaimRefusalCode(...)          (and its two call sites)
{ completed: false; noClaimRefusalCode?: string | null } outcome field
```

Every refusal / 5xx / transport loss now flows through `markOperationalGroupAttemptFailed(...)` with the
recorded attempt, so the occurrence, members, selections, reuse ids and fingerprint stay byte-identical.
`clearOperationalGroupAttempt(...)` is called only from the two authoritative-completion branches.

## 3. Focused frontend evidence (18/18)

```text
cd src/frontend
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
```

```text
✔ static render: Marketplace group shows both sellers, items, parcel and group action (18.7004ms)
✔ mounted: reviewed quantities produce exactly one group request across two sellers (67.5215ms)
✔ mounted: same-tick double click performs exactly one mutation (30.5420ms)
✔ mounted: retry keeps the prebound occurrence; a completed run mints a new one (60.9476ms)
✔ mounted: remount after a lost response reuses the same prebound occurrence (60.1707ms)
✔ mounted: known refusal is inline and reviewed quantities are retained (30.5315ms)
✔ mounted: lost response surfaces inline error, remount reconciles recorded parcels (56.9482ms)
✔ mounted: actor without manage permission sees no actionable create (5.7376ms)
✔ static render: seller-owned groups stay independent and offer no group workflow (1.4463ms)
✔ panel source uses synchronous single-flight, try/finally and inline error (0.6739ms)
✔ R4 mounted: the COMPLETE prebound envelope is persisted before the first POST (28.3874ms)
✔ R4/R5 mounted: retrying the SAME envelope reconciles it; a CHANGED envelope is blocked while unresolved (29.6767ms)
✔ R4 mounted: an undefined outcome (cancellation) keeps the exact envelope (4.4712ms)
✔ R4 store: an occurrence recorded by one viewer scope is never inherited by another (0.2088ms)
✔ R4 mounted: partial server progress + lost response keeps the envelope even when create is no longer ready (25.9758ms)
✔ R5 mounted: a viewer switch cannot act on the previous viewer's retained refs (29.4606ms)
✔ R6 state: a refused operation keeps the exact envelope and blocks a changed review (8.5353ms)
✔ R6 mounted: denial/compatibility/foreign refusals and transport loss keep the same occurrence (29.5006ms)
tests 18 / pass 18 / fail 0
```

### 3.1 `R6 state` (mounted section)

1. First operation returns `{completed:false}` (a denial) → attempt retained, reconcile card offered.
2. Quantity edited 2 → 1 → `admin-order-operational-retained-block-*` appears, the normal Create button is
   `disabled`, a click sends **no** request, and the stored occurrence / members / selections / fingerprint
   are unchanged.
3. `reconcile-retry` sends the **recorded** envelope (ol1 quantity 2, not the edited 1) under the same
   occurrence id; a further refusal keeps it.
4. `{completed:true}` clears it; the next deliberate action mints a **different** occurrence.

### 3.2 `R6 mounted` (real panel + fetch)

Five sequential attempts on one retained envelope:

```text
403 { error.code: order.operation.denied }                                  → retained
409 { error.code: order.operational_group.consolidate_incompatible }        → retained
409 { error.code: order.operational_group.workflow_group_foreign }          → retained
500 { error.code: order.operation.failed }                                  → retained
throw new TypeError("network down")                                         → retained
```

All five POST bodies carry the **same** `operationalGroupOccurrenceId`; the stored envelope still has
`members = [g1, g2]`, `selections = [{ol1, 2}, {ol2, 1}]`, and the reconcile card remains available after
every outcome.

## 4. Typecheck / production build

```text
npx tsc --noEmit -p tsconfig.json   → exit 0
npm run build                       → exit 0
.next/BUILD_ID                      → vr8IyL4HVfMU_ykXjaSyV   (previous PJOIKkpv60txqyyb_oqfI)
```

## 5. Canonical deployment (frontend only)

| Step | Result |
| --- | --- |
| Host publish | **not run** — no backend source changed |
| Host process | PID **17344** unchanged, `http://127.0.0.1:5088/health` **200** |
| Stop FE PID 7960 → `next start -p 3000 -H 127.0.0.1` | new PID **33744**, `/fa` **200**, `Ready in 563ms` |
| Migration | `20261108153000_AddOperationalGroupOccurrences` unchanged; **no new migration** |
| DataProtection | untouched |
| Checked-in gates | unchanged OFF; local CapturedPreview ON (`AvailableDateCount=7`) preserved |

### 5.1 Runtime parity

```text
Select-String .next/static/**/*.js "releaseOperationalGroupAttemptAfterNoClaimRefusal"  → OLD_SYMBOL_ABSENT
Select-String .next/static/**/*.js "نگه داشته شده است"                                   → chunk 2421-5ede00aa88595269.js
GET http://127.0.0.1:3000/_next/static/chunks/2421-5ede00aa88595269.js                 → 200, 217027 bytes
  contains new retained copy  → True
  contains removed symbol     → False
```

## 6. Canonical read-only checks

```text
GET http://127.0.0.1:5088/health                                              → 200
GET http://127.0.0.1:3000/fa                                                  → 200
GET http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf    → 200 (19230 bytes)
```

No order/cart/pack/create/dispatch/cancel/payment/merchant write, no business SQL, no reset/reseed,
no manual DDL, no user-tab automation.

## 7. Time and cleanup

| Event | Local time |
| --- | --- |
| Slice start (wake after R5 result) | 18:00 |
| Source repair (store / section / panel / tests) | 18:01 – 18:08 |
| Typecheck | 18:08 |
| Mount tests 18/18 | 18:09 |
| Production build | 18:10 – 18:11 |
| FE restart + parity + read-only checks | 18:12 |
| Docs / commit / push / bridge | 18:13 – 18:20 |

No disposable database was provisioned in this slice (frontend-only removal, as the task instructed).
