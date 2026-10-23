PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_EXISTING_DRAFT_GROUP_COLLECTION_AND_REAL_DESTINATION_EDITOR
Expected-Base-SHA: 06466c7546aa2801690f888015583db958866e4e
Hard-Timebox-Minutes: 45
Stop-New-Implementation-And-Tests-At-Minute: 35
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Outcome
Complete group/destination authoring within the EXISTING connected admin draft editor: group add/remove/reorder, selected-language group title patches, and multiple real destinations per group. Keep General/rates/root-week together in ONE editor-document Apply. No whole-method creation or activation.
Read AGENTS/latest recovery,203-R1 review, frontend skill/architecture standard and Shipping rules/accepted186-R2 reference. Verify expectedbase HEAD==origin/main safe fast-forward; preserve unrelated work. No reset/rebase/stash/force-push/Bridge probe.199/200/201/202/203 accepted; no proof replay.

Source authority (read before coding)

Current EditorDocument/EditorEdits and backend ShippingAuthoringEditorDocumentContracts + PreflightRegionGroups/PreflightPresentation: preserve exact replace versus patch semantics and own-group identities.
Existing app/admin/shipping-services/geography-admin-api.ts and shipping-destination-picker*.ts(x) show already-shipped Geography reads; Geography.Endpoints/Admin/GeographyAdminEndpoints.cs defines their real wire fields and parent semantics.
Legacy picker is app-owned, FA-only, decoder silently filters and falls back to code. Do NOT import app code into the feature or copy its permissive parser/unsafe identity assumptions. A small feature-owned typed read adapter to EXISTING routes is allowed, reusing current shared auth/header/failure conventions; no new auth client/backend seam, no legacy rewrite.

Ordered group collection, one geography set each
Use cohesive local group draft identities for existing and new groups:
existing stable groupId; new group client-local key with groupId:null. Never use a fake GUID in an EditorDocument baseline or confuse local key with server identity.
Allow add empty local group, explicit remove, move up/down and per-active-LanguageId title edit. Keep order by local list; every retained group/row id stays exact, new rows/groups null ids. No per-language/group copied schedule or duplicated geography under rates/packaging.
Titles are selected-language PATCHES: [] untouched, explicit null clears only that pair, unedited/inactive raw stored titles kept readable and NEVER echoed/deleted. Registry languages/direction, no fa/en hardcode. Invalid empty group/title/selection intent remains dirty and blocked according to existing contract, not silently omitted.
Group delete including its rows is deliberate local intent before Apply; provide clear confirmation for destructive existing-group removal. Remove/reorder must not erase edits in other groups. [] clears the full collection ONLY from explicit removal of all groups; never from missing local state or General-only edit.
Currency/binding/owner server-owned and read-only; new group no fabricated currency. Adopt actual server group/row ids/order/currency from validated successful response; never persist local keys.
Rates203 must work in NEW groups too, retaining exact money and all limits; week remains root once. Evolve its current baseline-only grouping minimally into the cohesive group draft model rather than a competing grouping store.

Real Geography picker, no world scan or guessed ancestry
Existing authorized GET routes only:
/v1/admin/geography/countries?languageId=<registry id>
/v1/admin/geography/countries/{countryId}/administrative-areas?languageId=...
/v1/admin/geography/countries/{countryId}/cities?administrativeAreaId=<optional>&languageId=...
/v1/admin/geography/cities/{cityId}/service-areas?languageId=...
These return arrays of GeographyPlaceOption {id,code,label,parentId,isActive} through current response conventions. Read actual endpoint/ApiResponseFactory if needed, don't guess envelope.
Strictly validate IDs/nonempty GUIDs, required scalar shapes, duplicate identities and parent context; malformed/foreign-parent response is typed error, NOT success[] or filtered smaller list. Legitimately empty catalog stays empty.401/403/dependency/network/untrusted states explicit/localized; do not substitute empty successful options.
Read only the selected country/area/city branch as operator opens/changes it. No eager all-country/world/recursive scans, new persistence/registry/global cache or backend query.
Pass actual configured content LanguageId for labels, independent of FA/EN interface locale. Existing Geography endpoint may fall back label to code; no new client-side code/id-as-name fallback. Missing label shows unavailable state, never raw GUID. Names stay display-only; selection identity always typed(kind,id).

Cascading choice and identity:

country options parentId=null; area rows parentId=countryId (per current endpoint);
city rows parentId is ACTUAL nullable administrativeAreaId, not country id. When choosing from country-wide cities, use that row's actual parent and verify it against returned country areas when non-null; don't substitute currently highlighted area or assume null. A city without admin remains lawful.
service-area rows parentId=selected cityId; inherit the VERIFIED selected city's country/admin tuple.
explicit terminal level Country/AdministrativeArea/City/ServiceArea; changing parent clears dependent staged selections, NOT the saved group's destination chips. Only deliberate Add selection appends the chosen canonical terminal to that group.
active-only new selection, no silent current inactive/unknown removal; unresolved stored201 facts visible and retained until explicit operator removal/replacement.
support multiple chips per group, remove one explicitly; reject duplicate(kind,id) locally. Canonical cross-level/cross-group overlap remains backend authority, not another geography/coverage engine.
late responses for old parent/group/language/session ignored; disabled during save/reconcile/view. New lookups have no write authority; server revalidates on PUT.

Use201 validated destinationFacts for retained selections + operation-local verified picker facts for newly selected keys as ONE resolver. Exact terminal/kind must match canonical area. Never trust caller-supplied hydrated facts over server validation.
Apply blocking must inspect the PROPOSED submitted tree, not all baseline selections: explicitly removing an unresolved destination/group must allow other valid submitted selections to save. Remaining unresolved selection blocks with meaningful label/state, no silent dropping.

Integrate ALL local drafts, preserve accepted safety
General, group topology/titles/selections, rates and week form one local composed intent/controller.edit. No capability callback replaces another capability's intent. Existing raw invalid dirty, formSeed/whole-reset/discard, busy guard, epoch and unknown recovery apply to new groups and staged picker work too.
Confirmed discard restores all group/rate/title/destination drafts from same baseline and clears staged lookups/selections appropriately. Cancel rejection preserves all; untouched capability stays absent wrapper/patch-free.
One deliberate Apply = ONE existing PUT expectedRevision with ordered full groups, nested rows and touched titles, optional week/settings/type and method text. No per-capability mutation, autosave, post-save reload or automaticretry.
403/409/errors preserve every local draft. PUT unknown blocks replay and explicit read reconciles before deliberate nextsave. No optimistic server id/revision or false success from dispatch.

Focused verification
Use existing Node/deferred-transport tests; no new framework/package.
Cover actual composed helpers/controllers wired into UI:

baseline0groups -> newgroup + real-shaped selected destination + nested row + title -> one body with null newids/localkeys absent; successfulresponse adopts realids;
two retainedgroups: reorder/remove-one/add-one without changing untouched ids/areas/rows/exactmoney/inactive text; explicit all-remove ->[]; General/rate/week edits all survive topology edits;
title active patch/explicitclear/untouchedinactive preserved;
Geography URLs/queryLanguageId, strict malformed/duplicate/wrong-parent rejection vs real empty; country-wide city actualadmin and nulladmincity/servicearea canonical tuples;
parent/language/group switch with late promise ignored, no hidden N+1 world preload, active-only additions;
proposed selection resolver: retained201facts + verified new choices; unresolved retained blocks, explicitly removed unresolved does not;
invalid/newempty raw input dirty+blocked, discard resets whole model, onePUT/duplicate gate/403/409/unknown preserve newgroup work.
Tests involving new helper must call the SAME helper wired into component, not unused source copies. Source inspection is not mounted/browser evidence; report limitation.
Run new focused tests, current203/203-R1 and202 focused UI regressions once after final code; npm run typecheck and relevant feature/import/test-discovery/flat-folder guards only, commands<=120s. No full foundation/backend/Host/solution/build/discovery/source-size sweep. Four old source-size failures stay untouched; no baseline widening.

Bounds / finish
Allowed only existing admin-shipping-authoring feature api/model/session/components/messages/tests/public boundary. No feature->app import or legacy picker/dialog/grid rewrite, no new route/backend/DTO/schema/DB/Host/shared library/design-system/sharedCSS/package/config/skill/AGENTS/baseline change.
No full-method create, owner/seller picker, activation/readiness/status/cutoff/carrier/funding/quote logic; no pricing/grouping algorithm.
No new server/browser/login/network/livewrite probe, container/migration/install/download/sharedports3000/3419/5088/5432/Redis/RabbitMQ/process/cache/bin/obj/.next operation. Visual NOTRUN if no existing usable permitted harness; never claim visualacceptance from typecheck.
45min HARD fromclaim, stop newcoding/tests35; final10 diff/evidence/scopedcommit/push/fetchparity/report. At most2 bounded harness corrections; honest PARTIAL on blocker, no confidence loop or extension.
Persist received task verbatim, own RESULT, one recovery pointer. Include actual wire/parent authority, reachable UI, onePUT/identity preservation, commandcounts vsNOTRUN, exactfiles/time/deviations/cleanup. Production functional acceptance still PENDING.
Result BEGIN_TOOBA_WORKER_RESULT includes task/parent/mode/base/code/evidence/parity; Production-Functional-Acceptance:PENDING; Schema-Data-Flag-Changes:NONE; Visual:actualstate; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.
Only this task; no automatic next envelope. Next review determines visual/live validation.