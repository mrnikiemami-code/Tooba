# TASK — TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R2

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

- Task-ID: TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R2
- Parent-Task: TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R1
- Channel: tooba-main
- Worker-Id: tooba-worker-01 · Agent-Type: cursor
- Repository: mrnikiemami-code/Tooba · Branch: main
- Program: Tooba Shipping — repair the user's calendar save failure end to end
- Mode: REPRODUCE_AND_REPAIR_REAL_AUTHORING_SAVE_WITH_INDEPENDENT_CALENDAR_READBACK
- Expected-Base-SHA: 9543b149e1d27210f823fafb94f80cd707b818be
- Stop-State: STOP_FOR_ARCHITECT · automaticNextImplementationTask: NONE

## 1. Latest user direction — supersedes earlier stop-after-every-fix gate

User accepted the underlined tab appearance: «اره اوکی هست فقط ذخیره نمی کنه». They now explicitly
authorize sequential continuation: fix save first, then close the FIVE remaining defects through the
planned stages; Architect receives/reviews one result before issuing the next task. Do not wait for a
separate user appearance approval after every correction. Final user handoff after the five issues are
closed, unless a genuinely required product decision/blocker needs escalation.

This does NOT authorize the Worker to self-start another task: execute ONLY 256-R2, post result,
STOP_FOR_ARCHITECT.

Current five-item ledger: tracking uniqueness; correct remaining quantity; non-delivery/return/resend;
shared-group common-or-separate parcels; due workqueue/shared operational order view. Planned 257 (first
two), 258, 259, 260 remain NOT ISSUED. All extra databases remain forbidden. Same tooba_alpha +
FE3000/Host5088 only.

## 2. Scope and root cause

Read CURRENT recovery/closure plan, 256/R1 TASK+RESULT and appropriate mandatory feature
skills/architecture/error/owner rules. The ONLY implementation scope is the user-reported inability to
save the two-calendar authoring form. UI appearance is approved; preserve real tabs, concise copy and
independent local drafts.

Do not guess whether disabled button, client validation, rejected PUT, transport uncertainty or
persisted-but-not-rehydrated state: reproduce the exact path and record the real failure stage.

Inspect the user's existing draft read-only:
`http://127.0.0.1:3000/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit`.
Do not overwrite its data or publish it without an explicit user save. Do not touch user's browser
tabs/profile or read private credentials.

For controlled SAVE proof use the EXISTING task-owned draft `01a1beed-d7df-7000-bfc8-0185ea61afe3` in
the SAME `tooba_alpha`. Supported editor API/browser SAVE and reversible edits on THIS draft are
authorized; no new DB/draft/order needed. Verify its ownership/identity before mutation. No
Publish/withdraw needed.

## 3. Required behavior — real command/query roundtrip

Customer and carrier schedules edited independently in the same document/revision. Configure/copy,
change weekday open/close/closed, switch tabs; unsaved edits remain intact. Existing Save enabled for
lawful changes, all applicable local/server validation visible in the owning tab with focus on actual
field. On success, response replaces confirmed baseline/revision; fresh GET and reload reproduce EXACT
saved calendar values and preserve unrelated names/languages/regions/rates/settings/customer slots.
Copy is one-time, not an implicit link; saving carrier hours never replaces customer hours.
Cancel/reset returns to confirmed data, not guessed defaults. Revision conflict/transport loss keeps
user edits and offers the existing explicit reconcile; do not clear edits on failure, add blind retries
or weaken revision/owner validation. One same-tick Save mutation; meaningful pending state; a single
concise human error on failure. No raw technical paragraphs, no fake success/toast-only proof. Invalid
calendar cannot be saved; show why in its own tab. Fix actual validation/wire/version/storage mismatch
rather than suppressing validation. All existing configured languages/readback, format3 carrier
snapshots and 256 MAX/cutoff/planned evidence stay coherent. Do not change delivery calculations or
reopen unrelated architecture work.

Use smallest repair in actual owning API/model/state/backend seam. No parallel save endpoint/state
machine, no feature-folder baseline exemptions, no broad rewrite.

## 4. Focused verification and true canonical delivery

