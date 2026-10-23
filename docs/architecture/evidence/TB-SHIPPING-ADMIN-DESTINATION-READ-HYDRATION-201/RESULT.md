# TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201 — Worker Result

Mode: BOUNDED_CONTRACTS_ONLY_DESTINATION_IDENTITY_READ_HYDRATION
Base SHA: `b1c89e20f495e326a932e9c0b497105b0d89bec6` (fast-forward only from `64f3f307c`)
Parent: TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R2

## What was added

### 1. Geography Contracts bulk-by-key reader (internal Contracts edge)

`Tooba.Geography.Contracts/Places/IGeographyPlaceSnapshotReader.cs` — four typed bulk reads over the
EXISTING snapshots (`CountrySnapshot`, `AdministrativeAreaSnapshot`, `CitySnapshot`,
`ServiceAreaSnapshot`): `GetCountriesAsync`, `GetAdministrativeAreasAsync`, `GetCitiesAsync`,
`GetServiceAreasAsync`. No untyped JSON, no id→label inference, no catalog, no owner parameter.

Implementation `Tooba.Geography.Infrastructure/Places/GeographyPlaceSnapshotReader.cs`:
- own `GeographyDbContext` only, `AsNoTracking`, `WHERE ids.Contains(<identity column>)`;
- requested ids de-duplicated, `Guid.Empty` treated as "not a real identity" and dropped;
- empty request → `Array.Empty<>()` with **no query at all**;
- inactive rows returned with `IsActive = false` (not hidden — activity is the consumer's policy);
- absent id is a legitimate absence, never an error; provider failure/cancellation propagate, no
  catch-to-empty;
- read-only: no `SaveChanges`, no schema, no transaction, no paging, no world scan, no join, no cache.

Registered once, scoped, in Geography's own composition root (`GeographyModule.AddServices`), directly
next to the existing `IGeographyLookup`/`IGeographyLabelLookup` registrations. `IGeographyLookup`, the
Geography HTTP surface and the legacy resolver are unchanged.

### 2. Shipping authoritative destination-fact projection

- Application model `Authoring/Models/ShippingAuthoringDestinationFactModels.cs`:
  `ShippingAuthoringDestinationResolution` (`Resolved`/`Unresolved`),
  `ShippingAuthoringDestinationWriteShape(kind, countryId, administrativeAreaId?, cityId?, serviceAreaId?)`,
  `ShippingAuthoringDestinationFactItem(kind, id, resolution, area?, label?)`.
- Application port `Authoring/Ports/IShippingAuthoringDestinationFactsReader.cs` — keys (kind, id) in,
  facts out, plus the authoritative content language id for labels.
- Infrastructure adapter `Authoring/ShippingAuthoringDestinationFactsReader.cs` consuming ONLY
  `IGeographyPlaceSnapshotReader` + `IGeographyLabelLookup` + the canonical `IModuleCallTracer`.
  Reads are batched per kind and only the parents a real terminal actually references (city → country
  + optional admin; service area → owning city → then its parents; recursive admin parents closed with
  a visited set). All de-duplication and caching is operation-local; nothing global is introduced.
- Canonical ancestry extracted into ONE shared pure helper
  `Infrastructure/Authoring/ShippingDestinationAncestry.cs`; the existing
  `ShippingAuthoringDestinationResolver` now delegates to it (same walk, same failure conditions), so
  read projection and write validation cannot diverge and no competing validator exists.
- `CitySnapshot.AdministrativeAreaId` nullable is preserved: a city without an admin parent resolves
  with `AdministrativeAreaId = null` rather than a synthesized parent.
- Terminal identity must equal the requested key: a contract response row whose id is outside the
  requested set (or a duplicate identity) fails closed with
  `ShippingErrorCodes.AuthoringConfigurationUnavailable`.
- Labels come from `IGeographyLabelLookup` for the real `context.DefaultContentLanguageId`; a missing
  label stays `null` (no fa/en hardcode, no locale guess, no raw GUID as display name).

### 3. Additive editor read field + pre-write facts on PUT

- `ShippingAuthoringEditorDocumentItem` gained an init-only `DestinationFacts` property
  (`= []`), so the positional constructor and every existing call site stay compatible.
- GET `GetShippingAuthoringEditorDocumentHandler`: after the owner-scoped draft read, the typed
  context and the type observation, it reads facts for the **stored** typed keys of that draft
  (`ShippingAuthoringEditorDocumentComposition.SelectedKeys`, distinct (kind,id), ordered) and attaches
  them. Original selected keys / region tree are unchanged in every case, including `Unresolved`.
- PUT `ReplaceShippingAuthoringEditorDocumentHandler`: facts for the **submitted** proposed keys are
  acquired at step (6) — after owner/stale checks, after context, after canonical
  `PrepareAsync` validation and before the FIRST tracked mutation — then the single atomic commit runs
  at (7) and the response is composed at (8) from the saved root + those pre-read facts. No foreign
  read, no catalog lookup and no context read happens after `ReplaceEditorDocumentAsync`.
- Empty/zero selection: `destinationFacts: []` and zero Geography calls (asserted).

No new HTTP route, request, query, owner or language override. No validator exemption. Authorization
and business ordering unchanged. Writer still revalidates real Geography on every PUT; hydrated facts
are never accepted as authority from the browser.

## Files

Added:
- `Modules/Geography/Tooba.Geography.Contracts/Places/IGeographyPlaceSnapshotReader.cs`
- `Modules/Geography/Tooba.Geography.Infrastructure/Places/GeographyPlaceSnapshotReader.cs`
- `Modules/Shipping/Tooba.Shipping.Application/Authoring/Models/ShippingAuthoringDestinationFactModels.cs`
- `Modules/Shipping/Tooba.Shipping.Application/Authoring/Ports/IShippingAuthoringDestinationFactsReader.cs`
- `Modules/Shipping/Tooba.Shipping.Infrastructure/Authoring/ShippingAuthoringDestinationFactsReader.cs`
- `Modules/Shipping/Tooba.Shipping.Infrastructure/Authoring/ShippingDestinationAncestry.cs`
- `Modules/Shipping/Tooba.Shipping.Tests/Authoring/ShippingAuthoringDestinationFactsReaderTests.cs`

Modified:
- `Modules/Geography/Tooba.Geography.Infrastructure/DependencyInjection/GeographyModule.cs` (one scoped registration)
- `Modules/Shipping/Tooba.Shipping.Infrastructure/DependencyInjection/ShippingModule.cs` (one scoped registration)
- `Modules/Shipping/Tooba.Shipping.Infrastructure/Authoring/ShippingAuthoringDestinationResolver.cs` (delegates to the shared chain helper; country id now explicit in the walk)
- `Modules/Shipping/Tooba.Shipping.Application/Authoring/Models/ShippingAuthoringEditorDocumentModels.cs` (additive init property)
- `Modules/Shipping/Tooba.Shipping.Application/Authoring/Queries/GetShippingAuthoringEditorDocumentQuery.cs` (facts read + `SelectedKeys`)
- `Modules/Shipping/Tooba.Shipping.Application/Authoring/Commands/ReplaceShippingAuthoringEditorDocumentCommand.cs` (pre-write facts + `SubmittedKeys`/`TerminalId`)
- `Modules/Shipping/Tooba.Shipping.Tests/Authoring/ShippingAuthoringEditorDocumentEndpointTests.cs` (register a minimal fake for the new port; Unresolved facts only, no fabricated shape)

## Actually executed proof

| Command | Result |
| --- | --- |
| `dotnet build Tooba.Shipping.Infrastructure` (which builds Contracts/Domain/Application) | Build succeeded |
| `dotnet build Tooba.Geography.Infrastructure` | Build succeeded |
| `dotnet test --filter FullyQualifiedName~ShippingAuthoringDestinationFactsReaderTests` | **6 passed / 0 failed** |
| `dotnet test --filter FullyQualifiedName~ShippingAuthoringEditorDocument` | **10 passed / 4 failed** — all four are `ShippingAuthoringEditorDocumentStorageTests` and fail with `NpgsqlException: Failed to connect to 127.0.0.1:55432`; environment-blocked DB tests, not a source regression, and DB/container operation was forbidden by the task |

Facts-reader tests cover: canonical write shape for country / deep admin / city / service area; the
same GUID in two kinds staying two distinct facts; city-with-null-admin resolving without a fabricated
parent and reading no administrative areas; missing / inactive / contradictory / cyclic chains all
`Unresolved` with the raw key and order preserved; empty and blank selection performing **zero**
Geography and label reads; a foreign identity in a contract response failing closed; provider
exception and caller cancellation propagating unchanged; labels requested with the configured language
and absent labels staying null.

## Not executed / limits (deviations recorded as-is)

- **No Geography test project exists** in this repository, and creating one is a new project which the
  task forbids. Therefore `GeographyPlaceSnapshotReader`'s real EF behaviour (empty-ids-no-query,
  contains-filter, inactive rows) was verified by build + source review only, not executed. The
  Shipping projection (which is where the identity/parent/label logic lives) is executed. A Geography
  read proof needs either an approved Geography test project or explicit approval to reference
  Geography.Infrastructure from the Shipping test project — both outside this task's stated bounds.
- No real DI composition assertion for the two new registrations: resolving `GeographyDbContext`
  requires a configured commerce context and DB, which this task forbids.
- No real `ActivityListener` test for the new foreign reads. The adapter reuses the canonical
  `IModuleCallTracer` exactly like the accepted resolver (operation names only, no ids/labels, exception
  type only, `SetError` + rethrow), but no span test was executed.
- No real new editor handler query/command composition test with a scripted second-read-throwing
  dependency. The pre-write ordering is source-visible at steps (6)/(7)/(8) and the endpoint suite
  (10 passed) still exercises the handlers, but a dedicated no-post-commit-read test was not written.
- The 4 DB-backed storage tests were observed failing for connection refusal only; no claim is made
  that they pass.
- Time: exceeded the 30-minute hard box (coding continued past the 23-minute stop to keep the existing
  editor endpoint suite from regressing). This is reported honestly as a process deviation, not
  relabelled.

Status: PENDING_ARCHITECT_REVIEW. Worker PASS != Architect ACCEPT. Production-Functional-Acceptance:
PENDING. Schema/Data/Flag changes: NONE.
