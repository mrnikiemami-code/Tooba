PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R2
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_PREVALIDATED_COMPOSITE_SAVE_AND_LOCALE_BROWSER_PROOF
Expected-Base-SHA: 0c8f2803462442b8eda8aa51823420f196d7519f
Hard-Timebox-Minutes: 25
Stop-New-Interaction-And-Tests-At-Minute: 18
Maximum-Runtime-Setup-Minutes: 4
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Outcome: ONE complete mounted save, not another fixture-design session
Read AGENTS/latest recovery,205-R1 ARCHITECT-REVIEW, frontend skill and accepted186-R2 visual reference. Verify exactbase HEAD==origin/main safe tree; preserve unrelated work. Production199-204 accepted; no code/proof replay. No reset/rebase/stash/forcepush/Bridge probe.
No production file changes. Only harness/evidence. All /v1 browser traffic remains synthetic fail-closed; real merchant writes forbidden.

PREPARED AND ALREADY EXECUTED BY ARCHITECT
Use docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R2/scripts/save-scenario.mjs
and ARCHITECT-FIXTURE-PREFLIGHT.md. Its preflight() passed with UNCHANGED production parser/writer/decoder:
initial7 and saved8 both decode; negative PUT-write-areas inserted into a read response is rejected; raw money/time retained.
This is ONE fixed scenario, not a generic mock backend. DO NOT splice PUT groups into GET groups again.

Wire the existing205-R1 harness to the prepared scenario (minimal)
Reuse its finite-timeout CDP command functions, actual route, whitelist/typed shell fixtures and installed runtime approach. Do NOT design a new harness/framework.
Import initialResponseText, savedResponseText, expectedRequestText, assertScenarioRequest, preflight, IDS/NAMES/NEW_TITLE/NEW_END from the prepared module.
Initial editor GET -> initialResponseText.
Successful PUT -> capture body and increment count, call assertScenarioRequest(body). ONLY if it passes, fulfill savedResponseText and retain that text for subsequent GET/reload. A mismatch is a test failure with body diff, never a fabricated success. No production decoder change.
Add a simple hold/release of that ONE paused PUT so pending double-click can be measured, then fulfill the already-prepared response. No generic business/mutation engine.
The existing R1 fixtures remain for countries/types/shell and country1 branch data. Correct ONLY the harness branch for country2: areas=[] and cities=[] (real successful known-empty), since its current areas fixture belongs to country1. Leave country1 parent-aware fixtures as-is.
Fail all unhandled /v1 locally; no real mutation or fallback. Treat every CDP/timeout/protocol/fixture error as failure.
One preflight invocation permitted to confirm correct imports. No repeated pure-test confidence loop.

Known task-only runtime, <=4min
Use installed Node/Next/Chrome, task-owned frontend copy/output +node_modules junction,127.0.0.1:3517, fresh task browser profile/CDP9555. Env only TOOBA_HOST_ORIGIN=http://127.0.0.1:1. No install/download/shared .next/tsconfig/next-env/userprofile/shared:3000 restart.
Prove ports free before start, install interception BEFORE navigation.
Actual route /admin/shipping-services/authoring/<IDS.draft>?mode=edit. If runtime not ready within4min, STOP BLOCKED exactreason; no environment chase or replacement Node sweep.

Drive this EXACT visible scenario (it matches the prepared expected request)
Use real clicks/typing/select events on PRODUCTION components, not React state/helpers:

FA method name -> NAMES.fa = "ارسال استاندارد R2".
switch EN CONTENT tab -> NAMES.en = "Standard shipping R2"; switch back FA and prove first name retained.
root week Monday (ISO1): edit END ONLY to18:15. Start must remain09:00:00.500 (fractional exact peer); all other days unchanged.
Regions: add ONE group, active FA title NEW_TITLE = "گروه آزمایشی".
in that group's real picker choose Country terminal and countries row IDS.country2 (active Germany); deliberately Add destination. Retained country1 admin/city groups remain disjoint/unchanged.
add ONE nested row: freight0.50, packaging0.00, delivery min1/max2; all optional conditions blank/null. No existing group/row removal/reorder or other edits in this fixed success scenario.
Apply ONCE, hold the intercepted PUT, try a second visible click while pending. Assert putCount remains1; body matches assertScenarioRequest including expectedRevision7, exact retained ids/money/time, null ids on new group/row and no local keys.
Release to savedResponseText. Assert actual UI leaves saving/unknown, enters view, reports revision8, shows both edited names, Monday changed end/intact start, new group/title/row and its real synthetic assigned ids from actual confirmed document. No post-save GET (delta from pre-Apply GET baseline; StrictMode initial doubleGET is separate).
Predeclared response ids: group77777777-7777-4777-8777-999999999999; row88888888-8888-4888-8888-999999999999.
Read DOM/network diagnostics to prove adoption. Do not infer success from sending or fixture200 alone. If outcomeUnknown occurs, capture exact decoder reason/response and STOP; do not invent another fixture pass.
Four locale/viewport captures after the success
Use canonical tooba_locale cookie via CDP Network.setCookie/setCookies (path /, correct origin), THEN full Page.navigate/reload the same actual route in view mode. Do not merely change --lang, content-language tab or dir attribute.
Capture FA1440x900 General, FA390x844 Regions, EN1440x900 Regions, EN390x844 General (at least4). Confirm DOM interface lang/dir actually changed (fa/rtl vsen/ltr); content direction follows registry language.
Observe overflow/labels/buttons/dialog reachability, all rate axes, geography once/group, one root week and reference186-R2 spacing/layout. Synthetic screenshots only; no personal/account data or CSS/design-system patch.
Screenshots are review evidence, not userVisualACCEPT.
Preserve explicit remaining gap list: blank-name/discard controls, scripted409/transport/reconcile and full geography/move/remove matrix were not completed in205-R1. Do not claim allA-E from this narrowed scenario. If time remains, one such independent control may be observed and reported precisely, but no scope expansion or confidence loop.

Time / evidence
25min HARD fromclaim; stop newinteractions/tests18; final7 cleanup/evidence/diff/scopedcommit/push/fetchparity/report. At mostONE harness/fixture correction after prepared preflight. Stop PARTIAL on another blocker, with exact first diagnostic; no production fix.
Allowed only205-R2 task-owned harness/logs/synthetic screenshots/evidence, append-only205-R1 correction, task receipt and one recovery pointer. Prepared save-scenario remains the source of THIS fixture; any change must explain which preflight assumption failed, never weaken parser or expected body.
No build/typecheck/whole Node suite/source-size/backend/Host/solution/discovery/package/config/skill/AGENTS/baseline change. No server/backend/DB/schema/flag/credentials/livewriteprobe/sharedport/cache/bin/obj/.next cleanup. Never run historical post-result.js.
Stop exact task-owned processes/temp outputs; prove3517/9555 free afterwards and shared:3000 untouched.

Result include source/base/code/evidenceSHA/parity, setup and interaction times, exact request count/body assertions, successful adoption or exact decoder rejection, 4screenshots with real interface lang/dir, explicit NOTRUN controls, fixture-vs-live boundary and cleanup.
Production-Functional-Acceptance:PENDING; Shared-Database-Data-Flag-Changes:NONE; Visual:REVIEW_EVIDENCE_ONLY or actualNOTRUN/BLOCKED; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.
Only this task; no automatic next envelope.
