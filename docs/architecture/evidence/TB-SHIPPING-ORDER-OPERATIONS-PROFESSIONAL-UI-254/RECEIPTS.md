# RECEIPTS — TB-SHIPPING-ORDER-OPERATIONS-PROFESSIONAL-UI-254

Point-in-time observations from this worker execution. Nothing here is an Architect, Visual or PSP acceptance.

## Source base

- Base SHA: `8469a82f472f1902ae215c640761a98dc0aab4cc` (`docs(shipping): 253-R6 worker result contract + envelope SHAs`)
- Working tree before work: clean except the four files below (all created/modified by this task).

## Source delta

| File | Change | LOC |
| --- | --- | --- |
| `src/frontend/app/admin/admin-operational-group-workflow-dialog.tsx` | **new** — the one dedicated Marketplace group workflow dialog | 398 |
| `src/frontend/app/admin/admin-order-operational-delivery-section.tsx` | display-only card + one workflow entry point | 775 |
| `src/frontend/app/admin/admin-order-items-shipping-panel.tsx` | orphan-only seller aside; single parcel/action owner wiring | 985 |
| `src/frontend/app/admin/admin-order-operational-delivery.mount.test.ts` | rewritten + TB-254 contracts | 1375 |

`git diff --stat` (tracked): 3 files changed, 1073 insertions(+), 634 deletions(-); plus the untracked dialog (18,886 bytes).

No backend, contracts, DI, owner, quote, calendar, price, schema, migration or business-data change. No SQL was run.

## Focused tests

Command (from `src/frontend`):

```
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
```

Result: **22 / 22 pass**, 0 fail, 4.05 s. Cases:

```
TB-254 static render: the group card is display-only and owns no inline selection control
TB-254 mounted: the operational section has no selection control outside the dialog
TB-254 mounted: a group-covered parcel has exactly ONE owner; an orphan parcel stays in the seller list
TB-254 mounted: a Store-owned group with existing parcels has no selection control and one tracking action
mounted: reviewed quantities produce exactly one group request across two sellers
mounted: same-tick double submit performs exactly one mutation
mounted: retry keeps the prebound occurrence; a completed run mints a new one
mounted: remount after a lost response reuses the same prebound occurrence
mounted: known refusal is shown inside the dialog and the reviewed quantities are retained
mounted: lost response surfaces an inline error; remount offers no second allocation
mounted: actor without manage permission sees no actionable create
static render: seller-owned groups stay independent and offer no group workflow
panel source uses synchronous single-flight, try/finally and inline error
R4 mounted: the COMPLETE prebound envelope is persisted before the first POST
R4/R5 mounted: retrying the SAME envelope reconciles it; a CHANGED review is blocked while unresolved
R4 mounted: closing and reopening the dialog never cancels or re-mints the recorded operation
R4 mounted: an undefined outcome (cancellation) keeps the exact envelope
R4 store: an occurrence recorded by one viewer scope is never inherited by another
R4 mounted: partial server progress + lost response keeps the envelope even when create is no longer ready
R5 mounted: a viewer switch cannot act on the previous viewer's refs
R6 state: a refused operation keeps the exact envelope and blocks a changed review
R6 mounted: denial/compatibility/foreign refusals and transport loss keep the same occurrence
```

## Typecheck / build

- `npx tsc --noEmit -p tsconfig.json` → exit 0, no diagnostics.
- `next build` → success, Build ID **`o7FdiG_1QEjT376HubPBj`**.

## Canonical runtime (point-in-time)

- FE `http://127.0.0.1:3000` PID **30816**, `-p 3000 -H 127.0.0.1`, `TOOBA_HOST_ORIGIN`/`NEXT_PUBLIC_API_BASE_URL` = `http://127.0.0.1:5088`.
- `.next/BUILD_ID` = `o7FdiG_1QEjT376HubPBj`; process replaced the previous FE PID 33744.
- `GET http://127.0.0.1:3000/fa` → 200 (Ready in 516 ms).
- `GET http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf` → 200 (shell; the operational section is client-rendered after the authenticated host fetch).
- Served chunk `GET /_next/static/chunks/4205-7d458fa77c328377.js` → 200, 230,663 bytes, contains
  `admin-order-operational-dialog-` ×6, `admin-order-operational-prepare-` ×1, `admin-order-operational-qty-` ×1,
  `admin-order-operational-reuse-parcel-` ×1 — i.e. the delivered build carries the dialog surface and a single
  preparation entry point, not inline inputs.
- Host 5088 was not restarted or rebuilt; no backend change in this task.

## Architecture guards (pre-existing RED — disclosed, not re-baselined)

- `frontend-source-size.guard.test.ts` → RED, **18** entries. All are pre-existing; the only file this task
  touched inside that set is the already-oversized `admin-order-operational-delivery.mount.test.ts`
  (1061 LOC at base → 1375 LOC). No baseline file was edited.
- `frontend-flat-folder.guard.test.ts` → RED. At base the guard already reported the unbaselined 253 operational
  family; this task adds one more file to that same family
  (`admin-operational-group-workflow-dialog.tsx`), so the entry list is 12 instead of 11. The allowlist JSON was
  **not** edited.
- `frontend-import-boundary`, `frontend-feature-boundary`, `frontend-test-discovery` → unaffected by this slice.
