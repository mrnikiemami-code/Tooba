PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-LIST-PUBLICATION-USER-TRIAL-221
Parent-Task: TB-SHIPPING-ADMIN-DRAFT-PUBLICATION-API-220
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 87a138a224a3f12b3f1e4b481a9e4cc3980f7fd5
Mode: BOUNDED_VISIBLE_ADMIN_LIST_EDITOR_PUBLICATION_AND_REAL_USER_TRIAL
HARD45 minutes: UI/source25, focusedchecks5, runtime/browser10, docs/commit5.
STOP new implementation at25. ONE correction round. Do NOT repeat220's120-minute loop.
Persist this task verbatim FIRST; no lost-artifact Bridge probe later.

USER PRIORITY
User explicitly wants a testable PANEL NOW, before more Storefront work. Deliver the list->new editor->
publication workflow on the REAL local stack and a usable URL. No new engine/audit/helper-only milestone.
Read TOP AGENTS/recovery,220/ARCHITECT-REVIEW, applicable frontend/UI/backend skills. Preserve210/211 layout,
paired week rows, alternating area cards and accepted editor dirty/discard/one-PUT behavior.

1 PRIMARY LIST MUST REACH ACTUAL AUTHORING METHODS
/admin/shipping-services defaults to an owner-authorized list of EXISTING authoring drafts using the
existing authoring-drafts GET. Reuse AppDataGrid and existing saved-view infrastructure, not a plain table
or second grid framework. Feature owns transport/model/UI, app composes only through public index.ts;
inject app-owned saved-view adapter via thin client wrapper if necessary, NO feature->app import.
View/Edit row actions navigate DIRECTLY to actual /authoring/{draftId}?mode=view|edit.
No intermediate legacy modal. Stable row identity is SERVER draftId, never a fabricated method code.

CRITICAL: user's real trial draft binds post + NULL option. It must appear/reach correctly even if
legacy catalog rows bind post:standard / post:express. DO NOT attach it to one of those by splitting or
fuzzy matching methodCode. Use real owner draft list as authority for configured methods.
Keep old catalog/create/legacy controls reachable via explicit SECONDARY 'کاتالوگ و تنظیمات قبلی' link
(e.g. ?legacy=1 UI-only), not silent fallback on new-list errors. Existing catalog data never deleted.
This is primary configured-method management replacing its old View/Edit workflow, NOT a claim all
catalog/Storefront runtime has been replaced. No global tab redesign.

List needs real stored names: minimally ADD an init-only MethodTranslations property using the EXISTING
ShippingAuthoringMethodTranslationItem on existing ShippingAuthoringDraftItem, Include Translations in
directory.ListAsync and map once (no endpoint/new positional constructor/schema). Existing binding-only
consumers ignore additive field. NO per-row editor/presentation/publication GET (no N+1).
Use actual existing authoring-context/default-content-language registry for the default display name;
no locale->content-language or fa/en id guess. Missing translation is honest 'بدون نام' + actual binding.
Show name, binding, saved revision / area count and View/Edit. Do not relabel legacy isActive as Published.
Empty, denied, unavailable and ambiguous data stay explicit; NO auto POST/create on load or click View.
Whole-method creation beyond existing catalog flow is NOT this trial slice.

2 PUBLICATION CONTROLS IN EXISTING EDITOR FEATURE
Feature-owned strict API/model + small cohesive publication UI/state, compose into ShippingAuthoringScreen.
Reuse authoringHeaders/admin transport, same-origin rewrite, design-system Dialog/buttons and FA/EN messages.
GET publication when current draft loads; actual Host enum wire names must be verified (Host uses string
enums,220 test harness integers are NOT wire authority). Null fields may be OMITTED by Host policy.
Validate requested draftId, safe/exact integer tokens and coherent state/provenance. No rounded long token.
States: not published/withdrawn/current revision published/OLDER revision published; show real revision.
'انتشار نسخهٔ ذخیره‌شده' and 'برداشتن انتشار' only from CLEAN VIEW mode with known pointer version.
Before mutation explicit existing Dialog confirmation; Publish sends ONLY expectedRevision from confirmed
editor baseline + exact expectedSelectionVersion. Withdraw sends ONLY expectedSelectionVersion.
No implicit save/publish, owner/body identity, invented active/ready/checkout success or publish on Edit.
One synchronous in-flight gate BEFORE await: duplicate click =>one POST. Pending blocks publication actions.
409/403 preserve editor work, no retry. Network/abort/malformed2xx => explicit UNKNOWN latch: block another
publication write until operator explicitly reads publication status; never fabricate failure/success or
reset token0. Scope completions to draft/epoch, abort/dispose on switch. No stale response affects another
draft. Publication GET/error must NOT erase form or disable legitimate ordinary draft editing/saving.
Adopt validated POST response without editor-document reload or form reseed. No automatic post-save GET:
matching current draft vs selected capture can use confirmed baseline revision equality.
Short honest note: this selects a published version; checkout integration is not yet enabled. No technical
implementation jargon in normal flow. Keep public/internal export boundary and existing controller authority.

