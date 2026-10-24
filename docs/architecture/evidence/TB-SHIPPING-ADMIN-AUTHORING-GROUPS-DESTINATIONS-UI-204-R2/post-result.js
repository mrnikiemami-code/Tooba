/* Posts the TB-204-R2 worker result to the Bridge results API (complete contract only). */
const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R2
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_CURRENT_GENERATION_PARENT_EVIDENCE_AND_ADD_GUARD_REPAIR
Bridge-Task-Row: 60e3beb9-1092-44a4-853a-15428074717f
Status: PASS
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 65d1842c5ab889a10815b7d03c29cf1713fb4aba
Base-SHA: 65d1842c5ab889a10815b7d03c29cf1713fb4aba
Code-SHA: ec957367fadd94ceb14045e1b013d2ec308bade2
Evidence-SHA: ec957367fadd94ceb14045e1b013d2ec308bade2
HEAD-Origin-Parity: HEAD == origin/main == ec957367fadd94ceb14045e1b013d2ec308bade2 (fetch verified)
Production-Functional-Acceptance: PENDING
Schema-Data-Flag-Changes: NONE
Visual: NOT_RUN
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE

A. FRESH CONTEXT MEANS FRESH PREREQUISITE (reproduced, then repaired)
The coordinator kept a Map<language|country, ids> named verifiedAreas. Re-entering A after B started a fresh areas read AND immediately dispatched a country-wide city read with the OLD A set, so a cached set discharged a prerequisite it had not satisfied (Architect's 3-area/2-city reproduction).
Repair: the map is removed. Parent evidence is one generation-bound record {key, ids} | null that is CLEARED the instant a new areas generation starts (startAreas), including the SAME language|country (A->B->A), and on reset/failure/disable; it is SET only by a successful CURRENT areas completion (a legitimately EMPTY read sets {key, ids:empty}), and a country-wide city read proceeds only when evidence.key === areasKey(). DestinationReadState gained an explicit settled record (countries/areas/cities/serviceAreas): true only after the CURRENT generation completed successfully; begin/hold/reset/failure clear it, including beginStage, so a restarted stage can never look settled with empty rows.
Executed: A -> resolve A -> B -> A gives 3 areas reads and only 1 city read (the first A one); the second A city read exists only after the fresh A areas resolve. A failed fresh A read gives 0 city reads with an empty city list and a typed cities failure. A late first-A completion is aborted and dropped, cannot settle the new generation, and produces no city read. Sibling independence and late-completion rejection are unchanged.

B. ADD USES THE CURRENT VERIFIED ACTIVE TERMINAL PATH
New pure helper destinationAddEligibility(choice, evidence) in model/destination-choice.ts derives eligibility from the CURRENT rows plus settled flags, never from staged flags alone, and is used by canAdd AND re-checked synchronously in the real Add handler against coordinator.current?.getState(). Rules: country terminal = current active country row with no parent; administrative-area terminal = current active area row whose parent is that country; city terminal = current active city row, an explicit area filter requires exactly that parent and rejects a null-parent contradiction, a country-wide read requires a settled branch and a non-null parent must be a CURRENT ACTIVE area row of that country, a null-admin city stays lawful; service-area terminal = current active service row whose parent is the verified current city. Only the required terminal path is inspected, so a Country/AdministrativeArea Add is not blocked by unrelated city/service-area failures; after a language/context refresh the emptied rows make the old choice temporarily ineligible until the current path succeeds; pending and failed generations stay blocked; stored draft selections are never silently removed.
The coordinator's id set still contains inactive ids (evidence, not permission); activity and membership come from the rows, so an active city under an INACTIVE returned area is now rejected, and a city whose parent is absent from current rows or whose stage area became inactive is rejected.

STRICT ADAPTER CORRECTION (authorized): for City, an explicit administrativeAreaId filter is evaluated FIRST and requires exactly that parent (null parent rejected); a country-wide read without knownAdministrativeAreaIds is now untrusted (absent evidence is not evidence), while a verified EMPTY set keeps null-admin cities lawful. Known ids are normalized to lower case before the parent predicate. Routes/DTO/auth/error vocabulary unchanged.

UNCHANGED: sibling request independence, clear cascade, terminal-level ancestry, unavailable label, group collection/title/rates/week/General composition, single Apply/ONE PUT, discard/reseed, server-owned currency/authority, and the active route /admin/shipping-services/authoring/[draftId]. No backend/Contracts/DTO/route/auth/DI/schema/DB/Host/grid/design-system/CSS/package/config/skill/baseline change.

EXECUTED FOCUSED PROOF (new regressions written first; no broad sweep)
features/admin-shipping-authoring/destination-picker-204-r2.test.ts, 8 tests on the SAME coordinator/decoder/eligibility helper wired into the UI: A->B->A waits for the fresh areas read (1 city read before, 2 after); failed fresh A gives 0 city reads; a late first-A response cannot satisfy the new generation; country-wide active city under an inactive returned area rejects; active parent accepts; absent parent rejects; null-admin control accepts; null-admin without a settled country-wide read rejects; pending cities stage rejects; service area inherits only the currently verified city (foreign parent, absent row and inactive row rejected); context refresh ineligibility; Country can add despite unrelated child failure; AdministrativeArea terminal ignores a deeper staged city; filtered city whose stage area became inactive rejects; inactive country rejects; filtered city with null parent rejects; the decoder matrix (explicit filter exact, missing known set untrusted, explicit empty set plus null parent accepted, foreign non-null parent rejected). One source-wiring assertion supplements and does not replace the executed evidence.

COMMANDS AND COUNTS (frontend root, actually run)
node --experimental-strip-types --test features/admin-shipping-authoring/destination-picker-204-r2.test.ts -> 8/8 PASS
node --experimental-strip-types --test features/admin-shipping-authoring/destination-picker-204-r1.test.ts features/admin-shipping-authoring/shipping-groups-204.test.ts features/admin-shipping-authoring/destination-picker-204-r2.test.ts -> 29/29 PASS
npm run typecheck -> PASS
node --experimental-strip-types --test lib/architecture/frontend-feature-boundary.guard.test.ts frontend-import-boundary.guard.test.ts frontend-test-discovery.guard.test.ts frontend-flat-folder.guard.test.ts -> 7/7 PASS

NOT RUN (explicit): the root 68-fact sweep, nested adapter/session suites, frontend-source-size, backend/Host/solution/build/discovery, package install, browser/mounted React/visual, login/live HTTP, containers/migrations.

CHANGED PATHS
src/frontend/features/admin-shipping-authoring/ui/destination-read-coordinator.ts
src/frontend/features/admin-shipping-authoring/ui/authoring-destination-picker.tsx
src/frontend/features/admin-shipping-authoring/model/destination-choice.ts
src/frontend/features/admin-shipping-authoring/api/geography-lookup-api.ts
src/frontend/features/admin-shipping-authoring/destination-picker-204-r2.test.ts (new)
docs/ai/tasks/TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R2.task.md (received artifact, verbatim)
docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R2/RESULT.md (new)
docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R1/RESULT.md (append-only correction)
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md (one pointer)
AGENTS.md (checkpoint pointer only)

LIMITS / DEVIATIONS
Mounted React, browser, visual and live remain NOTRUN; no UI/visual/production acceptance claimed. One bounded harness correction was used: the settled flag had to be cleared in beginStage as well (production, reported), plus test-side ordering of the deferred area reads. No request/retry loop, no expectation weakening, no unrelated scope absorbed. Elapsed ~14 minutes to stop-new-code, with evidence/commit/push inside the 20-minute window.
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK`;

fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({
    channelId: "tooba-main",
    taskId: "TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R2",
    content,
  }),
})
  .then(async (response) => {
    const body = await response.text();
    console.log(`STATUS ${response.status} ${body}`);
    if (!response.ok) process.exitCode = 1;
  })
  .catch((error) => {
    console.error(`POST FAILED ${error}`);
    process.exitCode = 1;
  });
