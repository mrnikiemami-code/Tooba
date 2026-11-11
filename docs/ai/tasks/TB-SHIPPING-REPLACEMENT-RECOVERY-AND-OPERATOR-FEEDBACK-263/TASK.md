PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-REPLACEMENT-RECOVERY-AND-OPERATOR-FEEDBACK-263
Parent-Task: TB-SHIPPING-DAMAGED-ORIGIN-RECEIPT-RESOLUTION-AND-REPLACEMENT-262-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: REPRODUCE_REPLACEMENT_USER_FAILURE_AND_DISTINGUISH_KNOWN_PENDING_FROM_UNKNOWN_WITH_EXISTING_TOAST_MESSAGEBOX
Expected-Base-SHA: 7117d6d4977b14f0a37c4eda1c4950967bb29a44
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. Outcome and boundaries
The user trial REOPENS functional acceptance. Repair the actual stuck damaged-replacement path and professional feedback on the existing shared operational page. This is the ONLY issued implementation task; calendar readiness and Returns integration are recorded in SHIPPING-USER-FLOW-CLOSURE-263-265.md, not implemented here.
Read CURRENT AGENTS/START-HERE/recovery, the 262-R1 review, repository Contracts/architecture/design-system rules. Preserve accepted lifecycle/quantity/reservation identities. One worker, one task, no agent delegation.
Expected base may acquire this task's issue/recovery documentation descendants; verify legitimate ancestry, never discard work. No force/reset/amend/stash/rebase.

## 2. Reproduce before changing
User screenshot: /admin/orders/13feece0-4890-4e45-b92f-4d4b1a5e0c97/operations
fulfillment 3ce14fb8-6453-44c0-b364-0f919c8bf2e0, returned damaged source a44aa20e-ecf3-4964-b828-bf1fc4a72eac.
Dialog says response unknown + generic red "inspect server error...do not delete this window" + replay-only button. No meaningful actionable cause. Inspect these screenshot/user records READ-ONLY, current detail/operations + permitted logs/network; do not click mutation on user records.
Reproduce the same UI failure using a MINIMAL labelled task-owned production-path fixture on SAME tooba_alpha. Capture actual HTTP status/typed error/recorded resolution state and whether parcel/stock/hold exists. Do not assume screenshot proves a backend failure or no stock; establish cause.
Source-confirmed seam: post-dispatch-exception-dialog.tsx Props.onSubmit returns boolean; send() sends every false result into markPostDispatchAttemptFailed, and retained renders all as uncertain. Distinguish server-known pending/refusal from an actually unknown result; don't merely replace warning text.

## 3. Functional correction
- Completed command: authoritative refreshed owning facts; exactly one success toast; close existing operation dialog, clear only completed retained attempt, update parcel/line/capability once. Show actual linked replacement, never completed success without parcel.
- Known pending (e.g. healthy stock unavailable): concise Persian actionable reason; durable decision remains pending, same source/quantity/outcome/occurrence can continue when stock arrives. Do not label an acknowledged no-stock decision as a lost response, do not clear it or mint another decision/hold.
- Known validation/permission/conflict refusal: exact localized operator reason through existing error mapper; field errors adjacent to field. Do not classify every 4xx as safe to abandon: conflict or existing server decision must reconcile owning facts first. No remint or user-data reset shortcut.
- Actual timeout/network/unknown outcome: preserve complete durable envelope. Existing contextual "پیگیری درخواست" and one deliberate reconciliation of SAME exact intent; reload/server pending facts still work, no auto POST loop. No instructions to inspect server, technical diagnostics or "never close this window". Closing must be safe; reopening restores progress.
- A recorded incomplete replacement resumes through existing public command (262-R1). Fix any reproduced remaining orchestration/adapter/refresh cause at its owning seam; no lifecycle rewrite, auth weakening, package unlock or fake success. Supplied healthy stock permits same decision to produce one linked parcel; cancel/retry reuses retained hold.

