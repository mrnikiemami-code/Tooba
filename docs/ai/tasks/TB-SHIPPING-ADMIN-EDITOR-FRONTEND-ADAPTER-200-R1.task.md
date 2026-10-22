PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R1
Parent-Task: TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: BOUNDED_BASELINE_PRESERVATION_SESSION_EPOCH_AND_UNKNOWN_OUTCOME_REPAIR
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Expected-Base-SHA: 29cf0117eeaa087c7edc5e4cf1a0240d1a9f5e8e
Hard-Timebox-Minutes: 25
Stop-New-Implementation-And-Tests-At-Minute: 18
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Goal

Fix the concrete TB200 adapter/session defects in its ARCHITECT-REVIEW.md before any UI wiring. Existing feature ONLY, no new capabilities. Backend199 remains accepted. Follow AGENTS/recovery/frontend feature skill and architecture standard; preserve precision, onePUT, null/absence semantics, fail-closed Geography and the existing public feature boundary.

Fetch/verify HEAD==origin/main base, fast-forward only. Preserve unrelated work/stashes; no reset/rebase/stash/force-push. No task201 or another task issued here.

Required repairs / executable regressions

Write compact regression tests FIRST and run the specific new failing cases on unchanged200 implementation. Record observed behavior, not just source argument. Then repair same assertions, no weakening expected outcomes.

R200-01 — Baseline-aware full region replacement:
Current buildEditorDocumentBody uses edits.regionGroups ?? []; selectApplyRequest supplies only edits. Editing method text, schedule/settings or type on a loaded document can DELETE ALL persisted groups/rows/titles via regionGroups:[].

Make construction consume the actual confirmed baseline + intentional edits (change current unused public signature/index/tests as necessary). When edits.regionGroups is OMITTED, serialize the complete retained groups/order/areas/rows/ids from baseline; title patch lists [] and method patch [] when untouched, NEVER echo inactive text as active writes. Explicit edits.regionGroups:[] still means intentional whole collection removal. Baseline must not mutate.
Every retained selection still requires authoritative ancestry; unresolved stored selection returns needsResolution and NO body, not [] or omission.
Test baseline with at least TWO populated groups and inactive-language text: method-only/settings-only/type-clear edits preserve every retained group/row id, amounts and selection; explicit[] clears only when intended; unresolved baseline area blocks. One compact table suffices.
Enforce active-language intent using baseline registry for touched method/title edits: reject unknown/inactive intent, don't silently filter it into success. Untouched inactive text survives. Preserve all optional wrappers and exact decimal token text.

R200-02 — Never-reused async session identity:
Current A(generation1)->B->A(generation1) can accept OLD A load; save actions don't include generation and request ids reset. Introduce a local monotonically increasing session epoch (no global store) that NEVER resets on switch/discard/reopen, and carry the epoch in every load/save completion and operation. Preserve request id protection within that epoch. Don't rely on UI caller convention alone.
Tests: startA/load pending; switchB; reopenA/newload; deliver oldA success/failure => SAME current state object, no baseline overwrite. Also late oldA save success/failure with reused numeric requestId cannot clear newA edits after switch/discard/reopen; legitimate newepoch response succeeds. Same-draft close/discard/reload must invalidate old work as well. Keep doubleApply gating.

R200-03 — Sticky uncertain commit:
After saveFailed(outcomeUnknown), selectApplyRequest currently returns ready; edited resets statusclean. Hold uncertainty across local edits and block another PUT until an EXPLICIT safe reconciliation/fresh read establishes a new server baseline (or explicit discard followed by mandatory fresh load). No auto replay, no bumping revision from client, no pretending the previous write failed.
Retain edits/dirty warning while unknown; no hidden overwrite. Add minimal explicit reducer actions/selectors if needed, no generic workflow framework.
Test network-unknown, then edit/reselect/saveStarted => blocked, zero replay; explicit chosen recovery/load decision is required, stale reconciliation result ignored, confirmed fresh server document allows only a newly deliberate save. Discard must not clear uncertainty into old-baseline resubmission. Preserve conflict/error behavior and confirmed same-revision no-op adoption.

R200-04 — Narrow wire/selection coherence:
Current decoder shape-checks but accepts zero GUID, nested type.draftId different from root, duplicate contentLanguage ids / ambiguous default flags, fewer than seven schedule days,99:99:99. Request areaWrite only checks kind, so a resolver may substitute a DIFFERENT same-kind terminal id.

Reject emptyGUID identities where server guarantees genuine identities.
Require type.draftId equals outerdraftId (case-insensitive GUID identity); revision already checked.
Content registry: unique real ids, exactly one declared default, defaultContentLanguageId matches it, no invented order/default or fa/en restriction.
Supplied configured schedule: exactly seven unique ISO days; real TimeOnly bounds HH00..23/mm/ss00..59 and valid fraction precision. Do not introduce new timezone/DST/cutoff policy.
Resolved ancestry kind AND terminal identity must equal requested read selection; validate canonical required/forbidden ancestor field presence for its kind. A wrong/missing terminal must reject/needsResolution, NEVER rewrite destination to another id.
Compact negative matrix + real valid7day/mixed-language control. Keep canonical read raw text/time/decimal representation untouched and no duplicate backend pricing/business algorithm.

No overhaul of wire-json parser or new general JSON library. Public type/result changes stay within this feature. Prefer explicit typed request-construction failure to uncontrolled UI exception where natural; don't add hardcoded user-facing prose.

Authorized files

Only src/frontend/features/admin-shipping-authoring existing api/model/session/index and their focused tests; small cohesive helper in that feature if essential.
Docs: received task verbatim docs/ai/tasks/TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R1.task.md; own evidence RESULT.md; append-only TB200 RESULT correction; one worker recovery pointer.
No AGENTS/skill/config/package/lock/baseline/backend/schema/Host/live-page/route/preview/design-system/sharedCSS/shared-auth transport changes. No visual claim; no rendered screen yet.

Verification and limits

Run exact regression subset before repair, then current feature adapter/session tests once after corrections and one npm run typecheck (or equivalent existing tsc command), each command<=120s. No whole-discovery/build/Next server/browser/HTTP probe/backend test/DB/container. No need repeat unrelated known source-size guard failures or fix its four unrelated offenders. Preserve strictprecision and other existing feature positives.
At most TWO harness-only corrections; genuine requested production repairs are the mission, not mislabeled harness corrections. Stop on a new out-of-scope defect with precise result, no broad loop.

No dependency/image download, package install, sharedapp3000/3419/5088/DB5432/Redis/RabbitMQ operation, cache/bin/obj/.next purge, credential output or live merchant API writes.
25min HARD; stop new coding/tests at18; reserve7 for diff/evidence/commit/push/fetch parity/result. Report UTC accurately and separate actually-executed vs reused proof.

Result

Keep TB200 historical13PASS/typecheck as history with append-only correction that it did not cover these bugs. Three old corrections exceeded allowance2 (not >1); no retrospective compliance claim. No task201 until review.
BEGIN_TOOBA_WORKER_RESULT: task/parent/mode/base/code/evidenceSHA/parity; pre-fix actual regression and post-fix counts; preserved-baseline-vs-explicit[] proof; A->B->A/load/save epoch proof; stickyunknown/reconciliation proof; narrow wire/terminalid negatives; files/public signature delta; typecheck results; deviations/time/cleanup; Production-Functional-Acceptance:PENDING; Schema-Data-Flag-Changes:NONE; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.