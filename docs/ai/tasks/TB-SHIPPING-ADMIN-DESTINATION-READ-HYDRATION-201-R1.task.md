PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R1
Parent-Task: TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_INACTIVE_SERVICEAREA_AND_READ_BRIDGE_COMPOSITION_PROOF
Expected-Base-SHA: c576c4d423d8661a7cecdf92f7a573fa16d08383
Hard-Timebox-Minutes: 20
Stop-New-Implementation-And-Tests-At-Minute: 14
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Single bounded completion

Read AGENTS/latest recovery/backend feature skill and TB201 ARCHITECT-REVIEW.md. Preserve delivered read bridge; repair ONE actual activity defect and execute the missing small integration facts. No extra feature/UI/API redesign. Backend199/frontend200 remain closed; do not replay them.

Fetch/verify HEAD==origin/main expectedbase; safe fast-forward only; preserve unrelated work/stashes. No reset/rebase/stash/force-push or Bridge dummy payload.

1. Production repair (ONE branch only)

ShippingAuthoringDestinationFactsReader.Resolve ServiceArea branch lacks !serviceArea.IsActive. Add regression FIRST against unchanged code:

inactive servicearea, ACTIVE owning city and country, valid optional/deep admin => SAME raw kind/id retained, resolution Unresolved, Area null; label may remain truthful.
active otherwise-identical servicearea remains Resolved with exact terminal/country/city/admin (nulladmin is lawful).
Execute it pre-fix to record actual failure, then add the minimal activity guard and run unchanged assertions.
No other production behavior/contract/refactor authorized. If a different genuine defect fails, report exact blocker rather than absorb a new repair.
2. Execute real Geography EF reader in EXISTING Shipping.Tests

Absence of Geography.Tests doesn't require a new project or abandoning proof.
Explicit authority: add ONE test-only ProjectReference from Tooba.Shipping.Tests.csproj to Geography.Infrastructure (plus its transitive own Domain). Comment that it is cross-module composition proof ONLY; production project references stay Contracts-only. No other project/package/new runtime dependency/new testproject.

Use the existing EFInMemory dependency and the REAL GeographyDbContext + GeographyPlaceSnapshotReader; existing module Domain factories for controlled seed. One compact parameterized fact can cover all4 methods:
requestedids filtered, requestedduplicates deduped, unrelated row absent, genuine missingid absent, inactive facts returned, parent snapshots exact incl city-nulladmin, AsNoTracking leaves no pending/tracked reads. Empty-id collections perform no provider work (test a disposed/unusable context fast path or established query interceptor; don't pretend relational command interceptor counts InMemory queries). Never SaveChanges in reader; seed setup labelled separately. Build/source evidence is NOT execution.
No real PostgreSQL, schema creation, migration, container or network. This proves own EF query behavior with controlled InMemory, not SQL/provider schema parity.

3. Actual module DI + real listener (compact reuse)

Invoke REAL GeographyModule.AddServices and ShippingModule.AddServices plus canonical observability foundation. Replace ONLY provider/data boundaries with EFInMemory contexts and required external fixtures; do NOT hand-register either new own adapter/interface. Descriptor + resolution assertions:
exactly one scoped IGeographyPlaceSnapshotReader -> GeographyPlaceSnapshotReader;
exactly one scoped IShippingAuthoringDestinationFactsReader -> ShippingAuthoringDestinationFactsReader;
same instance within scope, different across scopes, canonical tracer once. Resolve and actually read. No hosted service/migrator/provider connection started. Reuse existing ShippingAuthoringTypeCatalogCompositionDiTests pattern.

ACTUAL ActivityListener for new read path with actual parent Activity + CorrelationIdContext:
for one country lookup and one label lookup, exactly one child per actual foreign call, correct Shipping->Geography constant operation/module_call tags, parentSpanId/traceId/correlation/Ok, Activity.Current restored.
Script foreign lookup to throw named exception, assert SAME instance, Error/type-only tag, no message/id/label/tenant data in tags/events. Caller OCE/token propagation similarly with actual caller token; no fake empty success. Canonical tracer, no mock tracer/no-listener shortcut/no re-instrumentation.
One compact success/error/token fact is sufficient; don't multiply by every kind.

4. NEW editor handler pre-write enrichment proof

Extend existing nonrelational editor endpoint harness/facts (real delegates/owner authorizer/body reader/MediatR/handler/directory). Current fake returning Unresolved only does not prove the response enrichment.
Script IShippingAuthoringDestinationFactsReader to return real-shaped resolved facts for exact submitted selectedkeys and to throw named sentinel on ANY second read in one scripted sequence.
Actual composite PUT persists a real change,200; destinationFacts correspond to actual saved selections and canonical parents/label; exactly ONE pre-write facts call with authoritative defaultLanguageId and caller token; actual saved root id/revision in response. A later deliberate GET hits poisoned source and fails without moving saved revision/time/tree. This is a READ failure, not a failed PUT.
Zero submittedselections => [] and0calls; stale/foreigndraft =>0calls. Can reuse prior own owner gate tests, just measure the NEW facts boundary.

Run only precise filters

Build Shipping.Tests + references (now includes Geography implementation). Focused new facts and existing ShippingAuthoringDestinationFactsReaderTests / ShippingAuthoringEditorDocumentEndpointTests ONLY.
Do NOT use wildcard FullyQualifiedName~ShippingAuthoringEditorDocument: it selects Postgres storage tests without a server. No storage test rerun, wholeShipping/Geography/Host/solution/Architecture confidence sweep, browser/server/HTTP probe/DB/container/provider startup or download/install.
Commands<=90s, at most2 bounded harness corrections. If required facts can't finish before deadline, precisePARTIAL with unexecuted items, not broad fallback.

Exact file authority

Production ONLY the ServiceArea IsActive condition in ShippingAuthoringDestinationFactsReader.cs.
Tests: existing Shipping.Tests csproj test-only reference, existing facts/endpoint tests, ONE cohesive new Authoring read-composition/EF test file if needed. No production Geography change, Contracts/DI/Domain/Application/Endpoints/persistence/schema/transaction/frontend/Host/sharedlib/config/skill/AGENTS/manifest/baseline change.
Docs: received task verbatim; own evidence RESULT; append-only TB201 RESULT correction; one recovery pointer. Record prior6facts+10editorPASS/4storageconnectionrefused truthfully; no retroactivegreen. Preserve deadline/correction deviations; retrieve readily available claim/finish receipt times or stateUNKNOWN, don't invent.

No sharedports3000/3419/5088/5432, tooba_alpha/store-alpha/Redis/RabbitMQ/process/cache/bin/obj/.next operations or credentials. No new project/package, images or network writes besides normal scopedgit/Bridge finalResult.

20min HARD from claim; no newcoding/tests after14; reserve6 for diff/evidence/scopedcommit/push/fetchparity/result. No next task/UI issued automatically.

Result BEGIN_TOOBA_WORKER_RESULT: task/parent/mode/base/code/evidenceSHA/parity; inactive-servicearea pre/post proof; real EF/DI/listener/newhandlercounts and exact filters; precise provider limitations; test-only reference vs productionbyteparity; time/deviations/cleanup; Production-Functional-Acceptance:PENDING; Schema-Data-Flag-Changes:NONE; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.