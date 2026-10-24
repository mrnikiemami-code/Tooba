# TB-204 — bounded existing-draft group collection + real destination editor

Task: `TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204` (parent `203-R1`)
Mode: `BOUNDED_EXISTING_DRAFT_GROUP_COLLECTION_AND_REAL_DESTINATION_EDITOR`
Base: `06466c7546aa2801690f888015583db958866e4e`
Feature: `src/frontend/features/admin-shipping-authoring`

## What changed

1. **Cohesive group draft model** (`model/editor-groups.ts`). One model owns the ordered collection:
   existing group key = its server `groupId`; a new group is a client-local key with `groupId: null`
   and `currencyCode: null`. Add / remove / move up-down / per-active-language title text / area
   add-remove. Rate rows stay in `RatesFormState` keyed by the SAME local key (no second grouping
   store). `buildGroupsEdits` emits the full ordered `regionGroups` replacement only when topology,
   areas, titles or rows really changed; otherwise `undefined` (PRESERVE). Titles are patches: `[]`
   untouched, `{title: null}` clears exactly one pair, trim-equivalent text is not an edit, and
   inactive stored titles are never echoed/deleted. Explicit removal of every group yields `[]`.
2. **One composed intent** (`model/authoring-intent.ts`). General + groups + rates + week compose to
   ONE `EditorEdits`; the 4-argument form keeps the accepted 203 baseline-topology semantics.
3. **Cohesive reseed** (`model/authoring-drafts.ts`). `seedAuthoringDrafts` / `resetAuthoringDrafts`
   now return `{form, groups, rates, week}` from the SAME confirmed document; the screen also clears
   session-picked facts on the same reset.
4. **Strict feature-owned Geography read adapter** (`api/geography-lookup-api.ts`). Exact existing
   routes only (`countries`, `countries/{id}/administrative-areas`, `countries/{id}/cities[?administrativeAreaId]`,
   `cities/{id}/service-areas`, always with the configured content `languageId`). Non-array payload,
   non-GUID/zero id, blank code, non-boolean `isActive`, duplicate identity, or a row whose parent is
   foreign to the requested branch is a TYPED failure (`untrusted`); 401/403 → `denied`; other
   non-OK → `unavailable`; transport → `network`. A lawful empty catalog stays `ok` with no options.
   Labels are display-only: a blank label renders as unavailable, never as the code or the GUID.
5. **Cascading on-demand picker** (`ui/authoring-destination-picker.tsx` + `model/destination-choice.ts`).
   Only the opened branch is read; every read is epoch-guarded and aborted on parent/language switch,
   so a late response of an old parent cannot win. Country-wide cities keep the row's ACTUAL nullable
   `parentId` (verified against the returned areas when non-null), never the highlighted area; a city
   with no administrative area remains lawful. Terminal level is explicit (Country / AdministrativeArea
   / City / ServiceArea); changing a parent clears only the dependent STAGED levels, never a saved
   chip. Duplicate `(kind, id)` is rejected locally before any dispatch.
6. **ONE resolver** (`composeAncestryResolver`). Retained `destinationFacts` + this session's
   verified picks feed the single Apply resolver. Apply blocking inspects the PROPOSED tree
   (`unresolvedDraftSelections`): explicitly removing an unresolved destination/group unblocks the
   remaining valid tree, a remaining unresolved one still blocks with a localized reason.
7. **Group collection UI** (`ui/authoring-groups-editor.tsx`). Ordered cards with read-only group
   identity/currency, per-active-language title input (inactive stored titles shown read-only),
   destination chips with explicit removal (unresolved chips stay visible and removable only
   explicitly), the picker, add/remove-with-confirmation/move controls, and the reused 203 rate-row
   editor nested per group. `authoring-regions-panel.tsx` is superseded and removed.
8. Messages added for both locales (no `fa`/`en` hardcode in logic; RTL/LTR comes from the dialog
   shell/`dir` of the screen).

## Wire / parent authority used

- `src/backend/Modules/Geography/Tooba.Geography.Endpoints/Admin/GeographyAdminEndpoints.cs` —
  `GeographyPlaceOption(Id, Code, Label, ParentId, IsActive)`, success = bare JSON value
  (`ApiResponseFactory.From<T>` → `Results.Json(result.Value)`), country `parentId = null`, area
  `parentId = countryId`, city `parentId = AdministrativeAreaId` (nullable), service area
  `parentId = cityId`.
- `ShippingAuthoringDraft.PreflightRegionGroups` / `ValidateShape`: ≤50 groups, ≤200 selections per
  group, ≤500 per draft, duplicate `(kind, id)` across the whole draft is
  `AuthoringRegionSelectionOverlap` (backend authority — the UI only rejects exact local duplicates),
  an empty selection list is lawful, `NormalizeTitle` trims and clears to null (max 200).