Evidence MUST include actual browser Save on the task-owned draft (not intercepted request): before
revision/document → local edit → ONE PUT with actual status/body/error (redacted) → fresh GET → reload →
exact values. Bounded cases: carrier-only update; customer-only update retaining carrier; copy then
independent edit; invalid-save field targeting; reject/unknown preserves local edits. Use focused tests
for deterministic error paths; no full-module or unrelated suites. No screenshots; authorized DOM/HTTP
facts suffice. Prove the exact previously failing case now succeeds, not only independent synthetic
model tests.

Same source `D:\Users\User\source\repos\SarvNewVer`. Canonical SingleStore/store-alpha/tooba_alpha,
FE3000 → Host5088. No extra/disposable/parallel database, Docker DB, alternate runtime/port, edition
change. Do not rerun DB-dependent suites that require another database. No schema change expected. If
unavoidable, report exact reason; only necessary additive same-DB migration through existing
backup/MigrationRunner process; no manual DDL/business SQL/reset/reseed. No
checkout/payment/packing/parcel/dispatch/delivery/cancel or merchant publication mutation. User draft
stays unchanged. Preserve DataProtection, local CapturedPreview ON/7 dates, checked-in OFF; Tipax
deferred, 255 parked. Typecheck + affected tests + production build; backend publish only if backend
changed. Deploy to canonical and verify served build matches proof. No build-only or shell200
acceptance.

## 5. Recovery and result

Persist exact task + RESULT/RECEIPTS under `docs/ai/tasks/TASK-ID/` and
`docs/architecture/evidence/TASK-ID/`. Update closure plan and three TOPs: `AGENTS.md`,
`docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md`,
`docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md`. Record latest
sequential-continuation authorization and five-item ledger clearly; earlier per-correction user-wait
paragraphs are superseded, not erased. Next task only issued by Architect AFTER this result review. Do
not say planned 257 is already issued. Record exact root cause, touched files, real PUT/GET/reload
proof, revisions/task-owned draft locator, migration/deploy state and remaining blockers. Commit/push
normally, local==origin/main verified; no force/amend/reset/stash/rebase. Disclose unrelated work.
Return complete BEGIN_TOOBA_WORKER_RESULT with exact Base/Code/Evidence SHA, status
PASS/PARTIAL/BLOCKED, concise root cause, same-database save proof, review URL. If real save is not
proven, PARTIAL/BLOCKED, never PASS. Worker PASS is not Architect/user acceptance.

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
# ISSUED BRIDGE ENVELOPE (as delivered on channel tooba-main)

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R2
Parent-Task: TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — repair the user's calendar save failure end to end
Mode: REPRODUCE_AND_REPAIR_REAL_AUTHORING_SAVE_WITH_INDEPENDENT_CALENDAR_READBACK
Expected-Base-SHA: 9543b149e1d27210f823fafb94f80cd707b818be
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. Latest user direction — supersedes earlier stop-after-every-fix gate
User accepted the underlined tab appearance: «اره اوکی هست فقط ذخیره نمی کنه».
They now explicitly authorize sequential continuation: fix save first, then close the FIVE remaining defects through the planned stages; Architect receives/reviews one result before issuing the next task. Do not wait for a separate user appearance approval after every correction. Final user handoff after the five issues are closed, unless a genuinely required product decision/blocker needs escalation.
This does NOT authorize the Worker to self-start another task: execute ONLY 256-R2, post result, STOP_FOR_ARCHITECT.
Current five-item ledger: tracking uniqueness; correct remaining quantity; non-delivery/return/resend; shared-group common-or-separate parcels; due workqueue/shared operational order view. Planned 257 (first two), 258, 259, 260 remain NOT ISSUED.
All extra databases remain forbidden. Same tooba_alpha + FE3000/Host5088 only.

## 2. Scope and root cause
Read CURRENT recovery/closure plan, 256/R1 TASK+RESULT and appropriate mandatory feature skills/architecture/error/owner rules.
The ONLY implementation scope is the user-reported inability to save the two-calendar authoring form. UI appearance is approved; preserve real tabs, concise copy and independent local drafts.
Do not guess whether disabled button, client validation, rejected PUT, transport uncertainty or persisted-but-not-rehydrated state: reproduce the exact path and record the real failure stage.
Inspect the user's existing draft read-only:
http://127.0.0.1:3000/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit
Do not overwrite its data or publish it without an explicit user save. Do not touch user's browser tabs/profile or read private credentials.

