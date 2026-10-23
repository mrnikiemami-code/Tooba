# TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R1 — Worker Result (PARTIAL)

Mode: BOUNDED_INACTIVE_SERVICEAREA_AND_READ_BRIDGE_COMPOSITION_PROOF
Base SHA: `c576c4d423d8661a7cecdf92f7a573fa16d08383` (fast-forward only from `e0c2a2651`)
Parent: TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201

## 1. Production repair (the ONE authorized change) — DONE

`ShippingAuthoringDestinationFactsReader.Resolve`, ServiceArea branch, now also requires the terminal
itself to be active:

```csharp
if (!serviceAreas.TryGetValue(key.Id, out var serviceArea) || !serviceArea.IsActive
    || !cities.TryGetValue(serviceArea.CityId, out var city) || !city.IsActive
    || !countries.TryGetValue(city.CountryId, out var country) || !country.IsActive)
{
    return Unresolved(key, label);
}
```

This is the only production change (`+1/-1`). No other production behaviour, contract or refactor.

## 2. Pre-fix / post-fix execution — NOT PROVEN, and a NEW blocker was found

A terminal-activity regression was written FIRST and executed against the unchanged code:

- Pre-fix: `1 failed / 0 passed`. **However the failure was not the missing activity guard**: the read
  returned `shipping.authoring.configuration_unavailable` (fail-closed) before reaching the assertion.
- Post-guard: identical `configuration_unavailable` failure. The guard therefore cannot be exercised by
  that scenario, so **no post-fix green run for the activity assertion can be claimed**.
- After removing the unfit scenario the focused suite is green on a clean rebuild:
  `ShippingAuthoringDestinationFactsReaderTests` → **6 passed / 0 failed**.

Root cause of the blocked scenario (reproducible, preserved verbatim as
`repro-servicearea-only-fail-closed.cs.txt`): when the request contains **only terminal keys** and all
ancestors must be discovered by the reader itself (service area → city → admin → country), the read
returns `Failure(AuthoringConfigurationUnavailable)` instead of facts. The same shape succeeds when the
city/country keys are also part of the request, which is what the currently green tests do. So the
operation-local parent closure path documented in TB-201 fails closed on a legitimate input. This is a
second, genuine defect beyond the single authorized repair, so per the task instruction it is reported
rather than absorbed.

## 3. Required proof that was NOT executed

All four remaining blocks stayed unexecuted because the box was consumed by the blocker above:

- Test-only `ProjectReference` from `Tooba.Shipping.Tests` to `Geography.Infrastructure` + real
  `GeographyDbContext`/`GeographyPlaceSnapshotReader` EFInMemory fact (ids filtered, dedup, unrelated
  and missing absent, inactive returned, city-null-admin, no provider work on empty ids) — NOT added,
  NOT executed.
- Real `GeographyModule.AddServices` + `ShippingModule.AddServices` descriptor/resolution proof
  (single scoped registrations, same instance per scope, different across scopes) — NOT executed.
- Real `ActivityListener` parent/correlation/count/Ok/Error-type/token propagation proof for the new
  read path — NOT executed.
- New editor handler pre-write enrichment proof (real-shaped resolved facts, exactly one pre-write call
  with authoritative default language and caller token, poisoned second read, zero-call empty/stale/
  foreign cases) — NOT executed. The existing fake returning only `Unresolved` remains unproven as an
  enrichment witness, as the review said.

So the three proof gaps the Architect listed are still open.

## 4. Truthful execution log

| Command | Result |
| --- | --- |
| `dotnet test --filter FullyQualifiedName~ShippingAuthoringDestinationFactsReaderTests` (clean rebuild, guard applied) | **6 passed / 0 failed** |
| `dotnet test --filter FullyQualifiedName~An_inactive_service_area_is_unresolved` (pre-fix, unchanged code) | 1 failed / 0 passed — failed closed at `IsSuccess`, not at the activity assertion |
| `dotnet test --filter FullyQualifiedName~ShippingAuthoringServiceAreaActivityTests` (post-guard) | 1 failed / 0 passed — same `configuration_unavailable` |
| `dotnet build Tooba.Shipping.Tests` (guard applied) | Build succeeded |

No Postgres/container/network/provider/DI-host/browser operation was performed. No storage test was run;
the four `ShippingAuthoringEditorDocumentStorageTests` connection-refused results remain observed-blocked
validation, never green.

## 5. Process deviation — recorded, not hidden

- The hard 20-minute box and the 14-minute stop-coding point were exceeded while chasing the blocker.
- **Stale-assembly pitfall:** an intermediate run reported `5 passed / 1 failed` for the focused suite.
  That was a stale build: a temporary diagnostic instrumentation of the adapter was restored with its
  original timestamp, so MSBuild skipped recompiling and the instrumented binary was tested. Forcing the
  source timestamp and rebuilding produced the truthful `6 passed / 0 failed`. No instrumentation remains
  in the repository (verified by search) and no green claim is derived from any instrumented build.
- One additional diagnostic exercise (temporary throw/console markers) was applied and fully reverted.
- Claim/finish receipt times are not available from the task artifact, so elapsed is stated as
  `UNKNOWN` rather than invented.

## 6. Files

- Modified (production): `Modules/Shipping/Tooba.Shipping.Infrastructure/Authoring/ShippingAuthoringDestinationFactsReader.cs` — one condition.
- Added (evidence only): `repro-servicearea-only-fail-closed.cs.txt` (the exact failing scenario, not committed as a test because it is red against the current defect).

No production Geography change, no Contracts/DI/Domain/Application/Endpoints/persistence/schema/
transaction/frontend/Host/sharedlib/config/skill/AGENTS/manifest/baseline change. The delivered TB-201
read bridge is byte-identical apart from the one guard line.

Status: PARTIAL. Production-Functional-Acceptance: PENDING. Schema/Data/Flag changes: NONE.
Worker PASS != Architect ACCEPT.

---

## Append-only correction — TB-201-R2 (2026-10-23)

The "second genuine defect" reported above is **withdrawn**. It was a FIXTURE defect, not a production
defect: the saved reproduction's fake echoed every requested id, so when two service areas shared one city
the reader's legitimate [city, city] request produced two identical city rows and
`ReadVerifiedAsync` correctly refused the duplicated response identity. The real
`GeographyPlaceSnapshotReader` de-duplicates requested ids, as its contract requires.

Executed proof in R2: with the fixture made lawful and the SAME activity assertions, an inactive service
area stays `Unresolved` with its raw key/Area = null, the otherwise-identical active one on the same
city resolves, a terminal-only request discovers its whole ancestry, and a deliberately duplicated foreign
response is still refused. So the guard is exercised, terminal-only closure is NOT broken, and no second
production change was made or needed. See
`../TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R2/RESULT.md`.
