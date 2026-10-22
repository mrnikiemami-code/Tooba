PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2-R1
Parent-Task: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 0d9936f07045c9f7568a6e1f27336ac2d15ea7c2
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_PRECERT_APPROVED_SHADOW_COMPATIBILITY_GUARD_REPAIR
Hard-Timebox: 45 minutes HARD; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Resolve one demonstrated pre-Certify contradiction between the historical Shipping operational-quote compatibility test and the later explicitly approved read-only Order shadow consumer. Preserve the approved architecture by replacing the obsolete blanket prohibition with effective, tested protection of Contracts-only observation and unchanged customer authority.
This is a bounded test/decision-record repair. Do NOT remove approved shadow behavior, change production code or relax architectural boundaries to make a red test green. No Certify wave, business implementation or new user-facing integration.

MANDATORY FIRST CHECK
Fetch origin/main; inspect HEAD/branch/dirty files/stashes. A safe strictly behind main may fast-forward only to the exact expected base. Require HEAD == origin/main == Expected-Base-SHA before edits. Changed remote base, divergence or overlapping dirty files => STOP_FOR_ARCHITECT / RECOVERY_CONFLICT. No reset/rebase/stash/force-push. Preserve all unrelated work and running processes.

READ / APPLY
Read AGENTS.md, docs/ai/AGENT-SKILLS.md, repository backend-feature and architecture Analyze/Migrate/Structure/Certify skill boundaries, relevant TMAR locks and current Shipping manifest entry.
Read W0–W2 evidence and actual source, especially:

