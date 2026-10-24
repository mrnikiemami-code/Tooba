# TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R1 — RESULT

Task-ID: TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R1
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204
Base: `5eaf479417a1d771f6c428f242e901e35b2d76f3` (HEAD == origin/main at claim)
Mode: BOUNDED_DESTINATION_PICKER_REQUEST_AND_CHAIN_CORRECTNESS_REPAIR
Stop-State: STOP_FOR_ARCHITECT (Production-Functional-Acceptance: PENDING)

## Root cause (picker, not the group model)

The 204 picker ran four `useEffect` reads against ONE shared `epoch.current`. On a country change the
areas effect incremented the epoch and the cities effect incremented it again, so the legitimate areas
response was ALWAYS stale. Cities captured `lists.areas` from a closed-over render, excluded `areas`
from its dependency list, and passed `knownAdministrativeAreaIds: undefined` whenever the set was
empty — so `parentAccepted` accepted ANY non-null city parent. Independently:

- `choiceAncestry` took `administrativeAreaId` from `choice.city.parentId` even for an
  AdministrativeArea terminal (country+admin + null-admin city, switch level → terminal=admin,
  ancestry=null);
- `choiceChainActive` did not exist, so `isActive` was never enforced at Add (inactive rows were
  ordinary selectable options);
- area/city/service `onChange` returned `previous` on a `null` selection, so a staged parent could
  not be cleared;
- `OptionSelect` fell back to `option.code` for a blank label.

## Repair

1. **One coordinator** — `ui/destination-read-coordinator.ts` (framework-free, adapter injected):
   per-stage request token AND per-stage branch key, so siblings of the same context coexist
   (country read is never invalidated by areas/cities starting). Every context change aborts,
   invalidates and EMPTIES only the dependent stage, so stale branch rows are not selectable while
   the new parent is loading. Unmount/dispose aborts and drops late completions.
2. **Verified prerequisite** — a country-wide city read starts only after the areas read for that
   exact `language|country` succeeded; the verified id set (including a legitimately EMPTY set,
   fail-closed, never `undefined`) is the parent evidence. A failed prerequisite leaves cities with
   an empty list, a typed failure and `loading:false`; no country-wide read is attempted. A filtered
   `administrativeAreaId` read keeps its exact filter and passes no country-wide set.
3. **Terminal-path ancestry** — `choiceAncestry` now serializes an AdministrativeArea terminal from
   its OWN staged row; a City/ServiceArea terminal takes exactly its city row's verified `parentId`.
   Terminal id always equals the serialized tuple's terminal.
4. **Active chain guard** — new `choiceChainActive` re-checked at Add (and used for `canAdd`):
   country/area/city/service must be active, an area's parent must be the staged country, a city with
   a non-null parent must match the staged area, a null-admin city under an explicit area filter is
   incoherent, a service area's parent must equal the staged city. Inactive rows are rendered
   `disabled` in the selects. Stored inactive/unresolved draft selections are NOT touched by this
   repair (group chips unchanged until a deliberate Add/remove).
5. **Real clearing** — `clearAdministrativeArea` / `clearCity` / `clearServiceArea` replace the
   no-op placeholder branch, so clear-area returns to the country-wide city branch and each clear
   cascades exactly one level down.
6. **Honest label** — `optionDisplayName` renders `messages.destinationNameUnavailable` for a blank
   label; never `code`, never a GUID, no fabricated name.

Unchanged: group/title/rate/week/General collection, composed intent, ONE `PUT`, discard/reseed,
request builder, server-owned currency/authority. No backend, Contracts, DTO, route, DI, schema,
shared service, design-system, package or baseline change.

## Executed proof (real deferred promises, no React harness)

New `destination-picker-204-r1.test.ts` — 12 tests, drives the SAME coordinator the component wires:

- sibling country read applied even after areas/cities start (the exact 204 defect);
- country-wide cities start only after areas succeed, with `administrativeAreaId:null` and the
  verified id set; the real `decodeGeographyOptions` rejects a city whose parent is not in an EMPTY
  verified set (`untrusted`);
- failed prerequisite → no city read, empty rows, `loading:false`, typed failure;
- out-of-order: country A→B aborts the obsolete areas request, clears rows, and the late A response
  is dropped while B's applies;
- clear-area: aborts the filtered city read, clears city rows, keeps the verified area rows, starts
  the country-wide read, and drops the late filtered response;
- clear-city: aborts service-area read, clears rows, drops late completion;
- dispose: aborts and drops;
- ancestry: AdministrativeArea terminal with a deeper city → own admin, `cityId:null`; service-area
  terminal on a lawful null-admin city; mismatched city/area and inactive every level rejected;
- clear helpers cascade; blank label → unavailable name (asserted `!== code`, `!== id`).

`shipping-groups-204.test.ts` — 204's facts preserved (9 tests). The previous source-regex
"late-epoch guard" assertion was replaced by **204-05b**, an executed two-parent out-of-order case on
the same coordinator.

Commands actually run (frontend root):

```
node --experimental-strip-types --test features/admin-shipping-authoring/destination-picker-204-r1.test.ts   -> 12/12 PASS
node --experimental-strip-types --test features/admin-shipping-authoring/shipping-groups-204.test.ts          -> 9/9 PASS
node --experimental-strip-types --test features/admin-shipping-authoring/*.test.ts                            -> 68/68 PASS
npm run typecheck                                                                                             -> PASS
node --experimental-strip-types --test lib/architecture/frontend-{feature-boundary,import-boundary,test-discovery,flat-folder}.guard.test.ts -> 7/7 PASS
```

Matched by the root glob `features/admin-shipping-authoring/*.test.ts` (7 files):
`authoring-general-ui`, `authoring-lifecycle-202-r1`, `authoring-rates-week-203`,
`authoring-rates-week-203-r1`, `repair-200-r1`, `shipping-groups-204`, `destination-picker-204-r1`.

**NOT run (explicit):** nested `api/editor-document-adapter.test.ts`,
`session/editor-session.test.ts`, `session/editor-session-200-r2.test.ts` (not matched by that glob);
`frontend-source-size` guard (four pre-existing failures outside this task); backend/Host/solution/
build sweeps; mounted React; browser; visual; any live HTTP/login/network probe.

Component wiring was inspected but NOT mounted: `authoring-destination-picker.tsx` creates the
coordinator in one `useEffect` (disposed on unmount, StrictMode-safe), feeds it a `useMemo` context
derived from the staged choice, and renders coordinator state. async proof comes from the executed
coordinator tests, not from a mounted render.

## Limits / deviations

- Mounted-React, browser, live and Visual remain NOTRUN; no UI/visual/functional production acceptance
  is claimed from pure module tests.
- Two self-authored test-side typing/expectation corrections were made while bringing the new suite up
  (no production-expectation weakening).
- No commit/route/schema change; no new endpoint; no proof loop.

Elapsed: ~24 minutes (claim 14:28Z → result). Hard 30-minute window respected; no new coding after the
22-minute mark.