For controlled SAVE proof use the EXISTING task-owned draft 01a1beed-d7df-7000-bfc8-0185ea61afe3 in the SAME tooba_alpha. Supported editor API/browser SAVE and reversible edits on THIS draft are authorized; no new DB/draft/order needed. Verify its ownership/identity before mutation. No Publish/withdraw needed.

## 3. Required behavior — real command/query roundtrip
- Customer and carrier schedules edited independently in the same document/revision.
- Configure/copy, change weekday open/close/closed, switch tabs; unsaved edits remain intact.
- Existing Save enabled for lawful changes, all applicable local/server validation visible in the owning tab with focus on actual field.
- On success, response replaces confirmed baseline/revision; fresh GET and reload reproduce EXACT saved calendar values and preserve unrelated names/languages/regions/rates/settings/customer slots.
- Copy is one-time, not an implicit link; saving carrier hours never replaces customer hours.
- Cancel/reset returns to confirmed data, not guessed defaults.
- Revision conflict/transport loss keeps user edits and offers the existing explicit reconcile; do not clear edits on failure, add blind retries or weaken revision/owner validation.
- One same-tick Save mutation; meaningful pending state; a single concise human error on failure. No raw technical paragraphs, no fake success/toast-only proof.
- Invalid calendar cannot be saved; show why in its own tab. Fix actual validation/wire/version/storage mismatch rather than suppressing validation.
- All existing configured languages/readback, format3 carrier snapshots and 256 MAX/cutoff/planned evidence stay coherent. Do not change delivery calculations or reopen unrelated architecture work.

Use smallest repair in actual owning API/model/state/backend seam. No parallel save endpoint/state machine, no feature-folder baseline exemptions, no broad rewrite.

## 4. Focused verification and true canonical delivery
Evidence MUST include actual browser Save on the task-owned draft (not intercepted request):
before revision/document -> local edit -> ONE PUT with actual status/body/error (redacted) -> fresh GET -> reload -> exact values.
Bounded cases: carrier-only update; customer-only update retaining carrier; copy then independent edit; invalid-save field targeting; reject/unknown preserves local edits. Use focused tests for deterministic error paths; no full-module or unrelated suites.
No screenshots; authorized DOM/HTTP facts suffice.
Prove the exact previously failing case now succeeds, not only independent synthetic model tests.

Same source D:\Users\User\source\repos\SarvNewVer.
Canonical SingleStore/store-alpha/tooba_alpha, FE3000 -> Host5088. No extra/disposable/parallel database, Docker DB, alternate runtime/port, edition change. Do not rerun DB-dependent suites that require another database.
No schema change expected. If unavoidable, report exact reason; only necessary additive same-DB migration through existing backup/MigrationRunner process; no manual DDL/business SQL/reset/reseed.
No checkout/payment/packing/parcel/dispatch/delivery/cancel or merchant publication mutation. User draft stays unchanged.
Preserve DataProtection, local CapturedPreview ON/7 dates, checked-in OFF; Tipax deferred, 255 parked.
Typecheck + affected tests + production build; backend publish only if backend changed. Deploy to canonical and verify served build matches proof. No build-only or shell200 acceptance.

## 5. Recovery and result
Persist exact task + RESULT/RECEIPTS under docs/ai/tasks/TASK-ID/ and docs/architecture/evidence/TASK-ID/.
Update closure plan and three TOPs:
AGENTS.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md
Record latest sequential-continuation authorization and five-item ledger clearly; earlier per-correction user-wait paragraphs are superseded, not erased. Next task only issued by Architect AFTER this result review. Do not say planned 257 is already issued.
Record exact root cause, touched files, real PUT/GET/reload proof, revisions/task-owned draft locator, migration/deploy state and remaining blockers.
Commit/push normally, local==origin/main verified; no force/amend/reset/stash/rebase. Disclose unrelated work.
Return complete BEGIN_TOOBA_WORKER_RESULT with exact Base/Code/Evidence SHA, status PASS/PARTIAL/BLOCKED, concise root cause, same-database save proof, review URL.
If real save is not proven, PARTIAL/BLOCKED, never PASS. Worker PASS is not Architect/user acceptance.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
