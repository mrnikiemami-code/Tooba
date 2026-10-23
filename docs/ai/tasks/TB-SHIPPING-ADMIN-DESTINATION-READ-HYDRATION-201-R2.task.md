PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R2
Parent-Task: TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_LAWFUL_FIXTURE_AND_DESTINATION_READ_COMPOSITION_PROOF
Expected-Base-SHA: 5a396dd6d91d4469cadf0e503c22e7a144742d55
Hard-Timebox-Minutes: 30
Stop-New-Implementation-And-Tests-At-Minute: 23
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Mission: finish the read bridge, no speculative production repair

Read AGENTS/latest recovery/backend feature skill and201-R1 ARCHITECT-REVIEW.md. Retain the already-applied serviceArea.IsActive guard. Production code is FROZEN for this task; fix the unlawful reproduction fixture and execute compact missing integration proof. No UI202 or new feature.

Fetch/verify HEAD==origin/main expectedbase, safe fast-forward only; preserve unrelated work/stashes, no reset/rebase/stash/force-push. Don't modify file timestamps to evade builds, use stale assemblies, or instrument production with throw/Console. No Bridge dummy result.

1. Exact root cause and live regression (first)

Restore saved repro-servicearea-only-fail-closed.cs.txt as an EXECUTABLE test with its two service areas sharing one city.
Trace ONLY fixture calls/responses in the test:
ReadAsync owningCityIds = [City,City].
CountingSnapshots.GetCitiesAsync currently maps both ids and returns duplicateCityRows.
ReadVerifiedAsync correctly rejects duplicate response identity.
Actual GeographyPlaceSnapshotReader.DistinctIdentifiers deduplicates input as the public contract requires.
This is the source-backed hypothesis; verify it, don't accept it merely from Architect prose.

Execute the saved scenario with the original unlawful fake: capture configuration_unavailable and the duplicate city response. Then make fixture output lawful with distinct requested ids for all bulk methods and keep the SAME activity assertions:
inactive servicearea -> rawkey retained/Unresolved/Area null;
otherwise-identical active servicearea -> exact Resolved parent shape.
Also exercise ONE active servicearea as the ONLY requested key (no country/city/admin keys preloaded) -> discovers all ancestors and resolves.
Keep a separate deliberate duplicate-parent-response negative -> configuration_unavailable. Do not weaken ReadVerified or fold duplicates into trusted success.
Retain these tests committed; don't delete a red case and substitute prose. Do not claim the old activity pre-fix failure was observed; it failed earlier for another reason.

If the lawful fixture STILL fails, preserve red test and exact requestedids/returnedids/callstage; STOP without another production patch or open-ended diagnosis. Do not assert blanket terminal-only-closure defect from an invalid fake.

2. ONE reusable real-module / EFInMemory composition harness

Complete original201 EF + DI obligations together rather than separate frameworks.
Already-authorized ONE test-only ProjectReference: Shipping.Tests -> Geography.Infrastructure, with a comment stating integration-test purpose. No new project/package and no production cross-module implementation reference.

Invoke REAL GeographyModule.AddServices + ShippingModule.AddServices + canonical foundations.
Replace ONLY provider/data boundaries with existing EFInMemory contexts and necessary controlled external fixtures; do not hand-register either own new reader adapter. Existing197 ShippingAuthoringTypeCatalogCompositionDiTests is the pattern; don't start hosted services/migrators/tenant DB connections.
Assert exactly one SCOPED descriptor each:
IGeographyPlaceSnapshotReader -> GeographyPlaceSnapshotReader;
IShippingAuthoringDestinationFactsReader -> ShippingAuthoringDestinationFactsReader;
same instance within scope, different across scopes; canonical tracer once. Resolve and READ.

