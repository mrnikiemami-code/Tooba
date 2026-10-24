PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R2
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_CURRENT_GENERATION_PARENT_EVIDENCE_AND_ADD_GUARD_REPAIR
Expected-Base-SHA: 65d1842c5ab889a10815b7d03c29cf1713fb4aba
Hard-Timebox-Minutes: 20
Stop-New-Implementation-And-Tests-At-Minute: 14
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Goal
Close two executed remaining204-R1 picker defects. Preserve its sibling-request/clear/label/terminal fixes and all group/rate/week/General/ONEPUT functionality.
Read AGENTS/latest recovery,204-R1 ARCHITECT-REVIEW and mandatory frontend skill/standard. Verify exactbase HEAD==origin/main safe fast-forward; preserve unrelated work. No reset/rebase/stash/force-push/Bridge probe.199/200/201/202/203 closed, no proof replay.

A. Fresh context means fresh prerequisite
Architect executed unchanged coordinator with deferred reads:
updateA -> resolveAareas -> updateB -> updateA BEFORE freshAareas resolves
=>3area reads,2city reads both forA; secondcity already dispatched using old verifiedAreas.
Bind parent evidence to CURRENT opened generation. Re-entering A, language changes or disable/resume must not use previous verifiedAreas to discharge the fresh areas read already started. Clear/rebind evidence or associate it with active successful stage identity; no global cache/framework.
Cities must remain unavailable/pending until current areas succeed, even on A->B->A; current failure =>no city read; known-empty success remains explicit known-empty. Preserve correct sibling independence and late completion rejection. No request/retry loop.

B. Add uses current verified ACTIVE terminal path, not only stored choice flags
Country-wide city with non-null parent can be selected with staged administrativeArea=null. Current choiceChainActive checks activity only when a staged area exists; thus an active city under an inactive returned area passes. Coordinator's known id set includes inactive rows.
Implement ONE testable eligibility/path helper actually used by canAdd AND rechecked by the real Add handler:

use CURRENT successfully loaded rows/context to establish the chosen country/city/service membership and the actual parent administrative row, including activity and country ownership;
countrywide non-null city parent: matching current returned area must exist and be active even though no admin filter is selected;
null-admin city and its service area remain lawful on a verified country-wide read;
explicit area filtered read requires that exact parent, no null-parent contradiction;
selected values from an old language/context, absent current rows, pending/error prerequisite or disposed generation cannot be added from a stale choice snapshot. Handler should consult current coordinator evidence rather than an obsolete closure alone.
inspect only the REQUIRED terminal path: Country selection need not wait for unrelated cities/serviceareas; AdministrativeArea terminal ignores irrelevant deeper staged city. Don't make all loading/errors globally block every terminal.
inactive/currently missing parent/terminal rejects, no silent removal of stored draft selections. Server remains final authority, no new permission/coverage rules.

Small strict-adapter correction authorized:
geography-lookup-api.ts parentAccepted still returns true for null City parent BEFORE checking explicit admin filter; missing countrywide known set falls through true.
Check explicit filter first, and require countrywide parent evidence (known empty is valid evidence, absent evidence is not). Null-admin remains valid only with lawful country-wide evidence. Don't change routes/DTO/auth/error vocabulary.

Executed focused regressions, no broad sweep
Write new regressions before fixes where possible, using SAME coordinator/decoder/eligibility helper wired into UI:

A->B->A secondA city read zero until freshAareas; failedfreshA produces0city; latefirstA cannot satisfy newA.
countrywide activecity +inactive actualadmin rejects; activeparent accepts; absentparent rejects; nulladmincontrol accepts; serviceinherits only verified current city.
language/contextrefresh makes oldchoice temporarily ineligible until CURRENT path succeeds; errors stay blocked; Country can add despite unrelated child failure.
filteredcity withnullparent rejected; countrywide missingknownset rejected; explicitknownempty+nullparent accepted, foreignnonnullparent rejected.
Run new focused file and existing12coordinator+9group facts ONCE after final changes, typecheck and touched import/feature/test-discovery/flat-folder guards. Commands<=120s. No root68/nested foundation sweep, source-size/backend/Host/solution/build/discovery/browser test or package install.
Source wiring assertion may supplement but not replace the executed evidence/path tests. Report pure/function/source versus mounted coverage honestly.
At most1 bounded harness correction; stop PARTIAL with exact blocker if wider work is needed. Do not chase unrelated latent issues, add proof loops or weaken expectations.

Allowed changes only
Existing destination-read-coordinator, destination-choice or one small eligibility helper, strict geography adapter, picker wiring and focused tests/public exports if needed. No group/rate/week/general model rewrite; no backend/Contracts/DTO/route/auth transport/DI/schema/DB/Host/legacygrid/sharedlib/design-system/CSS/package/config/skill/AGENTS/baseline change.
No create/activation/cutoff/carrier/funding/quote/globalregistry/autosave/retry/postsaveGET. No server/browser/login/network/livewriteprobe/container/migration/install/download/sharedport/process/cache/bin/obj/.next operation.
20min HARD fromclaim; stop newcoding/tests14; final6 for evidence/diff/scopedcommit/push/fetchparity/result.
Persist task verbatim, own RESULT, append-only204-R1 correction and one recovery pointer. Exactcounts/files/time/deviations/cleanup; no visual/live acceptance.
Current main's post-result.js is historical evidence only: never execute/probe it.
Result BEGIN_TOOBA_WORKER_RESULT includes IDs/base/code/evidence/parity; Production-Functional-Acceptance:PENDING; Schema-Data-Flag-Changes:NONE; Visual:NOTRUN; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.
Only this task; wait review before205/visual envelope.