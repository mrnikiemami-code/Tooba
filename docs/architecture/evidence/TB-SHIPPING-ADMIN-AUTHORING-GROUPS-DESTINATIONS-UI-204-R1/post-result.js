/* Posts the TB-204-R1 worker result to the Bridge results API (complete contract only). */
const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R1
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_DESTINATION_PICKER_REQUEST_AND_CHAIN_CORRECTNESS_REPAIR
Bridge-Task-Row: 905c2033-34e0-4b7c-b974-e7675fad51c6
Status: PASS
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 5eaf479417a1d771f6c428f242e901e35b2d76f3
Base-SHA: 5eaf479417a1d771f6c428f242e901e35b2d76f3
Code-SHA: 0b1f9abcbad8c1a5fc244ef5354413510dcf1cd8
Evidence-SHA: 0b1f9abcbad8c1a5fc244ef5354413510dcf1cd8
HEAD-Origin-Parity: HEAD == origin/main == 0b1f9abcbad8c1a5fc244ef5354413510dcf1cd8 (fetch verified, 0/0 ahead-behind)
Production-Functional-Acceptance: PENDING
Schema-Data-Flag-Changes: NONE
Visual: NOT_RUN (no mounted/browser harness was used; no visual claim)
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE

ROOT CAUSE (confirmed by execution, not by source pattern)
The 204 picker wired four sibling Geography reads to ONE shared epoch ref. A country change made the areas effect increment the epoch and the cities effect increment it again, so a correct areas response was ALWAYS discarded; cities closed over stale/empty lists.areas, excluded areas from dependencies and passed knownAdministrativeAreaIds:undefined when empty, so parentAccepted accepted ANY non-null city parent. Independently: choiceAncestry took administrativeAreaId from choice.city.parentId even for an AdministrativeArea terminal; no active/chain guard existed at Add (inactive rows were ordinary options); area/city/service onChange returned previous on null so a staged parent could not be cleared; OptionSelect fell back to option.code for a blank label.

REPAIR
1. ONE COORDINATOR: new ui/destination-read-coordinator.ts (framework-free, adapter injected) owns all four stages. Each stage has its own request token AND its own branch key, so a sibling completion can never invalidate another sibling of the SAME context (the country read is no longer destroyed when areas/cities start). Any parent/language change aborts, invalidates and EMPTIES only the dependent stage, so stale branch rows are never selectable/Add-able while the new parent loads; unmount/dispose aborts and drops late completions.
2. VERIFIED PREREQUISITE: a country-wide city read starts only after the areas read for that exact language|country succeeded; the verified id set is the parent evidence, and a legitimately EMPTY set is a KNOWN EMPTY set passed explicitly (fail closed), never undefined. A failed prerequisite leaves cities with zero rows, a typed failure and loading:false, and no country-wide read is attempted. A filtered administrativeAreaId read keeps its exact filter and passes no country-wide set.
3. TERMINAL-PATH ANCESTRY: choiceAncestry serializes an AdministrativeArea terminal from its OWN staged row (country+admin with a deeper null-admin city now yields terminal=admin and ancestry=admin, not null); City/ServiceArea terminals take exactly their verified city row parentId; the terminal id always equals the serialized tuple terminal.
4. ACTIVE CHAIN GUARD: new choiceChainActive is used for canAdd AND re-checked inside the Add handler, so a forged/stale programmatic option cannot bypass it: country/area/city/service must be active, an area parent must be the staged country, a city's non-null parent must equal the staged area, a null-admin city under an explicit area filter is incoherent, a service area parent must equal the staged city. Inactive rows render disabled; stored inactive/unresolved draft selections stay visible and are NOT removed.
5. REAL CLEARING: clearAdministrativeArea / clearCity / clearServiceArea replace the no-op placeholder branch; clear-area returns to the country-wide city branch, clear-city drops service areas, clear-service drops only the service area, and clearing invalidates the old request/rows.
6. HONEST LABEL: optionDisplayName renders messages.destinationNameUnavailable for a blank label; never code, never GUID, no fabricated name. Server-side label fallback is unchanged.

UNCHANGED (verified not touched): group collection/add/remove/reorder/title patches, rate rows, root week, General form, composed intent, single Apply/ONE PUT, request builder, discard/reseed, server-owned currency/authority, active route /admin/shipping-services/authoring/[draftId]. No backend/Contracts/DTO/route/transport-auth/DI/schema/DB/Host/grid/design-system/shared-CSS/package/config/skill/baseline change.

EXECUTED PROOF (real deferred promises on the SAME coordinator the component wires; no React framework installed)
New features/admin-shipping-authoring/destination-picker-204-r1.test.ts, 12 tests: sibling country read still applied after areas/cities start (the exact 204 defect); country-wide cities start only after areas succeed with administrativeAreaId:null plus the verified set, and the real decodeGeographyOptions rejects a city whose parent is not in an EMPTY verified set (untrusted); failed prerequisite yields no city read, empty rows, loading:false, typed failure; out-of-order country A->B aborts the obsolete areas request, clears rows, drops A and applies B; clear-area aborts the filtered city read, clears city rows, keeps the verified area rows, starts the country-wide read and drops the late filtered response; clear-city aborts the service-area read and drops its late completion; dispose aborts and drops; ancestry cases (AdministrativeArea terminal with deeper city, lawful null-admin city service area, mismatched city/area, inactive at every level, wrong service parent, foreign area parent); clear helpers cascade; blank label equals the unavailable name and differs from code and id.
shipping-groups-204.test.ts (204 facts preserved, 9 tests): the previous source-regex /epoch.current/ assertion was replaced by 204-05b, an EXECUTED two-parent out-of-order case on the same coordinator.

FOCUSED COMMANDS AND COUNTS (frontend root, actually run)
node --experimental-strip-types --test features/admin-shipping-authoring/destination-picker-204-r1.test.ts -> 12/12 PASS
node --experimental-strip-types --test features/admin-shipping-authoring/shipping-groups-204.test.ts -> 9/9 PASS
node --experimental-strip-types --test features/admin-shipping-authoring/*.test.ts -> 68/68 PASS (7 matched files: authoring-general-ui, authoring-lifecycle-202-r1, authoring-rates-week-203, authoring-rates-week-203-r1, repair-200-r1, shipping-groups-204, destination-picker-204-r1)
npm run typecheck -> PASS
node --experimental-strip-types --test lib/architecture/frontend-feature-boundary.guard.test.ts frontend-import-boundary.guard.test.ts frontend-test-discovery.guard.test.ts frontend-flat-folder.guard.test.ts -> 7/7 PASS

NOT RUN (explicit, not claimed)
Nested api/editor-document-adapter.test.ts, session/editor-session.test.ts and session/editor-session-200-r2.test.ts are NOT matched by the root glob and were NOT run; the former "full foundation" wording is withdrawn. frontend-source-size guard not run (four pre-existing failures outside this task, no baseline widening). No backend/Host/solution/build/discovery sweep, no package install, no browser/mounted React/visual run, no login or live HTTP probe, no container/migration.

EVIDENCE / REPORTING CORRECTIONS (append-only on 204, history preserved)
Actual entry route is /admin/shipping-services/authoring/[draftId]; the earlier /drafts/{id} claim was wrong and no routing change exists or was made. The 55 figure came from the feature-root glob only. The original late-epoch claim was source-pattern evidence and is superseded by the executed 204-05b and the new coordinator suite.

CHANGED PATHS
src/frontend/features/admin-shipping-authoring/ui/destination-read-coordinator.ts (new)
src/frontend/features/admin-shipping-authoring/ui/authoring-destination-picker.tsx
src/frontend/features/admin-shipping-authoring/model/destination-choice.ts
src/frontend/features/admin-shipping-authoring/destination-picker-204-r1.test.ts (new)
src/frontend/features/admin-shipping-authoring/shipping-groups-204.test.ts
docs/ai/tasks/TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R1.task.md (received artifact, verbatim)
docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R1/RESULT.md (new)
docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204/RESULT.md (append-only correction)
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md (one pointer)
AGENTS.md (checkpoint pointer only)

LIMITS / DEVIATIONS
Mounted-React, browser, live and Visual remain NOTRUN; no UI/visual/functional production acceptance is claimed from module tests. Component wiring was inspected but not mounted, and the async proof comes from the executed coordinator tests. Two self-authored test-side typing/expectation corrections were made while bringing the new suite up (no production-expectation weakening). Elapsed ~30 minutes from Working heartbeat to result; stop-new-implementation was respected before the final evidence/commit/push window.
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK`;

fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({
    channelId: "tooba-main",
    taskId: "TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R1",
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