ONE TINY BACKEND CONSISTENCY FIX (carried220, no R1 loop)
Withdraw handler currently returns Cleared/version0 for ABSENT pointer, contradicting DTO and next GET.
Return Absent when cleared.Value.Exists is false; existing Cleared retains positive version. Verify real
absent-withdraw+GET agree when exercised below. No new schema/selection authority/readiness rules.

3 FOCUSED CHECKS ONLY
One focused feature test file on SAME API/state/list helpers the components call:
real string enums/omitted nullable fields; exact tokens; list uses real ids/default language;
one publish/withdraw body; duplicate gate;409+UNKNOWN explicit-read recovery; stale draft response ignored.
npm run typecheck ONCE after final source; relevant import/feature/flat-folder/discovery guards once.
No119/full frontend/backend/solution/browser matrix/confidence loop or unrelated source-size baseline fix.

4 REQUIRED LIVE RUNTIME (ALREADY AUTHORIZED, DO NOT ASK AGAIN)
User explicitly said this is NONproduction and authorized required migrations/Host refresh/startup effects.
Target ONLY127.0.0.1:5432/tooba_alpha, alias tenant-alpha, SingleStore/store-alpha.
Reverify actual Host5088 / FE3000 process identities; old PIDs are history, not a kill instruction.
Build current Host AND existing MigrationRunner once from current source, task-owned isolated outputs,
--no-restore / existing dependencies. No solution/test sweep, install or download; do not write locked sharedbin.
Runner plan MUST show exactly existing pending:
20261025094223_AuthoringRevisionSnapshots
20261025130021_SelectedCapturedRevision
(all other module pending sets empty). Apply with existing real CLI --tenant store-alpha; no invented
--module flag, no manual SQL/history editing, no repeat of nine already applied migrations.
If plan differs, stop ONLY that unsafe scope and report exact mismatch, no generic all-migration guess.
Record schema history/table verification and actual shared changes, not 'NONE'.

Refresh ONLY exact verified Host5088 with current isolated binary, same Development/edition/tenant/config,
keep old binary for rollback. Preserve frontend3000 and user's tab; Next dev should hot-reload. Existing
startup/schema/seed/background effects were already authorized: record them honestly; no new seed/reset/
drop/auth bypass/edition change. Use exact PID, graceful bounded stop then force only that PID if needed.
Avoid cmd trailing environment spaces (use safe quoted SET syntax).
No Docker engine restart, task DB/container unnecessary for this slice; no unrelated process/port change.
No secrets/connection strings logged or committed.

5 REAL MOUNTED COMPLETION GATE (NO /v1 INTERCEPTION)
Reuse installed Chrome + finite-timeout CDP harness with isolated task profile, real traffic only.
No fixture/fulfill/mock/login bypass/user-profile reload/package install.
Actual trial draft:01a16eeb-70b3-7000-87c2-564c82835496. Re-read current fields/revision FIRST, do not assume2.
- Main list shows the REAL stored name; View/Edit routes reach THAT draft directly (post/NULL lawful).
- Regions/rates/week render; local add-area-card/add-row/money text controls reachable; cancel/reject/confirm
  uses existing flow. NO ordinary draft PUT/field overwrite needed: discard local inspection changes.
- Publication real GET200; actual Host enum/nullable wire recorded. If originally Absent/Cleared, perform one
  deliberate publication via real button/confirmation; pending duplicate gate, success shows real snapshot/
  revision; explicit status read confirms persistence. Then withdraw via button to restore 'no selected
  capture' intent (version legitimately advances, record it; never reset/delete pointer). If originally
  Selected, do not withdraw somebody else's selection: read/idempotent same-revision path only when safe.
  CAS conflict from concurrent user work is respected, never reread+auto-overwrite.
- Where pointer is originally absent, a real expected0 withdrawal can verify the tiny Absent0 response fix
  without creating a row; no replay loop. No SQL business data mutation.
- Fresh read before/after proves actual draft fields/revision unchanged. Capture/pointer trial writes and
  migration/startup effects MUST be disclosed as such; not 'zero writes'.
- Capture main list + publication editor1440, one390 viewport (no page overflow; grid may scroll within its
  own container). Preserve alignment/RTL, no user Visual ACCEPT inferred.
Only after these actual checks can report READY_FOR_USER_TRIAL_ADMIN_PANEL with exact working URLs.
Typecheck/screenshots alone are NOT readiness. If blocked, give the first concrete remaining blocker;
do not spend another two hours expanding harness/framework.

FINISH
Task+RESULT evidence/TB-SHIPPING-ADMIN-LIST-PUBLICATION-USER-TRIAL-221/; scoped source+screenshots evidence.
Update ALL THREE TOP recovery files (canonical+AGENTS+new-chat guide): actual runtime binary/PID/config,
migrations now applied or still pending, actual user draft/revision, list URL, publication outcome,
shared changes, precise remaining Storefront ownership/type/membership/measurement +consumer wiring.
No Storefront development in221, no gate flip/TB185/quote/activation/certification claim.
Exact-path staging, commit/push/fetch parity; no gitadd-A/history rewrite/historical post-result.js/Bridge.
STOP_FOR_ARCHITECT, Automatic-Next-Task NONE. NO222 ENVELOPE.
Next gate is USER TRIAL of this admin panel before more Storefront implementation.

