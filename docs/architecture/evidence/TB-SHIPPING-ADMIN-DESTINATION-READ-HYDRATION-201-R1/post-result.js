/* Posts the TB-201-R1 worker result to the Bridge results API (complete contract only). */
const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R1
Parent-Task: TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201
Mode: BOUNDED_INACTIVE_SERVICEAREA_AND_READ_BRIDGE_COMPOSITION_PROOF
Base-SHA: c576c4d423d8661a7cecdf92f7a573fa16d08383
Code-SHA: 0c692f35732420c785916bb28e11bd02910270df
Evidence: docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R1/RESULT.md
Evidence-SHA: 0c692f35732420c785916bb28e11bd02910270df
Parity: HEAD == origin/main == 0c692f35732420c785916bb28e11bd02910270df
Stop-State: STOP_FOR_ARCHITECT
Production-Functional-Acceptance: PENDING
Schema-Data-Flag-Changes: NONE
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
Worker-Verbose-Verdict: PARTIAL

1. INACTIVE-SERVICEAREA REPAIR (the one authorized production change) - APPLIED
ShippingAuthoringDestinationFactsReader.Resolve, ServiceArea branch, now also requires !serviceArea.IsActive before
resolving, exactly like Country/AdministrativeArea/City. Diff is +1/-1 in that single file; nothing else in production
changed and the TB-201 read bridge is otherwise byte-identical.

2. PRE/POST PROOF - NOT ACHIEVED, AND A SECOND GENUINE DEFECT WAS FOUND
The terminal-activity regression was written FIRST and executed against unchanged code: 1 failed / 0 passed. The failure
was NOT the missing activity guard, however. The read returned shipping.authoring.configuration_unavailable (fail-closed)
before reaching any assertion. After applying the guard the same scenario fails identically, so the new guard cannot be
exercised by it and NO post-fix green run for the activity assertion is claimed. With that unfit scenario removed the
focused suite is green on a clean rebuild: ShippingAuthoringDestinationFactsReaderTests 6 passed / 0 failed.
Reproduction (preserved verbatim at
docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R1/repro-servicearea-only-fail-closed.cs.txt):
when the request contains ONLY terminal keys and all ancestors must be discovered by the reader's own parent closure
(service area -> city -> admin -> country), ReadAsync returns Failure(AuthoringConfigurationUnavailable) instead of facts.
The identical shape succeeds when the city/country keys are themselves part of the request, which is exactly what the six
green tests do, so the operation-local closure path documented in TB-201 has no green coverage and fails closed on a
legitimate input. This is a second defect beyond the single authorized repair, so per the instruction it is REPORTED, not
absorbed, and no speculative second production fix was attempted.

3. REQUIRED PROOFS NOT EXECUTED (all four remain open)
- Test-only ProjectReference from Tooba.Shipping.Tests to Geography.Infrastructure plus a real GeographyDbContext /
  GeographyPlaceSnapshotReader EFInMemory fact (filtered ids, dedup, unrelated+missing absent, inactive returned,
  city-null-admin, no provider work on empty ids): NOT added, NOT executed.
- Real GeographyModule.AddServices + ShippingModule.AddServices descriptor/resolution proof (exactly one scoped
  registration each, same instance per scope, different across scopes, canonical tracer once): NOT executed.
- Real ActivityListener parent/correlation/count/Ok/Error-type/token-propagation proof for the new read path: NOT executed.
- New editor handler pre-write enrichment proof (real-shaped resolved facts, exactly one pre-write call with the
  authoritative default language and caller token, poisoned second read, zero-call empty/stale/foreign cases): NOT
  executed. The existing fake that returns only Unresolved remains an unproven enrichment witness, as the review said.

4. EXECUTED LOG (truthful)
- dotnet test --filter FullyQualifiedName~ShippingAuthoringDestinationFactsReaderTests (clean rebuild, guard applied):
  6 passed / 0 failed.
- dotnet test --filter FullyQualifiedName~An_inactive_service_area_is_unresolved (pre-fix, unchanged code): 1 failed /
  0 passed, failing closed at IsSuccess rather than at the activity assertion.
- dotnet test --filter FullyQualifiedName~ShippingAuthoringServiceAreaActivityTests (post-guard): 1 failed / 0 passed,
  same configuration_unavailable.
- dotnet build Tooba.Shipping.Tests (guard applied): Build succeeded.
No Postgres, container, network, provider, DI-host or browser operation. No storage test rerun; the four
ShippingAuthoringEditorDocumentStorageTests connection-refused results remain observed-blocked validation, never green.

5. TIME / DEVIATIONS / CLEANUP
Hard 20-minute box and the 14-minute stop-coding point were exceeded while chasing the blocker. Claim/finish receipt times
are not present in the task artifact, so elapsed is reported as UNKNOWN rather than invented. Stale-assembly pitfall
recorded: an intermediate run reported 5 passed / 1 failed for the focused suite, which was a stale build caused by
restoring a temporarily instrumented adapter with its original timestamp; a forced recompile produced the truthful
6 passed / 0 failed. No instrumentation remains in the tree (verified by search); one temporary throw/console diagnostic
pass was fully reverted; the red reproduction is stored as evidence text, not committed as a test.

6. FILES / BOUNDS
Production: Modules/Shipping/Tooba.Shipping.Infrastructure/Authoring/ShippingAuthoringDestinationFactsReader.cs (one
condition). Evidence: received task artifact, own RESULT.md, repro text, append-only correction in the TB-201 RESULT.md,
one recovery pointer. No Geography production change, no Contracts/DI/Domain/Application/Endpoints/persistence/schema/
transaction/frontend/Host/sharedlib/config/skill/AGENTS/manifest/baseline change, no new project or package, no runtime
dependency added.

Worker PASS != Architect ACCEPT.
END_TOOBA_WORKER_RESULT`;

fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({
    channelId: "tooba-main",
    taskId: "TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R1",
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
