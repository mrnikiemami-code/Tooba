PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3
Parent-Task: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 8fa3b7842985575ccb611eb7411be765caee00b1
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_AMSC_CERTIFY_EXISTING_SHIPPING_BASELINE
Hard-Timebox: 45 minutes HARD; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Execute tooba-architecture-certify on the existing Shipping backend baseline after W0 Analyze, W1 canonical-error/authorization repair, W2 Structure, and W2-R1 approved-shadow compatibility reconciliation. Independently verify every applicable ARCH-COMPLETE-002 prerequisite on this actual module surface. Promote the Shipping architecture manifest/state ONLY if all required gates genuinely pass.
This is architecture certification of existing code, NOT implementation/acceptance of the new owner-scoped Admin product, marketplace seller authoring, region groups, configured-language UI, payment/carrier/free modes, cutoff or shipping-type taxonomy. Product requirements D1–D7 and integrated runtime/customer acceptance remain open regardless of this verdict.

MANDATORY FIRST CHECK
Fetch origin/main; inspect branch, HEAD, dirty/untracked files and stashes. A safe strictly behind main may fast-forward only to the exact expected base. Require HEAD == origin/main == Expected-Base-SHA before work. Changed remote base/divergence/conflicting dirty files => STOP_FOR_ARCHITECT / RECOVERY_CONFLICT. No reset/rebase/stash/force-push. Preserve running apps and unrelated work.
Read the actual received Bridge task, not a historical task as executable work. Persist receipt verbatim.

MANDATORY SKILLS / AUTHORITY
Read fully .cursor/skills/tooba-architecture-certify/SKILL.md and .cursor/skills/tooba-architecture-structure/SKILL.md; honor sibling Analyze/Migrate handoffs, AGENTS.md and docs/ai/AGENT-SKILLS.md. Read backend-feature rules for any permitted test/registration verification work, but no production feature/refactor is authorized here.
Read required current TMAR standard, locks, reference pattern, Host-evacuation protocol, relevant current-state/manifests and Host final-closure evidence. Read the exact W0/W1/W2/W2-R1 evidence and actual source rather than adopting their PASS.
Read SHIPPING-ADMIN-RULES-DECISION.md, especially sections 13–16, the approved customer UX reference, dated shipping recovery and approved financial appendix. Do not scan the opt-in findings register. Offer/other certified modules are read-only principles references, not clone/recertification scope.

STRUCTURE HANDOFF AND REVIEWED BASELINE
W2 at code SHA 59ce36250f109fec62572284049c607cd0f74c1f / evidence 0d9936f07045c9f7568a6e1f27336ac2d15ea7c2 reports READY_FOR_CERTIFY, PROFESSIONAL_SHALLOW, CANONICAL solution, EXACT namespaces, CLEAN copies, ENFORCED roots and COHESIVE files. Read its evidence and verify it remains current for this same surface; do not manufacture Structure PASS from tests or a manifest.
W2-R1 changed tests/decision evidence only, no production or structure. The obsolete TB-076 blanket ban on outside operational-contract use was replaced using the existing TB-079 authorization/TB-080 acceptance, not a general consumer exception. Its guard checks the exact three reviewed Order observer/comparer/registration roles, forbids implementation leakage inside those roles, and tests negative expansion. Preserve section 16 and these semantics.
W2-R1 reports Shipping 382/382 with no skip plus Order operational-shadow filter 26/26. They are upstream evidence, not a substitute for independent applicable Certify checks. Source checks are not browser/E2E/data-flow proof. Do not claim cancellation was behaviorally tested just from reading catch blocks.
Existing W1 semantic Shipping failures intentionally retain legacy HTTP 400 / Business / Warning with localized titles; this explicit compatibility decision is not permission to infer statuses from code suffixes. Keep all success wire contracts and Foundation-owned errors intact.

BOUNDED CERTIFICATION

APPLICABILITY, OWNERSHIP, PHYSICAL STRUCTURE
Independently enumerate actual production Contracts/Domain/Application/Infrastructure/Endpoints, actual project includes and direct consumers. Prove HTTP_OWNING from shipped routes, not project names.
Verify exact canonical capability-first shallow placement, project roots, namespaces, semantic Contracts vs internal Application models/ports, one authoritative CQRS shape, no aliases/shims, duplicate/stale copies, unjustified single-file request leaves, root dumps or cohesion violations. A file below the LOC threshold is not automatically cohesive.
Separately verify canonical /Modules/Shipping/ solution membership for existing projects, resources and composition entry. Preserve accepted migration/generated namespace exemptions only with exact authority. An absent manifest in W0 was not a missing module; use the W2 entry.

INDEPENDENT ROUTE / INPUT / VALIDATOR SET-EQUALITY GATE
Re-derive shipped route+verb and actual ISender.Send -> authoritative request/handler set from current code. For every input trace real route/query/body/header/cookie/server-context provenance, optionality, shape constraints, invoked validator/policy, stable code, pipeline discovery and failure mapping.
Do not copy the W0 16-route matrix or the W2 count of 22 validators as proof. Differentiate request validators and nested transport validators. Optional input is caller-controlled unless proved otherwise; no circular or name-based exemptions.
Require exact set equality: every shipped endpoint-reachable request exactly one classified row, all required validators actually discovered/executed, and each exception semantically justified. Preserve a durable Shipping-scoped classification manifest/guard derived against actual request reachability. A count, hand-picked type list or text pin alone is insufficient. Verify representative valid/invalid/omitted inputs through the actual validation/Result path.
If any request/input/provenance/execution is unproved, report CERTIFICATION_BLOCKED; do not repair transport/business behavior in this task.

CANONICAL RESULTS, ERRORS, LANGUAGES AND DI
Verify all shipped success/failure responses once against current ApiResponseFactory, including status, raw-body/envelope, Content-Type and Location. Do not silently convert a response or add Location. Required canonicality must be proved, not inferred from sibling precedents.
Independently derive all actual semantic error usages and canonical descriptor owners; distinguish semantic errors, FluentValidation field codes, and non-error diagnostic reason codes. The 44 declared Shipping constants are not permission to ignore additional actual emitters. Preserve composed-catalog duplicate failure; no first/last-wins or suppression. Verify configured-language resources and canonical fallback for all relevant error/validation presentation paths, real ResourceManager identities, locale resolution and independent language/currency semantics. No invented translation coverage or hardcoded two-language production limit.
Verify real production DI registration, MediatR/validation discovery, correct lifetimes, real authorizer and its trusted edition/tenant negative cases. No new default or header mode/owner authority. This proves installation context only, not seller ownership.

BOUNDARIES, DATA, HOST AND OBSERVABILITY
Inventory actual inbound/outbound module/type/project edges and inspect implementation call sites. Cross-module references must use stable Contracts only; no foreign Application/Infrastructure/Domain/Endpoints, shared mutable aggregate, foreign DbContext/DbSet/schema SQL/FK/join or transaction assumption.
Shipping owns its persistence/schema. Prove migrations, designer/snapshot files, IDs and Up/Down/index/constraint behavior unchanged against the input baseline.
Inspect canonical logging, sensitive-data risks, correlation/trace continuity and actual synchronous cross-module call tracing/IModuleCallTracer decoration where required. Absence of a second ActivitySource alone does not prove continuity. A missing required mechanism is a blocker, not a warning hidden by passing tests.
Classify actual Host residue as composition/platform/security adaptation or illegal authority. Preserve HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED/HOST_ROOT_FINAL_CERTIFIED and all nonactive retained-file sets. No Host sink or unrelated module audit.
Re-verify the exact approved Order Contracts-only observation boundary using W2-R1 guards and targeted relevant Order tests; no production consumer edit/cutover or general broadening.

DURABLE GATES AND CONDITIONAL PROMOTION
Add/extend Shipping-only certification tests/guards and complete route/input classification evidence required by the Certify skill. Guards must enforce actual sets/physical facts and block future unclassified requests, not merely freeze counts.
W2's ShippingModuleStructureGuardTests currently includes Manifest_entry_is_present_and_explicitly_non_certified with an Assert.False(structureCertified). That assertion described the W2 lifecycle stage. This task explicitly authorizes replacing ONLY that stage-specific assertion/name with a certified-state + valid Shipping certification-lineage assertion IF and ONLY IF every certification prerequisite has passed and the manifest is promoted in the same reviewed changeset. Preserve all root/dependency/namespace/solution/cohesion assertions. Do not use "true or false is fine", a skip, or remove structural protection. Before promotion keep the non-certified state; on failure do not manufacture evidence to flip it.
On genuine success, update the single Shipping manifest entry to structureCertified=true / ARCH-COMPLETE-002 with exact roots/forbidden paths and evidence; do not create a second entry or rewrite unrelated module data. Update only the Shipping certification state in tmar-current-state.json with honest lineage, gates, verdict and stop state. Preserve global Host workflow/checkpoints and all unrelated module states. Final guard run must pass against the promoted metadata.
If any applicable gate fails or production refactor is needed, keep structureCertified=false and NOT_CERTIFIED, record exact blockers and smallest bounded repair handoff, and STOP. No production fix or automatic restart of all AMSC waves.

NON-GOALS / AUTHORIZED WRITES
Allowed: focused Shipping.Tests certification/validator-coverage/structure-stage tests, exact affected Host.Tests verification only where required for composed catalog/DI; Shipping-only certification metadata as conditionally authorized; received task and scoped evidence. Minimal Order shadow regression tests only if an actual missing check is essential and bounded; existing production files remain read-only.
No production C#, project dependency, new package, DTO/API/handler/validator/domain/DI behavior change; no source refactor, body-reader consolidation, code-class semantic deduplication, owner/region/taxonomy/language feature, schema/migration/data/config/flag change, frontend/Storefront redesign, financial/checkout authority change or deployment.
Duplicate body readers/code declarations are not automatically harmless or automatically certification blockers: judge actual cohesion/canonicality once against the skill and source. If production repair is required, report it; do not fix or invent an exemption.
No customer amount, paid snapshot, promise, capacity, payer ledger, PendingAllocation or default-OFF gate change. TB-185 remains paused. Do not read hardcoded preview fa/en fixtures as store configuration.

FOCUSED VALIDATION / ISOLATION
Run only required focused affected-project builds and applicable guards/tests: Shipping module behavior + complete validator/discovery guard, W1 errors/resources/authorizer, W2 structure, W2-R1 compatibility, composed-catalog uniqueness, tracing/correlation, source-size/cohesion and relevant recovery/manifest guards. Group filters; one final Shipping module suite is sufficient, no full solution or full Order suite. Reuse upstream proof only when its scope/current source identity is verified; never label an unrun check RUN.
Every command has explicit <=180-second bound, hard45m total. Use genuinely task-owned isolated build artifacts if running Host locks shared bins; W2 used %TEMP%/tooba-w2-187-artifacts, do not reuse/clean another task's artifacts. Preserve repository-root detection in source-based guards. No shared app stop/restart, DB/network fixture, container, browser, package install, migration runner or cache purge.
One deterministic in-scope test/evidence repair plus affected rerun allowed. A production prerequisite failure or persistent required-check failure => stop, not an open-ended test/repair loop. No weakening assertion/baseline/allowlist to reach PASS.
Architecture/runtime/service-extraction claims must match proof: in-process DI/tests are not deployment or real browser acceptance.

REQUIRED EVIDENCE / STOP
Persist docs/ai/tasks/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3.task.md verbatim.
Under docs/architecture/evidence/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3/ provide certification.md/RESULT.md and the Certify skill's required evidence, including current physical tree/root sets/namespace/copy proof; actual route-request-handler-input-validator matrix and set equality; semantic boundary/Contracts map; error-code-owner-catalog-resource/fallback proof; success/failure wire contracts; logging/tracing/Host authority; schema hashes; durable guards/manifest/SoT; commands/counts/build outcomes; exact current lineage and code/evidence SHAs; known product limitations and final verdict. Keep evidence concise, sanitized and source-linked.
PASS only for all applicable ARCH-COMPLETE-002 gates and passing required checks, honest promotion and no production change. PARTIAL for incomplete mandatory evidence/timebox; BLOCKED for prerequisite violations, unsafe recovery or production-repair requirement. Certification verdict is COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002 STRUCTURE_CERTIFIED only after every required gate; otherwise NOT_CERTIFIED with exact blockers.
No whole-product acceptance or release conclusion follows PASS. D1–D7, dynamic configured-language integration, owner isolation/region groups and all new business implementation remain pending.
Commit/push only scoped changes, fetch verify HEAD-origin parity, report actual scoped cleanliness and preserve unrelated dirty/untracked/stashed work. Post one complete Bridge result, no dummy/probe /api/results. Stop for Architect; no automatic next task, repair wave, polling or implementation.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3
Parent-Task: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_AMSC_CERTIFY_EXISTING_SHIPPING_BASELINE
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 8fa3b7842985575ccb611eb7411be765caee00b1
Base-SHA: ...
Code-SHA: UNCHANGED
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Structure-Gate-Source-And-Currency: ...
Applicability-And-Endpoint-Ownership: ...
Independent-Input-Provenance-And-Set-Equality: ...
Validator-Execution-And-Durable-Guard: ...
Canonical-Results-Error-Ownership-And-Configured-Fallback: ...
DI-And-Trusted-Edition-Tenant-Authorization: ...
Contracts-Boundaries-And-No-Cross-Module-Join: ...
Logging-Correlation-And-Call-Tracing: ...
Host-Final-Closure: ...
Persistence-And-Migration-Preservation: ...
Durable-Structure-And-Certification-Guards: ...
Focused-Builds-And-Test-Counts: ...
Manifest-And-SoT-State: ...
Certification-Verdict: COMPLETE_REFERENCE_PATTERN|NOT_CERTIFIED
Exact-Blockers-And-Bounded-Repair-If-Any: ...
Unimplemented-Product-Requirements: D1-D7_REMAIN_PENDING
Production-Schema-Data-Flag-Changes: NONE
Changed-Files: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK