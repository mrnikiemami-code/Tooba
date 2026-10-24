# TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R2 — RESULT

Task-ID: TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R2
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R1
Base: `65d1842c5ab889a10815b7d03c29cf1713fb4aba` (HEAD == origin/main at claim)
Mode: BOUNDED_CURRENT_GENERATION_PARENT_EVIDENCE_AND_ADD_GUARD_REPAIR
Stop-State: STOP_FOR_ARCHITECT (Production-Functional-Acceptance: PENDING)

## A. Fresh context means fresh prerequisite

`destination-read-coordinator.ts` kept a `Map<language|country, ids>` (`verifiedAreas`). Re-entering
A after B started a fresh areas read AND immediately dispatched a country-wide city read with the old
A set — the cached evidence discharged a prerequisite it had not satisfied.

Repair: the map is gone. Parent evidence is a single generation-bound record
`{ key, ids } | null` that is
- CLEARED the moment a new areas generation starts (`startAreas`), including the same
  `language|country` (A → B → A), and on reset/failure/disable;
- SET only by a successful CURRENT areas completion (a legitimately EMPTY read sets `{key, ids:∅}`);
- accepted by a country-wide city read ONLY when `evidence.key === areasKey()`.

`DestinationReadState` gained `settled` (`countries|areas|cities|serviceAreas`): true only after the
current generation completed successfully, false on begin/hold/reset/failure. `beginStage` now clears
it too, so a restarted stage can never look settled with empty rows.

Executed consequence: A → resolve A → B → A yields 3 areas reads and only **1** city read (the first
A one); the second A city read appears only after the fresh A areas resolve. A failed fresh A read
produces **0** city reads and a typed cities failure. A late first-A completion is aborted, dropped,
and cannot settle the new generation.

## B. Add uses the current verified ACTIVE terminal path

New pure helper `destinationAddEligibility(choice, evidence)` in `model/destination-choice.ts` uses the
CURRENT rows/`settled` flags (not staged flags) and is used by `canAdd` AND re-checked synchronously in
the Add handler against `coordinator.current?.getState()`:

- country terminal: current active country row, no parent;
- administrative-area terminal: current active area row whose parent is that country;
- city terminal: current active city row; explicit area filter → exactly that parent and never a
  null-parent contradiction; country-wide → the branch must be settled and a non-null parent must be a
  CURRENT ACTIVE area row of that country; a null-admin city stays lawful;
- service-area terminal: current active service row whose parent is the verified current city;
- only the required terminal path is inspected, so a Country/AdministrativeArea Add is not blocked by
  unrelated cities/service-areas; after a language/context refresh the emptied rows make the old choice
  temporarily ineligible until the current path succeeds; pending/failed generations stay blocked.

The coordinator's id set still contains inactive ids (it is evidence, not permission); activity and
membership now come from the rows, so an active city under an INACTIVE returned area is rejected.

Strict-adapter correction (`geography-lookup-api.ts`): for `City`, an explicit `administrativeAreaId`
filter is checked FIRST and requires exactly that parent (a null parent is rejected); a country-wide
read without `knownAdministrativeAreaIds` is now `untrusted` (absent evidence is not evidence), while a
verified EMPTY set keeps null-admin cities lawful. Known ids are normalized to lower case before the
parent predicate. Routes/DTO/auth/error vocabulary unchanged.

No silent removal of stored draft selections; the server remains final authority; no new
permission/coverage rule.

## Executed focused proof (new file first, then the fix; no broad sweep)

`destination-picker-204-r2.test.ts` — 8 tests on the SAME coordinator/decoder/eligibility helper wired
into the UI: A→B→A fresh-prerequisite cases (including the failed and late-generation variants),
country-wide active-city/inactive-parent, active parent, absent parent, null-admin control, service
inheritance from the verified city only, context-refresh ineligibility, Country-not-blocked-by-child
failure, terminal-path scoping, and the decoder fail-closed matrix (filtered null parent, missing known
set, explicit empty set + null parent, foreign non-null parent). One final source-wiring assertion
supplements (does not replace) the executed evidence.

Commands actually run (frontend root):

```
node --experimental-strip-types --test features/admin-shipping-authoring/destination-picker-204-r2.test.ts -> 8/8 PASS
node ... destination-picker-204-r1.test.ts shipping-groups-204.test.ts destination-picker-204-r2.test.ts  -> 29/29 PASS
npm run typecheck                                                                                          -> PASS
node ... frontend-{feature-boundary,import-boundary,test-discovery,flat-folder}.guard.test.ts               -> 7/7 PASS
```

NOT run (explicit): the root `features/admin-shipping-authoring/*.test.ts` 68-fact sweep, nested
adapter/session suites, `frontend-source-size`, backend/Host/solution/build/discovery, package install,
browser/mounted React/visual, login or live HTTP, containers/migrations.

## Limits

Mounted React, browser, visual and live remain NOTRUN; nothing here is UI/visual/production acceptance.
One bounded harness correction was used (test-side index/ordering in the new file plus the `beginStage`
settled flag, which is production and reported above). No request/retry loop, no expectation weakening,
no unrelated scope absorbed.

Elapsed: ~14 minutes from claim to stop-new-code, then evidence/commit/push inside the 20-minute window.