Shipping.Tests/Compatibility/OperationalQuoteCompatibilityTests.cs;
Order.Application/Storefront/Shipping/OperationalShadow/**;
the directly consuming real Order shipping projection and Order.Infrastructure/OrderModule.cs registration;
Order.Tests/Storefront/StorefrontShippingOperationalShadowObserverTests.cs;
Shipping.Contracts/Operations/ShippingOperationalQuoteContracts.cs and canonical rate/packaging/schedule public contracts.
Read the original authorizations as HISTORY ONLY (never replay their task operations):
docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-076.task.md;
docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-079.task.md, Mission A;
docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-080.task.md, Architect ruling accepting 079;
corresponding targeted evidence/planning and the current dated Shipping recovery/financial decisions.
Do not inspect the opt-in findings register or perform a whole-Host/module recovery.

ARCHITECT RULING
W2's mechanical composition extraction and structural placement are accepted for the existing foundation. Structure-State READY_FOR_CERTIFY is a structural handoff, not proof that every certification prerequisite passes. The existing OperationalQuoteCompatibilityTests failure must not be skipped during Certify.
The TB-076 guard No_module_outside_shipping_references_the_operational_quote_types predates TB-079. TB-079 Mission A expressly authorized the real Order read-only operational shadow through Contracts, default OFF, with no customer monetary/delivery authority change. TB-080 explicitly records acceptance of that bounded shadow. The current three reported consumers are:

Order.Application/Storefront/Shipping/OperationalShadow/StorefrontShippingOperationalShadowComparer.cs;
Order.Application/Storefront/Shipping/OperationalShadow/StorefrontShippingOperationalShadowObserver.cs;
Order.Infrastructure/OrderModule.cs (registration only).
Their existence is not inherently illegal coupling; consuming Shipping.Contracts is lawful. Direct Shipping Domain/Application/Infrastructure implementation consumption remains forbidden.
The approved invariant is narrower than an unrestricted public-consumer allowance and stronger than a path exclusion:
only the existing authorized observation/comparison/registration may consume the operational Contracts;
no new consumer or foreign implementation access;
independent default-OFF observation gates remain;
no change to customer quote, payable amount, order/payment draft, paid snapshot, promise or capacity reservation;
no fabricated measurement or currency/availability fallback.
This task authorizes a source-backed replacement of an obsolete temporary prohibition with guards for these already-approved semantics. It does NOT authorize an allowlist-only bypass, general exposure of operational pricing authority, baseline widening or new exceptions to Contracts-only architecture.

BOUNDED WORK

VERIFY THE EXACT CONFLICT ONCE
Independently enumerate the current non-generated production consumers and classify actual code references (ignore documentation-only text appropriately; do not miss qualified type/member usage). Read every discovered consumer and its immediate observation call site. Check project/type boundaries and actual DI. Record exact source provenance to TB-079/TB-080 and current approved behavior.
Run the known failing test once if needed to capture the concrete baseline, not an exploratory suite loop. If actual consumers/authority differ from the approved three roles or the authorization cannot be proved, stop and report BLOCKED; do not broaden the allowed set.

REPLACE THE OBSOLETE GUARD WITH REAL PROTECTION
Revise the historical test name/docs and assertions to enforce the current approved boundary. Keep the other compatibility tests and their legacy interface, nullable-unquoted-money, no money movement/migration and unenforced-capacity invariants.
The replacement must:

enumerate and compare the actual outside-Shipping operational-contract consumer set against the reviewed three roles/paths; unexpected production consumption remains a failure;
prohibit foreign Shipping Domain/Application/Infrastructure/Endpoints implementation types and evaluator imports outside the owning module, including inside those three approved files;
verify the approved OrderModule edge is registration only, and observer/comparer use only public Contracts for observation;
retain or add focused behavioral proof of observation gates and lack of customer authority. A filename whitelist, marker count, blanket Order exclusion or Assert.True is insufficient.
Reuse repository test tooling; no new package. Do not pretend string search proves arbitrary data flow: pair scoped source/semantic checks with exact source review and relevant real behavioral tests.
Make the guard detection testable and add meaningful negative cases for an extra consumer and an illegal evaluator/implementation reference INSIDE an otherwise permitted file. Tests must fail on the negative examples, not merely assert the current allowed files exist. Do not create production bad-code fixture files or commit deliberate violations.
Do not change stable Contracts APIs or remove legitimate Order observation to satisfy a historical test.
PROVE OBSERVATION SEMANTICS
Reuse the existing Order observer regressions before adding duplicates. Run focused proof for:
both default options OFF => zero quote calls; enabled without confirmed prerequisites => zero quote calls; the simple-rate gate cannot enable operational observation;
real measured grams/count and provenance forwarded to Shipping unchanged; missing/untrusted measurement never fabricated or reported as false parity;
freight mismatch, charged packaging and collect-on-delivery remain observation facts; no customer amount or payment mutation;
missing coverage/unquoted result never becomes free/allowed; carrier schedule is not a customer promise and capacity stays NotEnforced;
throwing authority is isolated; cancellation semantics preserved;
actual projection retains its original result and discards observation as customer authority, verified using the existing focused projection/observer harness and exact direct-call-source evidence.
Distinguish behavioral execution from source assertions. Do not call source pins browser/runtime parity or claim production E2E. No browser, real DB or shared server is required for this test-only repair.
If required behavioral protection cannot be demonstrated with bounded existing fixtures, report PARTIAL/BLOCKED rather than change production or claim the gate closed.
DURABLE DECISION, NO HISTORICAL REWRITE
Append one concise dated clarification to docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md, linked to the exact TB-079 Mission A/TB-080 acceptance and this repair evidence. State the reviewed Contracts-only observation allowance and forbidden price/promise authority; note why the TB-076 no-external-consumer assertion became stale. This records existing authorization, not new cutover approval.
Keep W0/W1/W2 and historic task/result evidence unchanged. No blanket declaration that a previously failing test was irrelevant; prove the replacement catches the risk the original guard protected.
Structure production surface is unchanged, so W2 structural evidence remains current. Do not modify structureCertified=false or prematurely promote the manifest/current-state. The W2 temporary non-certified-state assertion can be handled only in an explicitly authorized later Certify task; do not weaken it here.

AUTHORIZED CHANGES

src/backend/Modules/Shipping/Tooba.Shipping.Tests/Compatibility/OperationalQuoteCompatibilityTests.cs;
a cohesive adjacent Shipping.Tests compatibility helper/test only if needed for meaningful negative cases;
exact existing Order.Tests/Storefront operational-shadow/projection tests only for missing behavioral proof (no unrelated Order test or guard edits);
the specified appended Shipping decision clarification;
docs/ai/tasks/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2-R1.task.md and docs/architecture/evidence/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2-R1/**.
No production C#/csproj/project dependency, Host, frontend, Contracts, schema/migration/snapshot, data, flags, settings, manifests or certification-state changes. No other module recovery, error remapping, body-reader cleanup, owner/region/taxonomy/language implementation or financial/checkout cutover.
Default-OFF gates, PendingAllocation and all current money/promise authority remain unchanged. TB-185 stays paused.

FOCUSED VALIDATION / NO LOOP
Run the repaired Shipping operational compatibility class, its new negative detector cases, the existing exact relevant Order shadow filter, and Shipping W2 structure guards (to prove unchanged structural state). Run the narrow affected test-project compile if necessary; do not run the full Order/solution suite or broad architecture tests.
A single final Shipping module --no-build suite is allowed after the targeted fixes pass, solely to establish whether the known sole failure is resolved and detect new failures. Do not rerun that suite without a new code change or unresolved relevant concern. Report exact total counts and any unrelated failures honestly; no skip/exclusion/baseline refresh.
Each command has <=180-second explicit bound; hard45m total. Use task-owned isolated outputs if shared Host/test bins are locked; preserve exact source-root resolution for source-based guards. Never stop running apps, restart Host/DB, install packages, clear shared outputs/cache, run migrations or create DB fixtures.
One deterministic in-scope repair plus affected rerun only. Persistent failure/expansion => STOP with evidence, not an open-ended hunt.

EVIDENCE AND ACCEPTANCE
Receipt: docs/ai/tasks/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2-R1.task.md (verbatim).
Evidence: docs/architecture/evidence/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2-R1/RESULT.md plus concise provenance/consumer map and guard/behavior proof. Include old-vs-new invariant, real discovered source set, dependency checks, negative-case outcomes, tests actually run, counts, exact deltas/SHAs, preservation of flags/money/structural state and remaining business D1–D7.
PASS only if the historical contradiction is explained from actual authority, the existing failure is resolved by effective guard replacement, negative cases detect forbidden expansion, relevant observation regressions pass, and no production/state boundary changed. Missing proof => PARTIAL; unsafe recovery or actual authority conflict => BLOCKED. This is not module certification or production readiness.
Commit/push scoped changes, fetch and verify exact parity, report task-scoped cleanliness and preserve unrelated dirty/untracked work/stashes. Post one complete Bridge result, never a dummy/probe /api/results payload. Stop; do not author/run another task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2-R1
Parent-Task: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_PRECERT_APPROVED_SHADOW_COMPATIBILITY_GUARD_REPAIR
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 0d9936f07045c9f7568a6e1f27336ac2d15ea7c2
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Historic-Authorization-Provenance: ...
Actual-Consumer-Set-And-Contracts-Boundary: ...
Old-Versus-New-Guard-Invariant: ...
Unexpected-Consumer-Negative-Proof: ...
Illegal-Implementation-In-Allowed-File-Negative-Proof: ...
Default-Off-And-Observation-Only-Proof: ...
Customer-Amount-Promise-And-Capacity-Authority: ...
Focused-Commands-And-Test-Counts: ...
Final-Shipping-Suite-If-Run: ...
W2-Structural-State-Preservation: ...
Remaining-Blockers: ...
Production-Schema-Data-Flag-Changes: NONE
Changed-Files: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
Certification-State: NOT_CERTIFIED
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK