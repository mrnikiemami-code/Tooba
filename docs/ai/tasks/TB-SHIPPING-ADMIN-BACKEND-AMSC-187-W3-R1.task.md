PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1
Parent-Task: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 2605344c10e6cd497349493357e9dcf3e6d666a4
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_MODULE_CALL_TRACING_REPAIR_AND_CERTIFICATION_CORRECTION
Hard-Timebox: 45 minutes HARD; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Close the one concrete W3 certification blocker rejected by Architect: actual Shipping outbound Geography/Fulfillment Contracts reads lack canonical IModuleCallTracer coverage. Repair only these existing call sites, prove real span/correlation/exception continuity and correct the overstated certification record. Reuse unchanged verified W3 evidence by source identity and re-check only the repaired/affected gates; do not restart Analyze/Migrate/Structure/Certify waves for the whole repository.
This task explicitly authorizes the small production observability repair missing from W3. It does not authorize product/schema work or a general telemetry redesign.

MANDATORY FIRST CHECK
Fetch origin/main and inspect HEAD/branch/dirty files/stashes. A safe strictly behind main may fast-forward only to the exact expected base. Require HEAD == origin/main == Expected-Base-SHA before edits. Changed remote base/divergence/conflicting dirty files => STOP_FOR_ARCHITECT / RECOVERY_CONFLICT. Preserve unrelated work/running processes; no reset/rebase/stash/force-push.

READ / APPLY
Read AGENTS.md, registry, tooba-backend-feature and tooba-architecture-migrate skills for the authorized repair; tooba-architecture-structure for changed-file/namespace/physical-tree verification; and tooba-architecture-certify for the focused corrective verdict. These are sequential steps inside this ONE bounded task, not permission for new waves or broad audits.
Read W3 certification.md, classification JSON/guard, W2 structural gate, W2-R1 compatibility evidence, current Shipping-only manifest/state and actual affected adapters/tests/registration.
Canonical source: src/backend/BuildingBlocks/Tooba.BuildingBlocks/Observability/Tracing/ModuleCallTracer.cs (defines IModuleCallTracer, ModuleCallTracer and ModuleCallTrace); its ToobaTracingPolicy/correlation registration; reference-only Offer.Infrastructure/Adapters/Tracing/OfferModuleCallGateways.cs. Do not copy Offer's global gateway-registration scheme blindly. Read relevant current architecture locks and Shipping decisions; no findings-register scan.

ARCHITECT RULING — W3 NOT ACCEPTED AS SUBMITTED
The Certify skill section 10 requires "cross-module calls use IModuleCallTracer decoration and trace continuity is not broken". ModuleCallTracer's own contract is explicitly for in-process cross-module calls. A shared MediatR parent span does not supply the missing module-call child span. A similar omission in a sibling certified module is not exception authority.
Therefore W3's description of this omission as "non-blocking residual / optional hygiene" is rejected. There is no approved exemption. Do not preserve COMPLETE_REFERENCE_PATTERN based on that reasoning.
Preserve historical W3 evidence as historical output, but append an explicit Architect rejection/correction and superseding outcome in this task's evidence and Shipping certification metadata. The existing product requirements D1–D7, integrated Admin runtime and release acceptance remain pending irrespective of this repair.

BOUNDED REPAIR

EXPLICIT INVALIDATION BEFORE CONDITIONAL REPAIR
Mark only Shipping's overstated certification as NOT_CERTIFIED / trace-repair pending in the existing manifest/current-state representation, preserving physical root allowlists, W2 structural evidence, unrelated module entries and Host final-closure checkpoints. Remove Shipping from any certified-module index while it is pending; no duplicated entry or unrelated history rewrite.
Record W3's rejection reason and historical lineage without deleting the original report or changing its recorded execution results. If the task later proves the repair and all affected prerequisites, promotion is allowed under step 4. If it cannot, leave the final metadata honest and non-certified.
A successful targeted repair may be committed together with the corrective final metadata; no separate remote mid-task publication is required.

CANONICAL TRACING AT THE ACTUAL OUTBOUND BOUNDARIES
Initial source-proven scope:

Shipping.Infrastructure/Rates/GeographyDestinationScopeValidator.cs:
IGeographyLookup.GetByIdsAsync.
Shipping.Infrastructure/Rates/FulfillmentShippingMethodOptionValidator.cs:
IShippingCatalogReader.ListAsync;
optional IShippingOneLevelCanonicalMethodReader.IsCanonicalMethodBindingAsync, ONLY when that branch actually executes.
Verify this bounded edge inventory from the two adapters, current registration and direct constructor callers. Do not assume "two adapters" means "two calls". If another necessary outbound call within this exact capability is found, report it and keep scope minimal; unrelated module recovery is forbidden.

Inject/use the existing canonical IModuleCallTracer on each actual foreign call, with stable source Shipping, target Geography/Fulfillment and meaningful constant operation names. Each executed foreign read gets one child span, closed after its await. No span/call for local early rejection or a branch not executed; do not combine two executed foreign calls into one inaccurate span.
Use the canonical Begin/ModuleCallTrace outcome/Dispose behavior. Preserve returned values, exact arguments, nullable results, service/option normalization, branch order, optional canonical-reader availability and original exception/cancellation propagation. Foreign read returning null/false is a completed lookup, not an invented infrastructure exception; local Shipping business rejection semantics remain unchanged. Unknown exceptions must not be caught and converted into business success/failure just to annotate telemetry.
No new ActivitySource/Meter/correlation ID/header/AsyncLocal/framework, no direct StartActivity in Application/Endpoints, no user payload, service/option identifiers, destination IDs/coordinates, tokens or exception messages as span tags/logs. Use canonical bounded semantic tags and exception TYPE only.
Do not make the tracer optional/null/no-op by production constructor default to keep old tests green. Resolve it from actual existing foundation DI; update exact affected test fixtures to explicitly provide the canonical tracer.
Prefer local instrumentation of these Shipping-owned adapters or a narrow Shipping-owned existing-boundary wrapper. Do not globally replace/decorate shared Geography/Fulfillment Contracts registrations with sourceModule=Shipping; that could misattribute other consumers and create DI/coupling regressions.
Do not add a foreign implementation/project reference or move responsibility to Host. ShippingModule DI may change only if genuinely needed for the canonical scoped services, without duplicate tracer registration or lifetime drift.

EXECUTABLE TRACE / BEHAVIOR / DI PROOF
Use the REAL ModuleCallTracer and ActivityListener/real canonical correlation context in isolated tests, not merely a spy asserting Begin was called or a source search for the interface name.
Prove actual listener-visible module-call spans have the correct target/source/operation, share the incoming trace/correlation, have the correct parent, close once and restore the parent Activity.Current.
Cover:
Geography accepted and null-result destination, exact forwarded identifiers/cancellation token and unchanged Shipping result;
Fulfillment service-level and legacy-option success, unknown service/option, canonical-binding success/false and canonical reader absent;
empty/local invalid input has zero foreign call spans; successful legacy/service path does not invoke canonical fallback; actual fallback executes two distinct Fulfillment read spans (catalog plus canonical lookup);
dependency throws: annotate through canonical SetError and rethrow the same exception; caller cancellation remains cancellation, not a business/unavailable response;
diagnostics do not leak a sentinel exception MESSAGE, submitted identifiers or PII in exported span tags; no new logging;
real registration resolution constructs both adapters with the existing foundation tracer and foreign Contracts fakes, correct scoped lifetimes, no capture/duplicate registration/cycle.
Do not use a null ActivitySource listener path as proof spans were produced. Parallel listener tests must be isolated/filter scoped so unrelated test activities do not falsify counts. No real DB/carrier/network fixture.
FOCUSED STRUCTURE / CORRECTIVE CERTIFY HANDOFF
Use Structure skill on the changed surface only. Inline changes to existing adapters normally preserve W2 layout; any new wrapper must have one cohesive home and exact namespace, preserve roots and be included in current structure proof before certification. No broad W2 repeat or manifest-root widening.
Re-check the repaired trace gate and affected DI/dependency/exception behavior independently using Certify. Carry every unchanged W3 prerequisite forward only with source/hash/diff identity plus its actual proof; explicitly state which evidence is reused versus executed now.
If any remaining W3 applicable violation becomes apparent, return NOT_CERTIFIED with the exact blocker rather than grandfather it as residual or expanding into another audit/refactor. Existing classification/error/HTTP/authorization/schema proofs must remain valid.
Conditional promotion is authorized ONLY after the actual missing call spans and all affected prerequisite checks pass. Then update the single Shipping entry and SoT to the corrected COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002 outcome with THIS task as corrective authority, W3 retained in history as rejected/superseded, W2 structural lineage preserved, exact current evidence and once-only certified-index membership.
Update only the lifecycle/authority assertions in ShippingModuleStructureGuardTests as necessary to pin this corrective provenance while preserving all physical/capability/namespace/root/solution guards. Do not use assertions accepting any state, a skip, or a false evidence pointer. If blocked, final metadata/tests must truthfully reflect the non-certified state instead of retaining a green-but-false certification claim.
Do not broaden the approved Order observation consumer set; all W2-R1 negative protections remain.

