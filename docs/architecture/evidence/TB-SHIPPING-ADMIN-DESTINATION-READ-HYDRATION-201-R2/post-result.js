/* Posts the TB-201-R2 worker result to the Bridge results API (complete contract only). */
const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R2
Parent-Task: TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R1
Mode: BOUNDED_LAWFUL_FIXTURE_AND_DESTINATION_READ_COMPOSITION_PROOF
Base-SHA: 5a396dd6d91d4469cadf0e503c22e7a144742d55
Code-SHA: 9120f0cba36071a4c6748d1176819c032284c2ca
Evidence: docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R2/RESULT.md
Evidence-SHA: 9120f0cba36071a4c6748d1176819c032284c2ca
Parity: HEAD == origin/main == 9120f0cba36071a4c6748d1176819c032284c2ca
Stop-State: STOP_FOR_ARCHITECT
Production-Functional-Acceptance: PENDING
Schema-Data-Flag-Changes: NONE
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE

PRODUCTION BYTE PARITY
Production is UNCHANGED. git diff --stat -- src/backend touches only two TEST files. No Geography/Shipping runtime, Contracts,
DI, Domain, Application, Endpoints, persistence, schema or transaction file was modified; the retained serviceArea.IsActive
guard and the whole TB-201 read bridge are byte-identical to the base. No file timestamp was altered, no stale assembly was
used and no production instrumentation was added at any point; every number below comes from a normal build of current sources.

1. SOURCE HYPOTHESIS vs ACTUAL EXECUTION (root cause CONFIRMED, prior claim RETRACTED)
Hypothesis: two requested service areas share one city, so ReadAsync legitimately asks the bulk seam for [City, City]; the
R1 fake echoed every requested id and therefore answered with the SAME city row twice; ReadVerifiedAsync.TryAdd correctly
refused the duplicated response identity and returned configuration_unavailable; the real GeographyPlaceSnapshotReader
de-duplicates requested ids before querying, as its public contract requires.
Executed with the original unlawful fake: configuration_unavailable, as predicted. Then the fixture was made lawful (unique
ids per foreign answer for all four axes, matching the real reader) and the SAME activity assertions reached and passed.
Therefore the "terminal-only closure is broken / second production defect" claim is WITHDRAWN: with a lawful foreign
response, a terminal-only request discovers country -> deep admin -> root admin -> city -> service area and resolves with
the exact canonical write shape. No second production defect exists and none was patched.

2. GUARD POST-FIX PROOF (committed, executable)
ShippingAuthoringServiceAreaActivityTests, 4 passed / 0 failed:
- inactive service area (country/city/deep admin all ACTIVE, same shared city) -> raw kind/id retained, Unresolved, Area null;
- otherwise-identical ACTIVE service area on the SAME city -> Resolved with exact country/deep-admin/city/service-area shape;
- a single ACTIVE service area as the ONLY requested key (no country/city/admin keys supplied) -> full ancestry discovered
  and resolved;
- a deliberately duplicated foreign response identity -> still configuration_unavailable (ReadVerifiedAsync not weakened,
  duplicates never folded into a trusted success);
- duplicate requested keys -> one unique fact.
No red case was deleted or replaced with prose.

3. REAL EF / DI / LISTENER HARNESS (one reusable harness) - 5 passed / 0 failed
Test-only ProjectReference Shipping.Tests -> Geography.Infrastructure added ONCE with a comment that it is integration-test
composition proof only and that production stays Contracts-only. No new project or package.
REAL GeographyModule.AddServices + REAL ShippingModule.AddServices + canonical observability/CQRS foundations; ONLY the
provider boundary is replaced (EFInMemory options for the two module contexts) plus one controlled ILanguageLookup fixture the
Geography label adapter needs. Neither new adapter is hand-registered.
Descriptors: exactly ONE scoped IGeographyPlaceSnapshotReader -> GeographyPlaceSnapshotReader and exactly ONE scoped
IShippingAuthoringDestinationFactsReader -> ShippingAuthoringDestinationFactsReader; one canonical IModuleCallTracer; same
instance within a scope, different instances across scopes.
Real reader over a seeded real GeographyDbContext (module Domain factories, tracker cleared after seed): requested ids
filtered, duplicate input collapsed to unique output, unrelated and genuinely missing ids absent, inactive country returned
truthfully with IsActive=false, exact admin parents, city with NULL admin parent kept lawful, service area -> owning city,
ChangeTracker empty after the reads (no tracking leak, no write). Empty id collections return empty for ALL four axes with a
disposed/unusable context, so the fast path provably does no provider work.
Real listener through real DI: one country lookup plus its label lookup produce exactly TWO child spans, one per actual
foreign read, with Shipping->Geography module tags, the constant operations and module_call request kind, parent span/trace
id, correlation id, Ok status, Activity.Current restored, no id values in tags, and the configured language reaching the
label seam. Error branch: the SAME sentinel exception instance is rethrown with an Error span carrying only the type tag (no
message/id/label/tenant data). Cancellation: the SAME OperationCanceledException instance/same token value propagates to the
foreign seam, never an empty success.
PRECISE LIMITATION: this proves the modules' real EF query behaviour over in-memory storage, NOT PostgreSQL SQL/schema/
provider parity. No external database, container, migrator or hosted service was started.

4. ACTUAL NEW HANDLER ENRICHMENT - endpoint class now 7 passed / 0 failed
Existing real delegates/authorizer/body reader/MediatR/command/query/directory harness retained; only the facts port became
scriptable (authoritative-shaped Resolved facts for the exact submitted keys, recorded language, poison on any second read).
One fact proves: a genuine composite PUT returns 200 with EXACTLY ONE facts call BEFORE the module's own save using the
authoritative default content language; response destinationFacts match the actually persisted selections one-for-one with a
canonical write shape; the response carries the real draft id and revision; a later deliberate GET hitting the poisoned source
FAILS as a READ failure while the committed draft's revision and stamp do not move (so the successful PUT needed no
post-commit lookup); zero submitted selections yield destinationFacts [] ; and a stale-revision PUT is refused with
authoring.draft.revision_conflict at ZERO facts calls. No Unresolved-only placeholder is used as a success witness.

5. COMMANDS / FILTERS ACTUALLY RUN
dotnet build Tooba.Shipping.Tests (new references): succeeded.
FullyQualifiedName~ShippingAuthoringServiceAreaActivityTests: 4 passed / 0 failed.
FullyQualifiedName~ShippingAuthoringDestinationReadCompositionTests: 5 passed / 0 failed.
FullyQualifiedName~ShippingAuthoringDestinationFactsReaderTests: 6 passed / 0 failed.
Those three together: 15 passed / 0 failed.
FullyQualifiedName~ShippingAuthoringEditorDocumentEndpointTests: 7 passed / 0 failed (6 prior + 1 new).
No storage test, no wildcard ShippingAuthoringEditorDocument selection, no relational/solution/Host/browser/Postgres run. The
four previously observed connection-refused storage tests were NOT rerun and remain observed-blocked validation, never green.

6. DEVIATIONS / TIME
Two bounded harness corrections were used (the controlled ILanguageLookup fixture, and a fixture-constant/type name clash fixed
by renaming the constants). No temporary production diagnostics. The hard 30-minute box was reached around capturing this
evidence; the claim time is taken from the received task artifact and the finish time is not asserted rather than invented.
Changed files: two new test files, one modified test file, the test-only project reference, received task artifact, own
RESULT.md, an append-only correction in the 201-R1 RESULT, and one recovery pointer.

Worker PASS != Architect ACCEPT.
END_TOOBA_WORKER_RESULT`;

fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({
    channelId: "tooba-main",
    taskId: "TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R2",
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
