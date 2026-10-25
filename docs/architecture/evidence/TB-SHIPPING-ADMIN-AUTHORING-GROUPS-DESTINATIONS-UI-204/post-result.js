/* Posts the TB-204 worker result to the Bridge results API (complete contract only). */
const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203-R1
Mode: BOUNDED_EXISTING_DRAFT_GROUP_COLLECTION_AND_REAL_DESTINATION_EDITOR
Base-SHA: 06466c7546aa2801690f888015583db958866e4e
Code-SHA: 790c2583c12953986666b493d30122aeca603ce4
Evidence: docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204/RESULT.md
Evidence-SHA: 790c2583c12953986666b493d30122aeca603ce4
Parity: HEAD == origin/main == 790c2583c12953986666b493d30122aeca603ce4 (clean tracked tree)
Stop-State: STOP_FOR_ARCHITECT
Production-Functional-Acceptance: PENDING
Schema-Data-Flag-Changes: NONE
Visual: NOT_RUN (no permitted mounted/browser harness was used)
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE

1. ONE COLLECTION MODEL, ONE COMPOSED INTENT, ONE PUT
model/editor-groups.ts owns the ordered collection: an existing group's local key IS its server groupId,
a new group is a client-local key with groupId:null and currencyCode:null (no invented GUID, no fabricated
currency). Add / remove (rows go with the group) / move up-down / per-active-language title text / explicit
area add-remove all live there; rate rows stay in the accepted RatesFormState keyed by the SAME local key,
so there is no second grouping store. buildGroupsEdits emits undefined (PRESERVE) when topology, areas,
titles and rows are unchanged, and otherwise the FULL ordered replacement with retained exact ids/areas/
rows and only touched title pairs. Removing every group is [] only from explicit removal of all groups.
model/authoring-intent.ts composes General + groups + rates + week into ONE EditorEdits; the 4-argument
call keeps the accepted 203 baseline-topology semantics unchanged. The screen has one commit() that sets
form/groups/rates/week and dispatches exactly one controller.edit(composed.edits, composed.localDirty);
Apply remains one PUT expectedRevision through the unchanged request builder.

2. TITLES ARE PATCHES, NOT COPIES
Student-relevant facts: [] = untouched (an untouched or inactive stored title is never echoed or deleted),
an EMPTY active text = {languageId, title:null} clearing exactly that pair, trim-equivalent text is not an
edit, and inactive stored titles stay visible read-only in the group header. Language ids/direction come
from presentation.contentLanguages (registry driven, no fa/en hardcode in logic).

3. REAL GEOGRAPHY, STRICT, BRANCH-BY-BRANCH
api/geography-lookup-api.ts is a feature-owned typed read adapter over the EXISTING routes only:
/v1/admin/geography/countries?languageId=
/v1/admin/geography/countries/{countryId}/administrative-areas?languageId=
/v1/admin/geography/countries/{countryId}/cities[?administrativeAreaId=&]languageId=
/v1/admin/geography/cities/{cityId}/service-areas?languageId=
Authority verified in Geography.Endpoints/Admin/GeographyAdminEndpoints.cs (GeographyPlaceOption(id,code,
label,parentId,isActive) and bare-value success via ApiResponseFactory.From<T>). Decoding is strict: a
non-array payload, non-GUID/zero id, blank code, non-boolean isActive, duplicate identity, or a parent
foreign to the requested branch is a TYPED failure (untrusted) - never success[] and never a filtered
smaller list. 401/403 -> denied, other non-OK -> unavailable, transport -> network. A genuinely empty
catalog stays ok. A blank label renders as unavailable, never as code or GUID; identity is (kind,id).
The picker reads only the branch the operator opens (no eager country/world preload), each read is
epoch-guarded and aborted on parent/language switch, so a late old-parent response cannot win. City rows
keep their ACTUAL nullable parentId (verified against the returned areas when non-null) and the
highlighted area is never substituted; a city with no admin stays lawful; service areas inherit the
verified city tuple.

4. ONE RESOLVER; BLOCKING ON THE PROPOSED TREE
composeAncestryResolver merges the document's validated 201 destinationFacts (retained) with this
session's verified picks into the single Apply resolver. Blocking uses unresolvedDraftSelections over the
PROPOSED groups, so explicitly removing an unresolved destination/group lets the rest save, while a
remaining unresolved selection still blocks with a localized label. Duplicate (kind,id) is rejected locally
before dispatch; canonical cross-level/cross-group overlap remains backend authority (PreflightRegionGroups
/ AuthoringRegionSelectionOverlap) - no second coverage engine, no pricing/grouping algorithm.
Confirmed discard calls the ONE cohesive reseed (form+groups+rates+week from the same baseline) and clears
the session-picked facts; Cancel rejection, busy guard, epoch and unknown/reconcile handling are unchanged.

5. REACHABLE UI
/admin/shipping-services/drafts/{id} -> ShippingAuthoringScreen -> regions tab: add group, move up/down,
delete with explicit confirmation, per-language title input, destination chips with explicit removal
(unresolved chips are visible and removable only deliberately), cascading picker, nested 203 rate rows.
The superseded authoring-regions-panel.tsx was removed. Still out of scope: whole-method create,
activation/readiness/status/cutoff/carrier/funding/quote, owner/seller picker, any pricing algorithm.

6. COMMANDS ACTUALLY RUN (Node 24, this worktree)
node --experimental-strip-types --test features/admin-shipping-authoring/shipping-groups-204.test.ts
  -> 8 passed / 0 failed
node --experimental-strip-types --test features/admin-shipping-authoring/*.test.ts
  -> 55 passed / 0 failed (new 204 + 203 + 203-R1 + 202 + 201 + 200 + adapter/session/entry)
npx tsc --noEmit -> exit 0
import-boundary + flat-folder + feature-boundary + test-discovery guards -> 7 passed / 0 failed
NOT RUN (deliberate): source-size guard (four pre-existing failures untouched, no baseline widening),
full solution/backend/Host/build/discovery sweeps, mounted React, browser, visual, any live HTTP/login
probe, any container/migration/package/process operation.

7. FILES / TIME / DEVIATIONS
Added: api/geography-lookup-api.ts, model/editor-groups.ts, model/destination-choice.ts,
ui/authoring-destination-picker.tsx, ui/authoring-groups-editor.tsx, shipping-groups-204.test.ts.
Modified: authoring-core.ts (additive exports), model/authoring-intent.ts, model/authoring-drafts.ts,
ui/authoring-screen.tsx, ui/authoring-rates-editor.tsx (exports the existing row editor for reuse),
messages/authoring-messages.ts (group/picker strings in both locales), authoring-rates-week-203-r1.test.ts
(one source-wiring assertion relaxed from resetAuthoringDrafts(document) to resetAuthoringDrafts(document
because the helper gained the registry language list; its intent is unchanged).
Removed: ui/authoring-regions-panel.tsx.
Deviations: no backend/schema/DTO/route/shared-service/design-system/baseline/package change, no new
persistence/cache/global registry, no feature->app import of the legacy picker. One Set.add truthiness
defect in my own strict decoder (duplicates were silently accepted) was caught by the new duplicate case
and fixed before commit. Evidence limits: the new suite calls the SAME helpers the component wires and
asserts screen wiring, but it is not mounted-React/browser proof; visual/live acceptance is NOT claimed.
Hard 45-minute box: new coding/tests stopped at the 35-minute stop mark; final minutes were diff, evidence,
scoped commit/push/fetch parity and this report.

Worker PASS != Architect ACCEPT.
END_TOOBA_WORKER_RESULT`;

fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({
    channelId: "tooba-main",
    taskId: "TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204",
    content,
  }),
})
  .then(async (response) => {
    console.log(`RESULT_STATUS=${response.status} ${(await response.text()).slice(0, 300)}`);
    if (!response.ok) process.exitCode = 1;
  })
  .catch((error) => {
    console.log(`RESULT_FAILED=${String(error)}`);
    process.exitCode = 1;
  });