AUTHORIZED FILE SCOPE

The two named Shipping.Infrastructure/Rates adapters, minimal own ShippingModule DI adjustment if required, and a minimal cohesive own-module wrapper only if local instrumentation cannot preserve correctness;
focused Shipping.Tests trace/DI/adapter regressions and directly affected constructor fixtures, including exact Host.Tests fixture edits only if a genuine constructor use requires them;
Shipping-only corrected certification metadata/current-state and exact authority-stage test assertions;
task receipt and scoped correction evidence.
No other production module, Host production file, BuildingBlocks behavior, public Contracts/request/validator/domain/rate/coverage/packaging/schedule algorithm, project dependency, package, route or HTTP/body/error-code behavior change. No schema/migration/snapshot/data/config/flag changes, language/owner/region/taxonomy feature, frontend/Storefront/Checkout/payment/financial authority or deployment. Preserve all default-OFF gates, PendingAllocation and TB-185 pause.

FOCUSED CHECKS / HARD BOUNDS
Run affected adapter behavior tests, new REAL trace-listener/correlation/exception/DI tests, W2 structure/current certification metadata guards and existing classification/error/auth/compatibility guards as affected. Run one final Shipping module suite after targeted checks pass; no full Order/solution or broad architecture suite. If Host fixtures changed, only their exact focused filter.
Build affected projects safely using a unique task-owned isolated output when necessary; never overwrite locked running Host/shared artifacts. Each command <=180 seconds, hard45m total. No browser/server/DB/container/package/migration startup, shared cache purge or process restart.
One deterministic in-scope repair + affected rerun allowed. Persistent failure, unsafe isolation or production scope conflict => stop with PARTIAL/BLOCKED and leave NOT_CERTIFIED. Do not create an open-ended test/repair loop or lower the tracing requirement.

EVIDENCE / RESULT
Receipt: docs/ai/tasks/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1.task.md, received verbatim.
Evidence: docs/architecture/evidence/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1/RESULT.md plus concise call-site/span matrix, actual listener traces/tags sanitized, exception/DI tests, correction/promotion evidence and exact reused-vs-rerun prerequisite map. Include real commands/counts, before/after code paths, changed files, hashes/SHAs, metadata status, W3 rejection reason and hard elapsed.
PASS only for canonical per-call child spans on every executed bounded outbound edge, preserved behavior/exceptions/correlation/DI, current structural proof, all affected certification checks and truthful corrective metadata. PARTIAL for incomplete proof/timebox; BLOCKED for prerequisite/scope/recovery conflict. No remaining applicable blocker may be called optional to justify PASS.
Certify verdict may be COMPLETE_REFERENCE_PATTERN only after step 4 succeeds; otherwise NOT_CERTIFIED. Product D1–D7 and production functional acceptance remain pending even on successful architectural repair.
Commit/push only scoped files, fetch verify exact HEAD-origin parity, preserve unrelated dirty/untracked/stashed work and report scoped cleanliness. Post one complete Bridge result, never dummy/probe /api/results. Stop for Architect; no automatic next task or full AMSC restart.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1
Parent-Task: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_MODULE_CALL_TRACING_REPAIR_AND_CERTIFICATION_CORRECTION
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 2605344c10e6cd497349493357e9dcf3e6d666a4
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
W3-Architect-Rejection-And-Correction: ...
Actual-Outbound-Calls-And-Canonical-Spans: ...
Real-Listener-Parent-Correlation-And-Disposal-Proof: ...
Branch-Behavior-Exceptions-And-Cancellation: ...
PII-And-Exception-Message-Exclusion: ...
Production-DI-And-Contracts-Boundaries: ...
Changed-Surface-Structure-Proof: ...
Reused-Versus-Reexecuted-Certification-Evidence: ...
Focused-Builds-And-Test-Counts: ...
Corrective-Manifest-And-SoT-State: ...
Certification-Verdict: COMPLETE_REFERENCE_PATTERN|NOT_CERTIFIED
Exact-Remaining-Blockers: ...
Unimplemented-Product-Requirements: D1-D7_REMAIN_PENDING
Schema-Data-Flag-Changes: NONE
Changed-Files: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK