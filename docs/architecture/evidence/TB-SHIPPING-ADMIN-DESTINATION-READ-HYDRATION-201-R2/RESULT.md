# TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R2 — Worker Result

Mode: BOUNDED_LAWFUL_FIXTURE_AND_DESTINATION_READ_COMPOSITION_PROOF
Base SHA: `5a396dd6d91d4469cadf0e503c22e7a144742d55` (fast-forward only from `0c692f357`)
Parent: TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R1

## Production code: UNCHANGED (byte-identical)

`git diff --stat -- src/backend` shows only two TEST files. No production file in Geography, Shipping or
anywhere else was modified; the retained `serviceArea.IsActive` guard, the Contracts seam, DI, Domain,
Application, Endpoints and persistence are byte-identical to the base. No provider/assembly/timestamp
trick was used; every result below comes from a normal build of the current sources.

## 1. Exact root cause — hypothesis CONFIRMED by execution

The saved R1 reproduction was red because of its FIXTURE, not a production defect:

- two requested service areas share one city → the reader legitimately builds `owningCityIds = [City, City]`;
- that fake echoed **every** requested id, so `GetCitiesAsync` returned the same city row twice;
- `ReadVerifiedAsync.TestAdd` correctly refuses a duplicated response identity →
  `shipping.authoring.configuration_unavailable`.

Executed with the unlawful fake first: `configuration_unavailable` (same as R1). Then the fixture was made
lawful (unique ids per foreign answer, exactly like the real `GeographyPlaceSnapshotReader`'s
`DistinctIdentifiers`) and **the same activity assertions now reach and pass**:

| Test (executed) | Result |
| --- | --- |
| `Inactive_service_area_is_unresolved_while_an_otherwise_identical_active_one_resolves` | PASSED |
| `A_single_active_service_area_is_the_only_requested_key_and_still_discovers_its_whole_ancestry` | PASSED |
| `A_duplicated_foreign_response_identity_is_still_refused` | PASSED |
| `Duplicate_requested_keys_produce_one_unique_fact` | PASSED |

So the claimed "terminal-only closure is broken" is **retracted**: with a lawful foreign response, a
terminal-only request discovers country → deep admin → root admin → city → service area and resolves with
the exact canonical write shape. No second production defect exists, and none was patched.

The guard is still proven: the inactive terminal keeps its raw key with `Unresolved` and `Area = null`,
while the otherwise-identical active terminal on the SAME city resolves. The deliberate duplicate-response
negative is retained as a separate case, so `ReadVerifiedAsync` was not weakened and duplicates are never
folded into a trusted success.

## 2. Real module + EFInMemory composition (one harness)

`Tooba.Shipping.Tests.csproj` gained ONE test-only `ProjectReference` to `Geography.Infrastructure` with a
comment stating it is an integration-test composition proof only and that production stays Contracts-only.

`ShippingAuthoringDestinationReadCompositionTests` composes the REAL `GeographyModule.AddServices` +
`ShippingModule.AddServices` + canonical observability/CQRS foundations and replaces ONLY the provider
boundary (EFInMemory options for the two module contexts) and one controlled external fixture
(`ILanguageLookup`, needed by the Geography label adapter). Neither new adapter is hand-registered.

Executed results (5 passed / 0 failed):

- exactly one SCOPED `IGeographyPlaceSnapshotReader` → `GeographyPlaceSnapshotReader`, exactly one SCOPED
  `IShippingAuthoringDestinationFactsReader` → `ShippingAuthoringDestinationFactsReader`, one canonical
  `IModuleCallTracer`, same instance within a scope, different instances across scopes;
- the REAL `GeographyPlaceSnapshotReader` over a seeded real `GeographyDbContext` (seed via the module's own
  Domain factories, tracker cleared after seed): requested ids filtered, duplicate input collapsed,
  unrelated and genuinely missing ids absent, inactive country returned truthfully with `IsActive = false`,
  exact admin parents (root null, deep → root), city with a NULL admin parent kept lawful, service area →
  owning city; `ChangeTracker` empty afterwards (no tracking leaks, no writes);
- empty id collections return empty for all four axes **with a disposed/unusable context**, so the fast path
  provably performs no provider work;
- through real DI: one country lookup + its label lookup produce exactly TWO child spans, one per actual
  foreign read, with correct `Shipping`→`Geography` module tags, constant operations,
  `module_call` request kind, parent span/trace id, correlation id, `Ok` status, `Activity.Current`
  restored, no id values in tags, and the configured language reaching the label seam;
- error path: the SAME sentinel exception instance is rethrown, the span is `Error` with a type-only
  exception tag and no message/id/label/tenant data; caller cancellation propagates with the SAME
  `OperationCanceledException` instance/token value reaching the foreign seam (never an empty success).

Provider limitation stated plainly: this proves the module's real EF query behaviour over in-memory
storage, NOT PostgreSQL SQL/schema/provider parity. No external database, container, migrator or hosted
service was started.

## 3. New editor handler enrichment (real endpoint harness)

`ShippingAuthoringEditorDocumentEndpointTests` keeps its real delegates/authorizer/body reader/MediatR/
command/query/directory; only the facts port became scriptable (authoritative-shaped `Resolved` facts for
the exact submitted keys, recorded language, and a poison on any second read).

New executed fact (class total now 7 passed / 0 failed) proves: a genuine composite PUT returns 200 with
exactly ONE facts call **before** the module's own save, using the authoritative default content language;
response `destinationFacts` match the actually persisted selections one-for-one with a canonical write
shape; the response carries the real draft id and revision; a later deliberate GET that hits the poisoned
source FAILS as a read failure while revision and stamp of the committed draft do not move (so no
post-commit lookup was needed); zero submitted selections yield `destinationFacts: []`; and a stale-revision
PUT is refused with `authoring.draft.revision_conflict` at ZERO facts calls.

## 4. Commands actually run (focused only)

| Filter | Result |
| --- | --- |
| `FullyQualifiedName~ShippingAuthoringServiceAreaActivityTests` | 4 passed / 0 failed |
| `FullyQualifiedName~ShippingAuthoringDestinationReadCompositionTests` | 5 passed / 0 failed |
| `FullyQualifiedName~ShippingAuthoringDestinationFactsReaderTests` | 6 passed / 0 failed |
| the three above together | **15 passed / 0 failed** |
| `FullyQualifiedName~ShippingAuthoringEditorDocumentEndpointTests` | 7 passed / 0 failed |

`dotnet build Tooba.Shipping.Tests` (with the new references) succeeded. No storage test, no wildcard
`ShippingAuthoringEditorDocument` selection, no relational/solution/Host/browser/Postgres run. The four
previously observed connection-refused storage tests were not rerun and remain observed-blocked validation.

## 5. Bounds / deviations

Production files unchanged; only tests, the test project reference, and docs. No new project, package,
config, skill, AGENTS, manifest, baseline or guard weakening.

Deviations recorded: two bounded harness corrections were used (the `ILanguageLookup` fixture needed by the
Geography label adapter, and a fixture-constant/type name clash fixed by renaming the constants). The hard
30-minute box was reached around the capture of this evidence; the exact claim timestamp is taken from the
received task artifact and the finish time is not asserted rather than invented.

Status: completion of the authorized TEST-ONLY scope. Production-Functional-Acceptance: PENDING.
Schema/Data/Flag changes: NONE. Worker PASS != Architect ACCEPT.