- Existing `EditorDocument`/`EditorEdits`/`buildEditorDocumentBody` contract is unchanged: one PUT
  with `expectedRevision`, full ordered groups, nested rows, touched titles only.

## Reachable UI (bounded)

`/admin/shipping-services/drafts/{id}` → `ShippingAuthoringScreen` → tab «گروه‌های مناطق و هزینه‌ها»:
add group, move up/down, delete with confirmation, per-language title, destination chips + picker,
nested rate rows; Apply sends ONE PUT. Still absent by scope: whole-method creation, activation,
carrier/cutoff/funding/quote, any pricing/overlap algorithm, owner/seller picker.

## Verification actually run (Node 24, this worktree)

| Command | Result |
| --- | --- |
| `node --experimental-strip-types --test features/admin-shipping-authoring/shipping-groups-204.test.ts` | 8/8 pass |
| `node --experimental-strip-types --test features/admin-shipping-authoring/*.test.ts` | 55/55 pass (204 + 203 + 203-R1 + 202/201/200) |
| `npx tsc --noEmit` | exit 0 |
| import-boundary / flat-folder / feature-boundary / test-discovery guards | 7/7 pass |
| source-size guard | NOT re-run (four pre-existing failures deliberately untouched) |
| mounted React / browser / visual | NOT RUN |

The new suite calls the SAME helpers the component wires (`buildAuthoringIntent(..., groups)`,
`buildGroupsEdits`, `withGroup*`, `composeAncestryResolver`, `unresolvedDraftSelections`,
`decodeGeographyOptions`, `geographyQueryPath`, `seed/resetAuthoringDrafts`) and asserts the screen
source calls them; the picker's epoch/abort/gating is source-inspected, not mounted-browser proof.
`authoring-rates-week-203-r1.test.ts` R203-08 was relaxed from `resetAuthoringDrafts(document)` to
`resetAuthoringDrafts(document` because the helper gained the registry language list; the intent of
that assertion (Cancel Edit uses the cohesive helper and clears the session intent) is unchanged.

## Deviations / limits

- No backend, schema, DTO, route, shared-service, design-system or baseline change; no new
  persistence/cache/global registry; no world/all-country scan; no live HTTP/login probe.
- One `Set.add` truthiness defect in the strict decoder (duplicates were not rejected) was found by
  the new duplicate case and fixed before commit.
- Visual/live/manufacturer-level acceptance: NOT RUN → `Production-Functional-Acceptance: PENDING`.

## Files

Added: `api/geography-lookup-api.ts`, `model/editor-groups.ts`, `model/destination-choice.ts`,
`ui/authoring-destination-picker.tsx`, `ui/authoring-groups-editor.tsx`,
`shipping-groups-204.test.ts`.
Modified: `authoring-core.ts`, `model/authoring-intent.ts`, `model/authoring-drafts.ts`,
`ui/authoring-screen.tsx`, `ui/authoring-rates-editor.tsx` (export `RateRowEditor`),
`messages/authoring-messages.ts`, `authoring-rates-week-203-r1.test.ts` (one assertion relaxation).
Removed: `ui/authoring-regions-panel.tsx` (superseded by the group editor).

## Correction appended 2026-10-24 (TB-204-R1) — reporting only, history preserved

1. Actual shipped entry route is `/admin/shipping-services/authoring/[draftId]`. The earlier
   `/drafts/{id}` statement above was wrong; no such new route exists in the 204 diff and no routing
   change was made or is needed.
2. The "55 tests" figure came from the feature-ROOT glob `features/admin-shipping-authoring/*.test.ts`
   (7 files at the time: authoring-general-ui, authoring-lifecycle-202-r1, authoring-rates-week-203,
   authoring-rates-week-203-r1, repair-200-r1, shipping-groups-204 — plus the newly added
   destination-picker-204-r1 in R1). Nested `api/editor-document-adapter.test.ts`,
   `session/editor-session.test.ts` and `session/editor-session-200-r2.test.ts` were NOT matched and
   were NOT run. "Full foundation suite" was never true and is withdrawn.
3. The original "late epochs are guarded" claim was SOURCE-PATTERN evidence (`/epoch\.current/`), not
   an executed race. It is superseded by the executed deferred-promise case 204-05b plus the new
   coordinator suite in 204-R1.
4. The single-epoch defect the Architect reported was real and is confirmed by execution: the areas
   completion was discarded whenever the cities read started. Fixed in R1 only.
