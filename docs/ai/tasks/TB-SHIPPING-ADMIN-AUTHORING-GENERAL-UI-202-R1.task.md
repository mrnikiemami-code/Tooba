PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202-R1
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_GENERAL_FORM_DIRTY_DISCARD_AND_REACT_LIFECYCLE_REPAIR
Expected-Base-SHA: a66573615d902df3ceeefc43adf571a2db59179f
Hard-Timebox-Minutes: 35
Stop-New-Implementation-And-Tests-At-Minute: 25
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Outcome
Finish the delivered202 General UI's existing state/lifecycle contract. Do not implement another feature or replay backend199/frontend200/readbridge201. Read AGENTS, latest canonical recovery,202 ARCHITECT-REVIEW, mandatory frontend skill/standard and Shipping rules first. Fetch/verify expectedbase HEAD==origin/main on a safe tree; preserve unrelated work. Fast-forward only, no reset/rebase/stash/force-push, no Bridge probe.

Raw local work must remain dirty even when invalid

Actual unchanged202 editor-form execution:

createEditorForm(configuredBaseline) -> buildEditorEdits emits unchanged methodSettings; screen enables Apply although session is clean and controller blocks it.
stored name changed to blank, settings unconfigured -> edits={} plus nameRequired. controller.edit({}) marks clean and screen clean-form effect may rebuild the form.
one independent setting chosen on unconfigured baseline -> edits={} plus settingsIncomplete and falsely-clean.
description typed for an untranslated active language with blank name -> edits={} and issues=[].

Repair form derivation and screen/session integration, not only button disabling:

Untouched configured settings must omit wrapper. Change either axis preserves the other; reverting both to baseline is clean. Both-or-none on first configuration remains.
Raw changed name/description and incomplete settings remain unsaved work, including invalid input, across language/tab/view interactions. Description-only with blank name is a local nameRequired error, not ignored input.
Apply stays blocked on local errors, with no partial successful patch that silently omits them. Close/discard/browser warning still detects them.
Returning all values to confirmed baseline becomes clean; do not keep phantom dirty state.
Keep ONE session authority, distinguish raw dirty intent from valid wire patch with a minimal typed extension if needed. session/editor-session.ts may change narrowly for this; preserve epoch/requestId, unknown latch, explicit reconciliation and accepted200 guards.
Form initialization/reset happens only on the appropriate confirmed load/save/discard transition. An invalid edited({}) or a render must never erase typed values. Reconcile preserves pending local edits for operator review against the confirmed new baseline.
No unchanged settings/type coercion, inactive-language echo, guessed defaults, new backend validation or separate store.
Explicit Cancel Edit / Back, safe during save

Current Cancel Edit directly setForm(baseline), controller.edit({}), setMode(view), with no confirmation and enabled during save/reconcile.
Reuse the existing Dialog/session discard decision:

Dirty or invalid Cancel Edit requests confirmation. Reject keeps the exact form, mode, baseline, intent, uncertainty and revision.
Confirm Cancel Edit returns to view of the confirmed document without navigating away. Confirm Back navigates back only after explicit approval. Track pending UI destination locally, not a second editor store.
While save or reconciliation is actually pending, prevent these controls from resetting/discarding active work or presenting obsolete state as success. No extra PUT, automatic retry or post-save GET.
Unknown commit remains latched and unresubmittable until the existing explicit fresh-read recovery; Cancel Edit must not clear it.
Clean Cancel Edit may return to view directly.
No global navigation framework/history hacks; existing feature controls and beforeunload only.
Actual React/controller identity and retirement

Current useEditorController captures first draftId in a ref, empty-dependency callbacks/effect never change and has no cleanup; route has no key. Repair the real wiring:

A reused clean screen receiving B must load/display/control B, never A, and must reset A-specific local form/language/notice identity appropriately. An explicit key boundary is allowed if it fits existing route and lifecycle.
Dispose/retire obsolete controllers and subscriptions; late A load/save/reconcile must not mutate B or show A success metadata. A completion rejected by epoch/request identity must not still update savedRevision/notice.
Handle React StrictMode setup -> cleanup -> setup without reusing a permanently disposed controller or losing the initial load forever. Do not simply add cleanup to the current captured-ref effect.
Same-draft rerender/locale update must not recreate/discard an active editing session. Preserve accepted dirty/saving same-draft guards.
Use a small lifecycle binding/helper if necessary inside the existing feature; no new state framework/package or auth/transport replacement. Fix comments to match actual behavior.

Verification and scope
Write focused regression facts first where current Node pattern permits. Cover:
configured untouched/change/revert; blank existing name and description-only unnamed language; one-sided settings dirty+invalid; invalid after valid edit not reset; Cancel Edit request/reject/confirm versus Back; saving/unknown cannot be reset or replayed; clean A->B with late A completions ignored; lifecycle mount/cleanup/remount remains usable.
Tests must exercise the changed functions actually called by the screen/hook, plus inspect/report the actual React wiring. Pure reducer tests alone are not proof of hook mount behavior. Use existing installed test capabilities; if no React mount harness exists, do not install one or claim a browser/mounted test. Keep a focused lifecycle helper test and state the wiring limitation precisely.
Preserve existing38 feature regressions, ONE PUT gate and retained group/row/geography/currency/schedule behavior. Run new focused tests, feature tests once after final changes, npm run typecheck, and touched import/feature/test-discovery guards only, commands<=120s. No whole frontend discovery/build/backend/Host/solution suite. At most2 bounded harness corrections; stop PARTIAL with an exact remaining blocker instead of a proof loop.

Allowed: existing admin-shipping-authoring feature model/session/hook/screen/messages/tests/public boundary; thin authoring/[draftId]/page only for identity boundary. Minimal existing feature internals only. No legacy dialog/grid rewrite, new regions/rates/week editing, create/activation/cutoff/carrier/funding controls, backend/schema/DB/Host/shared library/design-system/sharedCSS/package/config/skill/AGENTS/baseline change.
No server/browser setup/login/network/live-write probe, container/migration, shared app3000/3419/5088/DB5432/Redis/RabbitMQ/process restart, download/install, cache/bin/obj/.next purge, credentials.
Visual remains NOTRUN unless independently available within existing authorized environment; no visual acceptance from typecheck.

Hard35minutes fromclaim, stop newcoding/tests25; final10 only diff/evidence/scopedcommit/push/fetchparity/result. No hidden deadline extension.
Persist received task verbatim; own RESULT with exact regression-before/after, actual UI/session/lifecycle wiring, commands/counts vsNOTRUN and process deviations; append-only correction to202 RESULT; one worker recovery pointer. Preserve history, no certification/promotion.
Result BEGIN_TOOBA_WORKER_RESULT includes IDs/base/code/evidence/parity, actual fixes/proof limits/time/cleanup; Production-Functional-Acceptance:PENDING; Schema-Data-Flag-Changes:NONE; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.
Only this task. No next envelope or task203 until Architect review.