Seed own GeographyDbContext with existing Domain factories (or established test seed pattern), using controlled real entities including inactive/unrelated rows and nullable-admin city. Call REAL GeographyPlaceSnapshotReader for all four typed bulk methods: requestedids filtered, duplicateinput yields uniqueoutput, unrelated/missing absent, inactive fact truthful, exactparents. AsNoTracking read leaves tracker clean after seed is detached. Empty ids fast-path works with unusable/disposed context or proper query hook; don't claim relational command counts on InMemory. This proves real EF query behavior, NOT PostgreSQL SQL/schema parity.

3. Real listener, compact reuse

For DI-resolved Shipping facts reader, real ActivityListener + actual parent Activity + CorrelationIdContext:
one country lookup plus one label lookup => one stopped child per actualforeignread, correct constant Shipping->Geography operations/module_call/correlation/parentTraceId+SpanId/Ok, Activity.Current restored.
Use controlled IGeographyLabelLookup fixture as a DATA boundary if needed, don't re-register the own adapters.
Separate error/cancel branches may use a controlled foreign snapshot fixture with REAL adapter/canonical tracer: SAME sentinel exception rethrown; Error/type-only/no message/id/label/tenant tags or events; OCE SAME instance and caller token VALUE preserved. No mock/no-listener tracer, no production patch.

4. Actual NEW handler enrichment, reuse endpoint harness

Extend existing nonrelational ShippingAuthoringEditorDocumentEndpointTests harness; real delegates/authorizer/body reader/MediatR/command/query/directory remain.
Script destinationFacts port with actual-shaped Resolved facts for exact submittedkeys, authoritative defaultLanguageId recorded, and throw on ANY second ReadAsync in the sequence.
A genuine composite PUT ->200, exactly1 facts read BEFORE own save, matching response destinationFacts and actual persisted draftid/revision/selection; a subsequent deliberate GET hits the poisoned source and fails without changing committed tree/revision/time. No postcommit lookup.
Stale/foreign owner -> zero calls to facts port.
Empty selection -> destinationFacts[] and zero FOREIGN Geography/label reads (the own facts port may be called with[]; do not demand zero own-port invocations).
No fake success from Unresolved-only placeholder. No DB-backed storage test needed; transaction/persistence code unchanged.

Allowed files / verification

Production files unchanged, including Geography/Shipping runtime, servicearea guard, Contracts/DI/Domain/Application/Endpoints/persistence/schema/transaction/frontend/Host.
Only Shipping.Tests csproj test-only reference; existing facts/endpoint tests; ONE cohesive new Authoring EF/DI/listener test file if needed. No new packages/projects/config/skill/AGENTS/manifest/baseline/guard weakening.
Docs: receivedtaskverbatim; own RESULT.md; append-only201-R1 result correction separating fake failure from unproved production defect; one recovery pointer.

Build Shipping.Tests+references ONCE with current new test sources; then exact nonrelational class/method filters. No wildcardEditorDocument selecting storage, wholeShipping/Geography/Host/solution/Architecture suite, repeated confidence races, browser/server/HTTPprobe/Postgres/container/DB/seed/provider startup.
InMemory test seeding is explicitly authorized; no EXTERNAL provider startup. No downloads/install/shared5432/tooba_alpha/store-alpha/apps3000/3419/5088/Redis/RabbitMQ/process/cache/bin/obj/.next/credentials.

Commands<=120s, at mostTWO bounded harness corrections; if exhausted precisePARTIAL, don't extend the box. No temporary production diagnostics. If project setup alone blocks for5min, stop with exact compile/DI blocker, don't spend the whole box chasing it.

30min HARD, stop newcoding/tests at23; reserve7 for diff/evidence/scopedcommit/push/fetchparity/result.
Result BEGIN_TOOBA_WORKER_RESULT: ids/base/code/evidence/parity; source hypothesis vs ACTUAL unlawful/lawful fixture executions; guard post-fix proof; realEF/DI/listener/handler counts with precise providerlimits; changedfiles/productionbyteparity; commands/filter counts; UTC/deviations; Production-Functional-Acceptance:PENDING; Schema-Data-Flag-Changes:NONE; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.