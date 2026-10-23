/* Posts the TB-201 worker result to the Bridge results API (complete contract only). */
const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201
Parent-Task: TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R2
Mode: BOUNDED_CONTRACTS_ONLY_DESTINATION_IDENTITY_READ_HYDRATION
Base-SHA: b1c89e20f495e326a932e9c0b497105b0d89bec6
Code-SHA: __CODE_SHA__
Evidence: docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201/RESULT.md
Evidence-SHA: __EVIDENCE_SHA__
Parity: HEAD == origin/main == __CODE_SHA__
Stop-State: STOP_FOR_ARCHITECT
Production-Functional-Acceptance: PENDING
Schema-Data-Flag-Changes: NONE
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE

WIRE FIELD / READ SURFACE (additive only)
ShippingAuthoringEditorDocumentItem gains an init-only property DestinationFacts (default []), preserving the positional
constructor and every existing call site. Each entry is {kind, id, resolution: Resolved|Unresolved, area: canonical write
shape or null, label: localized string or null} for one DISTINCT (kind,id) key of that draft. The region tree itself is
unchanged in every case, including Unresolved. No new HTTP route, request, query, owner or language override; the four
existing editor routes and their query/command contracts are untouched apart from this additive response property.

GEOGRAPHY READER AUTHORITY / DI / CONTRACTS EDGE
New GeoContracts/Places/IGeographyPlaceSnapshotReader with four typed bulk reads over the EXISTING CountrySnapshot,
AdministrativeAreaSnapshot, CitySnapshot and ServiceAreaSnapshot. Implementation uses ONLY the module's own
GeographyDbContext with AsNoTracking and WHERE ids.Contains(<identity column>); requested ids are de-duplicated and
Guid.Empty is dropped as "not a real identity"; an empty request returns Array.Empty with no query at all; inactive rows
are returned with IsActive=false rather than hidden; an absent id is a legitimate absence; provider failure and caller
cancellation propagate unchanged (no catch-to-empty). Read-only: no SaveChanges, schema, transaction, paging, world scan,
join or cache. Registered once with scoped lifetime in Geography's own GeographyModule.AddServices beside the existing
lookups. IGeographyLookup, the Geography HTTP surface and the legacy resolver are unchanged, so there is no new Geography
route and no write seam.

SHIPPING PROJECTION
Application model + port (Resolved/Unresolved, canonical write shape, localized label) and Infrastructure adapter
consuming ONLY the Geography Contracts reader, IGeographyLabelLookup and the canonical IModuleCallTracer. Reads are
batched per kind and only the parents a real terminal references (city -> country + optional admin; service area ->
owning city -> its parents; recursive admin parents closed with a visited set); de-duplication/caching is operation-local
only. CitySnapshot.AdministrativeAreaId nullable is preserved: a city without an admin parent resolves with null and no
synthesized parent, and such a key triggers no administrative-area read. The canonical admin-chain rule was extracted into
ONE shared helper (ShippingDestinationAncestry) that the existing write resolver now delegates to, with the expected
country id explicit in the walk; no competing validator and no duplicate rule. A contract row whose identity is outside the
requested set, or a duplicate identity, fails closed with AuthoringConfigurationUnavailable. Labels use the real
context.DefaultContentLanguageId and stay null when genuinely absent; no fa/en hardcode, locale guess or raw GUID label.

PRE-WRITE-ONLY ORDERING
GET: owner-scoped draft read first, then typed context, then type observation, then facts for the STORED typed keys of that
draft, then mapping. PUT: owner/foreign-404 and stale-revision checks, then typed context, then canonical PrepareAsync on
the whole proposed tree, then ALL destination facts for the SUBMITTED keys, then the single atomic ReplaceEditorDocumentAsync,
then the response composed from the saved root plus those pre-read facts. No foreign read, catalog lookup or context read
runs after the commit. Zero/empty selection yields destinationFacts [] and zero Geography reads. Writer authority is
unchanged: every PUT still revalidates real Geography; hydrated facts are never accepted from a caller.

ACTUALLY EXECUTED
dotnet build Tooba.Shipping.Infrastructure (builds Contracts/Domain/Application): Build succeeded.
dotnet build Tooba.Geography.Infrastructure: Build succeeded.
dotnet test --filter FullyQualifiedName~ShippingAuthoringDestinationFactsReaderTests: 6 passed / 0 failed.
dotnet test --filter FullyQualifiedName~ShippingAuthoringEditorDocument: 10 passed / 4 failed, where all four failures are
ShippingAuthoringEditorDocumentStorageTests throwing NpgsqlException "Failed to connect to 127.0.0.1:55432" - DB-backed
tests in an environment with no Postgres, not a source regression; DB/container operation was forbidden by the task.
Proven in the focused facts tests: canonical write shape for country/deep admin/city/service area; the same GUID in two
kinds staying two distinct facts; null-admin city resolved without a fabricated parent and with no administrative-area
read; missing/inactive/contradictory/cyclic chains all Unresolved with the raw key and order preserved; empty and blank
selection performing ZERO Geography and label reads; foreign identity in a contract response failing closed; provider
exception and caller cancellation propagating unchanged; labels requested with the configured language and absent labels
staying null.

NOT EXECUTED / LIMITS (deviation, reported as-is)
No Geography test project exists in the repository and creating one is a forbidden new project, so the Geography reader's
real EF behaviour was proven by build + source review only, not executed. No real DI-resolution assertion for the two new
registrations (needs a configured commerce context/DB). No real ActivityListener span test for the new foreign reads: the
adapter reuses the canonical tracer with operation names only, no ids/labels, exception type only, SetError + rethrow, but
no span was asserted. No dedicated scripted second-read-throwing handler test; pre-write ordering is source-visible at
steps (6)/(7)/(8) and the endpoint suite still exercises both handlers. The four DB-backed storage tests were observed
failing for connection refusal and no passing claim is made for them.

FILES / TIME / DEVIATIONS
Added: Geography Contracts IGeographyPlaceSnapshotReader, Geography Infrastructure GeographyPlaceSnapshotReader,
Shipping Application destination-fact model + port, Shipping Infrastructure facts adapter + ShippingDestinationAncestry
helper, focused facts test file. Modified: GeographyModule DI (one scoped registration), ShippingModule DI (one scoped
registration), destination resolver (delegates to the shared chain helper; country id explicit), editor document model
(additive init property), editor GET query (facts + SelectedKeys), editor PUT command (pre-write facts + SubmittedKeys/
TerminalId), editor endpoint test (minimal fake for the new port returning Unresolved only, no fabricated shape) plus docs.
No Geography domain/catalog/data/migration/HTTP change; no Shipping domain/persistence/DbContext/schema/migration/
transaction change; no Host/frontend/sharedlib/new project/package/config/flag change. The 30-minute hard box was exceeded
while keeping the existing editor endpoint suite from regressing; recorded honestly, not relabelled. One harness/test
correction pass was applied inside the new test file (two assertion/fixture fixes) before it went green, plus the required
fake registration in the existing endpoint test file.

Worker PASS != Architect ACCEPT.
END_TOOBA_WORKER_RESULT`;

fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({
    channelId: "tooba-main",
    taskId: "TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201",
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