## 4. UI feedback — ONE established app pattern
Discover and reuse CURRENT app Toast (react-toastify) and existing MessageBox API/components. No new library/provider/dialog framework/browser alert/confirm.
Toast: transient success and ordinary nonblocking operation notifications, once per confirmed action, not on GET/reload/re-render.
Existing MessageBox: blocking operation errors/decisions requiring explicit acknowledgement/confirmation, at most one visible feedback surface at a time; no stacked MessageBox over an open review modal. Follow current shared app pattern to coordinate surfaces.
Inline: field validation and compact persistent BUSINESS state only (e.g. awaiting healthy stock / durable pending request), not a lingering duplicate success/error notice.
Keep reviewed business form in the existing operation dialog. Remove screenshot's verbose technical amber+red duplicate messaging and generic unknown-error instructions. Pending, rejected and unknown are different semantic outcomes, communicated concisely and truthfully.
Retain RTL, Persian labels, 44px key actions, one action owner, no finance surfaces added. No raw enums/errors/identifiers in operator messages.

## 5. Binding business rules
Healthy returned source => resend SAME usable goods with no second sale reservation.
Damaged source => replacement uses NEW healthy-stock reservation attached to same paid order; damaged stock never becomes usable/saleable, original consumed reservation untouched.
Unconsumed active-order hold survives parcel cancel/return/retry/timeout; dispatch consumes once.
Latest policy for PLANNED Returns integration: creating a Returns case alone doesn't release stock. Owning approved return + cancellation of the corresponding order-line obligation may release ONLY its unconsumed reservation once and block resend/replacement for that quantity; consumed sale stock uses proper goods disposition, never "release". This future linkage is NOT authorized to implement in 263.
No auto refund, no product substitution, no original quantity inflation, no parallel money workflow. Referral remains pending, not actual Returns integration.

## 6. Focused proof, no loop
One canonical tooba_alpha @5432, FE3000 -> Host5088, SingleStore/store-alpha ONLY; no extra/disposable/Docker DB, 15433, edition/connection/port switch. User/screenshot rows read-only. Task-labelled minimal owning-path fixtures allowed, record IDs, no manual business SQL/DDL/reset/reseed/backfill/delete.
Never invoke global expiry/reclamation job with a future clock on shared DB; use scoped/read-only proof.
A. Actual public-command path: no stock -> known pending; close/reload -> same recorded decision; healthy stock supplied to fixture through owning path -> same occurrence completes with one hold/parcel. Existing hold cancel/retry retains exact hold, no new reservation.
B. Mounted REAL panel/dialog: success exactly one Toast + close/refresh; typed no-stock isn't unknown; field refusal is local; blocking error uses EXISTING MessageBox without duplicate surfaces; network lost response retains same payload and safe reopen/reconcile; single-flight.
Run focused affected checks once, frontend typecheck/build and relevant backend build/publish only if changed. No full-suite/base parity/repeated green runs/stress/screenshots. Optional read-only browser inspection, not screenshot deliverable.
If cause depends on missing provider/data outside scope, report precise blocker; don't expand into 264/265 or fabricate a fix.

## 7. Handoff
Persist TASK verbatim docs/ai/tasks/TB-SHIPPING-REPLACEMENT-RECOVERY-AND-OPERATOR-FEEDBACK-263/TASK.md and evidence TB-SHIPPING-REPLACEMENT-RECOVERY-AND-OPERATOR-FEEDBACK-263/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt}.
Deploy relevant changed app to same canonical ports, preserve DataProtection/CapturedPreview localON7dates checked-inOFF;255 parked/Tipax deferred. No provider call/fake waybill.
Update recovery/plan/TOPs with 263 result pending Architect review, 264 and265 NOT ISSUED. Result must include Base/Code/Evidence/Merge SHA, HEAD==origin/main, root cause with actual response, existing MessageBox component reused, focused case outcomes, fixture IDs, runtime/build and user trial URL/steps. Clearly separate independent execution proof from mounted proof.
One result then STOP_FOR_ARCHITECT; don't issue/execute next task automatically or ask user to debug server.
automaticNextImplementationTask: NONE
END_TOOBA_TASK
