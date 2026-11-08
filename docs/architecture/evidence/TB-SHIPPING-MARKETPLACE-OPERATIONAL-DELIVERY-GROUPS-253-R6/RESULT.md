PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R6
Parent-Task: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R5
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Status: PASS
Decision: ERROR_CODE_NO_CLAIM_RELEASE_REMOVED_RETAINED_OCCURRENCE_KEPT_UNTIL_AUTHORITATIVE_RESOLUTION
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 567a531b0c27be281405ffe9da523c06c5f31f8d
Base-SHA: 567a531b0c27be281405ffe9da523c06c5f31f8d
Code-SHA: stamped in the R6 result envelope
Evidence-SHA: stamped in the R6 result envelope
automaticNextImplementationTask: NONE

# RESULT — TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R6

Status: **PASS within the issued R6 correction/deploy scope**
Not Architect ACCEPT. Not Visual ACCEPT. Not live-provider acceptance.

## 1. Scope executed

R5's accepted work was not redone: changed-review remint blocking and the pre-remaining-quantity recorded
parcel reconciliation are untouched. Only the rejected mechanism was removed.

## 2. The no-claim release mechanism is gone

`operational-group-occurrence-store.ts` no longer exports `OPERATIONAL_GROUP_NO_CLAIM_REFUSAL_CODES`,
`isOperationalGroupNoClaimRefusal` or `releaseOperationalGroupAttemptAfterNoClaimRefusal`. The file now
documents why a bare code cannot prove absence of durable progress.

`admin-order-operational-delivery-section.tsx`:

- the `noClaimRefusalCode` outcome field and `outcomeNoClaimRefusalCode` helper are removed;
- both call sites (normal action and reconcile retry) no longer branch on any code;
- every non-completed outcome — `order.operation.denied`, `consolidate_incompatible`,
  `consolidate_requires_multi_seller`, `workflow_group_foreign`, `workflow_occurrence_required`, any other
  typed refusal, 5xx, timeout, transport loss, `undefined` — runs
  `markOperationalGroupAttemptFailed({ attempt })` and keeps the exact recorded occurrence/members/
  selections/reuse ids/fingerprint;
- `clearOperationalGroupAttempt(...)` is reached only from the two authoritative-completion branches;
- `admin-order-items-shipping-panel.tsx` returns `{ completed: false }` without forwarding `result.code`.

No reset/delete shortcut, no automatic new occurrence, no replacement request, no envelope loss, no new
status framework, no new panel and no raw diagnostics were added. The single retained notice now reads
«درخواست ثبت‌شدهٔ این گروه نگه داشته شده است؛ پیش از همگام‌سازی همان عملیات، درخواست جدیدی شروع نمی‌شود.»
and the existing localized workflow error still explains the actionable refusal.

## 3. Focused proof

`npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts`
→ **18/18 pass** (R2/R3/R4/R5 regressions included), `npx tsc --noEmit` exit **0**.

- **R6 state** (mounted section): refused attempt retained → quantity edited 2→1 → retained-block notice,
  normal Create button `disabled`, click sends no request, stored occurrence/members/selections/fingerprint
  unchanged → `reconcile-retry` sends the RECORDED envelope (ol1 = 2, not the edited 1) under the same
  occurrence → refusal keeps it → `{completed:true}` clears it → the next action mints a different
  occurrence.
- **R6 mounted** (real panel + fetch): five sequential attempts — `403 order.operation.denied`,
  `409 consolidate_incompatible`, `409 workflow_group_foreign`, `500`, and `TypeError network down` — all
  replay the SAME `operationalGroupOccurrenceId`, and the stored envelope stays `[g1,g2]` /
  `[{ol1,2},{ol2,1}]` with the reconcile action still offered.
- No backend no-claim proof was implemented (the task preferred removal), so no disposable PostgreSQL run
  was needed.

## 4. Canonical deployment

| Item | Value |
| --- | --- |
| Host publish | not run — no backend change |
| Host process | PID **17344**, `/health` **200** |
| Frontend build | `npm run build` exit **0**, `BUILD_ID` `vr8IyL4HVfMU_ykXjaSyV` |
| Frontend process | PID **33744**, `/fa` **200**, `Ready in 563ms` |
| Migration | `20261108153000_AddOperationalGroupOccurrences` unchanged; no new migration |
| DataProtection | untouched |
| Read-only admin order | `01a1b92d-e97e-7000-999a-143350d0edaf` → **200** |

Runtime parity: the served chunk `2421-5ede00aa88595269.js` contains the new retained copy and does **not**
contain `releaseOperationalGroupAttemptAfterNoClaimRefusal`.

No order/cart/pack/create/dispatch/cancel/payment/merchant write, no business SQL, no reset/reseed, no manual
DDL, no user-tab automation, no alternate DB or runtime.

## 5. Honest limitations

- The UI cannot transition back to a correctable review after a refusal; the retained occurrence must be
  reconciled or resolved. This is the conservative direction the task required.
- A structured server no-claim proof was not built; if it is ever added, the absent-record vs
  existing-record distinction must be proven before any client release is reintroduced.
- Tipax/live-provider acceptance remains deferred; Store/Seller ownership still fails closed.
- No screenshots and no visual-acceptance claim.
- TB185 remains paused/default-OFF/PendingAllocation; retained known issues (238 unknown 22001, 227 mobile
  empty-note, 225-R1 lost local edits) are unchanged.

## 6. State

`STOP_FOR_ARCHITECT`
`automaticNextImplementationTask: NONE`
END_TOOBA_WORKER_RESULT
