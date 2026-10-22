/* Posts the TB200-R1 worker result to the Bridge results API (complete contract only). */
const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R1
Parent-Task: TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200
Mode: BOUNDED_BASELINE_PRESERVATION_SESSION_EPOCH_AND_UNKNOWN_OUTCOME_REPAIR
Base-SHA: 29cf0117eeaa087c7edc5e4cf1a0240d1a9f5e8e
Code-SHA: __CODE_SHA__
Evidence: docs/architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R1/RESULT.md
Evidence-SHA: __EVIDENCE_SHA__
Parity: HEAD == origin/main == __CODE_SHA__
Stop-State: STOP_FOR_ARCHITECT
Production-Functional-Acceptance: PENDING
Schema-Data-Flag-Changes: NONE
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE

PRE-FIX EXECUTED REGRESSION (unchanged TB200, new suite written first)
0 passed / 12 failed. Observed by execution, not by source argument:
- R200-01: one-group baseline + method-text-only edit produced BASELINE_GROUPS 1 with body
  {"expectedRevision":4,"regionGroups":[],"methodTranslations":[...]} i.e. regionGroups:[] from a text-only edit.
- R200-02: no epoch existed; loadSucceeded/save completions carried no session identity and A->B->A reused
  generation 1, so the stale A completion was accepted.
- R200-03: saveFailed(outcomeUnknown) followed by edited reset save to clean.
- R200-04: decode accepted "groupId":"00000000-0000-0000-0000-000000000000" (ok:true).
POST-FIX: same suite 12/12; whole feature set (repair + adapter + session) 25 passed / 0 failed; tsc --noEmit exit 0.

R200-01 PRESERVED BASELINE vs EXPLICIT []
buildEditorDocumentBody(baseline, edits, resolver) is baseline-aware. edits.regionGroups OMITTED serializes the
complete retained tree (same groupId, order, areas re-resolved through ancestry, every rateRowId and exact decimal
token text such as 1850000.00 / 0.50 / 12.50) with titles:[]; methodTranslations:[] when untouched; only explicit
edits.regionGroups:[] removes the collection. Proof: two populated baseline groups with inactive-language stored text
- method-only, settings-only and type-clear edits keep both group ids and both row ids, untrimmed/inactive text is not
echoed, explicit [] clears, a retained CITY selection with no authoritative ancestry returns needs-resolution and NO
body (never [] or an omitted group), and the confirmed baseline is not mutated (JSON snapshot equal before/after).
Active-language intent is enforced from the baseline registry: an inactive/unknown language id in a method or title
intent returns invalid-intent (languageUnavailable) instead of being filtered into a smaller successful patch.
One pre-fix assertion was corrected to check the exact body text instead of a JSON.stringify round-trip.

R200-02 A->B->A / LOAD / SAVE EPOCH
EditorSessionState.epoch replaces the per-draft generation and is monotonic, never reset: loadStarted +1,
discardConfirmed +1 (switch and same-draft discard), reconcileStarted +1. Every loadSucceeded/loadFailed/
saveStarted/saveSucceeded/saveFailed/reconcile* carries the epoch (save also keeps the in-epoch requestId). Non-matching
completions return the SAME state object. Proven: A load pending -> switch B -> reopen A -> old A success and old A
failure ignored; late old A save success with a reused numeric requestId cannot clear new A edits after
switch/discard/reopen; same-draft close/discard/reload invalidates old work; current-epoch completion succeeds;
duplicate Apply still gated and not queued.

R200-03 STICKY UNKNOWN / RECONCILIATION
saveFailed(outcomeUnknown) latches commitUncertain; edited keeps save=outcomeUnknown, selectApplyRequest returns
blocked:uncertain and saveStarted is ignored (zero replay, no client revision bump, no assumption the write failed).
Only two explicit exits: (a) reconcileStarted/reconcileSucceeded at a new epoch installs the confirmed server document,
clears the latch, keeps edits dirty and then allows a newly deliberate save - stale reconciliation results are ignored;
(b) discardConfirmed leaves baseline null and load idle with the latch retained, so the old baseline is not
resubmittable and a fresh loadStarted+loadSucceeded is mandatory. Conflict/validation/error behaviour and confirmed
same-revision no-op adoption are preserved.

R200-04 NARROW WIRE / TERMINAL ID NEGATIVES
Decoder rejects empty GUIDs, type.draftId inconsistent with the root draft id, duplicate content-language ids, a
declared-default count other than exactly one, a defaultContentLanguageId that is not the declared default, a configured
schedule that is not exactly seven unique ISO weekdays, and malformed TimeOnly text (99:99:99 rejected; HH00-23/mm/ss00-59,
fraction 1-7 digits). Valid control (7 days, mixed rtl/ltr registry, raw text/time/decimal) still decodes unchanged; no
timezone/DST/cutoff policy and no pricing algorithm added. Request construction now requires the resolved ancestry to
match the requested kind, carry the canonical required ancestors, omit forbidden deeper ancestors and resolve to the
SAME terminal id; wrong-kind, same-kind-different-id, missing terminal and extra-ancestor resolutions all return
invalid-intent and are never rewritten to another destination.

FILES / PUBLIC SIGNATURE DELTA (inside the feature only)
Modified: api/editor-document-request.ts, api/editor-document-decode.ts, session/editor-session.ts, index.ts and their two
existing tests; added repair-200-r1.test.ts. buildEditorDocumentBody(EditorDocument, EditorEdits, resolver);
EditorRequestBody gains invalidIntent; EditorSessionState generation->epoch plus commitUncertain/reconcile/
reconcileFailure; new reconcileStarted/reconcileSucceeded/reconcileFailed actions; blocked reasons add uncertain and
invalid-intent. No backend/schema/Host/package/config/baseline/shared-lib/auth-transport/route/page/preview/design-system
change. Public results stay inside this feature.

TYPECHECK / COMMANDS
Repair suite run once pre-fix (0/12), three bounded iterations during repair, then one full feature run (25/25).
tsc --noEmit: two test-helper narrowing errors, corrected, then exit 0. Not run: build, whole discovery, Next server,
browser/HTTP probe, backend tests, DB/container, live network. The four unrelated source-size offenders were not re-run
or fixed and no baseline was widened.

DEVIATIONS / TIME / CLEANUP
Three pre-fix/bounded test iterations and one helper narrowing fix are recorded as-is; TB200's three corrections against
an allowance of two are documented in its append-only correction, not relabelled. Elapsed ~27 min vs a 25-min hard box
(~2 min over during final evidence/push; no new coding after the test loop closed). No container, port, cache or
credential operation; no reset/rebase/stash/force-push.

LIMITATIONS
The write-side ancestry port is still intentionally unimplemented until the UI task supplies the authoritative Geography
resolution; unresolved retained selections correctly return needs-resolution. reconcile* is a minimal explicit state
transition, not a workflow framework. No visual, live-registry or production acceptance is claimed.

Worker PASS != Architect ACCEPT.
END_TOOBA_WORKER_RESULT`;

const body = JSON.stringify({
  channelId: "tooba-main",
  taskId: "TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R1",
  content,
});

fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body,
})
  .then(async (response) => {
    const text = await response.text();
    console.log(`RESULT_STATUS=${response.status} ${text.slice(0, 300)}`);
    if (!response.ok) process.exitCode = 1;
  })
  .catch((error) => {
    console.log(`RESULT_FAILED=${String(error)}`);
    process.exitCode = 1;
  });
