PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2
Parent-Task: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: a41e4274789848fc89ce7895dcce597d8bc997a7
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_AMSC_STRUCTURE_EXISTING_SHIPPING_FOUNDATION
Hard-Timebox: 45 minutes HARD; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Execute the repository Structure skill on the existing Shipping backend foundation after W0 analysis and W1 canonical-error/edition repair. Independently inspect its actual physical structure and make only the minimum behavior-preserving structural corrections needed for an honest Structure verdict and durable guard. This is AMSC W2, NOT a business-feature wave or Certify.
Scope is the six existing Shipping projects and their exact composition/solution references. Do not create a replacement Shipping module, import preview models, implement deferred owner/region/taxonomy rules, re-audit other modules or certify future features.

MANDATORY FIRST CHECK
Fetch origin/main and inspect HEAD/branch/dirty files/stashes. A safe strictly behind main may fast-forward only to the exact expected base. Require HEAD == origin/main == Expected-Base-SHA before edits; different remote head/divergence/conflicting dirty files => STOP_FOR_ARCHITECT / RECOVERY_CONFLICT. No reset/rebase/stash/force-push.
The pasted W1 result named 7e6b14798de7695716fee7567baa98d7654ca478 as HEAD; current remote is its descendant a41e4274789848fc89ce7895dcce597d8bc997a7. Use the expected base, not a pasted historical SHA. Record exact code/evidence/final SHAs honestly.

REQUIRED SKILLS AND AUTHORITY
Read fully .cursor/skills/tooba-architecture-structure/SKILL.md and follow its required context, classifications, evidence and hard rules. Read AGENTS.md, docs/ai/AGENT-SKILLS.md, backend-feature skill and sibling Analyze/Migrate/Certify handoff rules; apply backend-feature for authorized structural C# changes. Do not rewrite these skills.
Read current TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md, TMAR-architecture-locks.md, TOOBA-REFERENCE-MODULE-PATTERN.md and relevant current-state/manifests/Host-closure evidence. Read W0 SOURCE-MAP.md, INPUT-VALIDATION-MATRIX.md, MODEL-LEGACY-AND-HANDOFF.md and W1 RESULT.md, ERROR-CATALOG-MAPPING.md, AUTHORIZATION-MATRIX.md plus actual changed sources/tests.
Preserve SHIPPING-ADMIN-RULES-DECISION.md sections 13–15 and dated financial decisions. No opt-in findings-register scan. Current accepted sources override stale XML/planning text.

ARCHITECT ACCEPTANCE BOUNDARY
W1's bounded implementation is accepted from source review for Structure handoff:

canonical Shipping descriptors/resources and real production registration;
legacy HTTP 400 / Business / Warning preserved for existing Shipping semantic codes;
only intended failure-message localization delta plus trusted edition/tenant validation;
no owner isolation, new schema or marketplace seller-authoring feature implemented.
Worker-reported tests: new filter 29/29 and Host catalog uniqueness 3/3. Shipping suite has the known pre-existing OperationalQuoteCompatibilityTests failure involving three Order consumers; no W1 source caused it. This is not an all-green module or final certification claim. Do not erase, exempt, baseline or repair that issue in W2. Carry it honestly to the later Architect/Certify decision if applicable.
A locked running Host prevented full link in W1. Never stop/restart it to complete W2. Use focused build/isolated task-owned output only if required; report an unavailable check.

BOUNDED STRUCTURE WORK

ACTUAL INVENTORY AND APPLICABILITY
Enumerate physical production files in Shipping.Contracts/Domain/Application/Infrastructure/Endpoints and the Shipping test project, excluding generated bin/obj. Inspect actual csproj includes, project references, module registrations and Tooba.slnx entries. Re-enumerate after any moves.
Prove HTTP_OWNING from the shipped routes and real ISender requests; internal quote/lookup seams do not justify removing genuine Endpoints. Reuse existing Rates/Packaging/Scheduling/Operations and actual capability names. No guessed capability from a command filename. Identify exact root-file sets, single-file request leaves, technical-axis-first trees, path/namespace discrepancies, stale/duplicate copies and mixed-responsibility files.
Do not infer missing foundation from no manifest entry. Existing project/layer ownership is the baseline.

MINIMUM PHYSICAL NORMALIZATION
Apply capability-first shallow structure using the Structure skill. Keep meaningful same-capability multi-type files where cohesive; no one-file-per-type ceremony or cosmetic split to game LOC limits. Count production source files, not type count, for use-case leaf rules.
Check the ShippingEndpointModule composition class currently housed inside Admin/ShippingRateCoverageEndpoints.cs. If the canonical composition-root separation is required, extract it to the module's canonical composition entry, with exact path-derived namespace and minimal consumer imports. Do not move business behavior into Host. Keep MapShippingEndpoints and AddShippingEndpointPresentation behavior and registration count unchanged.
Where another physical/mixed-file defect is proved, allow only mechanical extraction/move plus required imports/usings/project includes and directly affected tests. No algorithm, validation predicate, code string, visibility, route, DTO shape, status, resource key or DI-lifetime redesign. Avoid breaking stable public Contracts type identity. If a structural correction would require an unapproved public namespace/contract or semantic migration, stop that item with exact options.
Do not consolidate duplicated body readers or validation-code semantics merely because W0 listed them. Physical separation must preserve exact behavior; semantic deduplication requires a separately authorized Migrate repair. Do not claim final readiness while a genuine in-scope cohesion violation remains unresolved.
Stale ShippingModule/ShippingDbContext remarks may be corrected comment-only, preserving the fact that migrations are CLI-applied, not emitted at Host startup.

STRUCTURAL MANIFEST AND DURABLE GUARD
Inspect actual manifest schema and guards before editing. If Shipping has no entry, add only a repository-consistent non-certified entry for this existing module with exact physical project roots/root allowlists/forbidden files/folders. Set structureCertified = false; do not claim COMPLETE_REFERENCE_PATTERN or flip whole-module certification. If the existing manifest format cannot safely express a non-certified structural entry, report that exact constraint instead of inventing a schema.
Add/extend a Shipping-scoped executable structure guard covering real file existence, root sets, path/namespace exactness with only existing canonical migration exemptions, capability-first/no unjustified single-file request leaves, solution membership and stale duplicates as applicable.
No global regex that bans legitimate Resources/Migrations/special-purpose single-file folders. Do not weaken an existing guard, increase source-size baselines, widen Host retained-file lists or create a new certification exemption.
If updating current-state is required by existing process, append/update only the Shipping W2 structural handoff state and evidence reference, explicitly NOT_CERTIFIED / pending later Certify. Preserve all existing Host final-closure checkpoints, workflow fields and unrelated module entries exactly. Do not reserialize giant unrelated documents unnecessarily.

SOLUTION, DEPENDENCIES AND HOST CLOSURE
Verify on disk and separately in Tooba.slnx that existing Shipping projects, including Endpoints and Tests, occur once under the canonical /Modules/Shipping/ solution group. Correct only Shipping grouping/membership if needed; no assembly renaming or csproj move for appearance.
Cross-module dependencies remain Contracts-only; no foreign Application/Infrastructure/Domain/Endpoints implementation references, DbContext/DbSet, schema joins, mutable shared aggregate or new project/layer. Internal Application models/ports stay internal; Contracts holds genuine boundary semantics, not a request dump.
Host changes are limited to existing Program.cs composition/import adjustments mechanically required by a Shipping composition-class relocation. No new Host file/folder or business authority. Exact existing platform/security retained sets remain protected.

PRESERVE SEMANTIC EVIDENCE
Carry W0's complete route -> ISender request -> input provenance -> validator/policy matrix and W1 error/auth evidence forward with changed paths only. Prove no request/validator was lost, duplicated or made undiscoverable through moves; preserve the actual request universe, body-reading behavior and DI pipeline. Do not manufacture NO_VALIDATOR_REQUIRED exemptions or claim semantic certification from count/text search.
Preserve code -> descriptor -> resource/fallback ownership and the real authorizer tests/registration. ResourceManager base names/embedded resource logical names must still resolve after any relevant move.
Independent Structure verification is required; the W0 layout opinion and passing compilation are not final structural proof.

AUTHORIZED FILE SCOPE

src/backend/Modules/Shipping/**: minimum physical structure repair, exact mechanical namespace/import changes, focused guards/tests and comment-only stale-documentation correction.
src/backend/Tooba.slnx: Shipping grouping/membership only if needed.
src/backend/Host/Tooba.Host/Program.cs: minimal composition imports/calls only if required by a relocation.
Existing directly affected Host.Tests source pins/imports may be updated only to follow actual authoritative moved files while retaining equivalent checks; no weakening.
Shipping-only non-certified manifest/current-state updates as above; received task and task evidence.
No other module production changes, foreign dependency additions, migrations/schema/snapshot/data changes, DB or package operations, owner/region/mode/taxonomy/language feature implementation, frontend/Storefront/Checkout/payment changes, config or flag activation. Preserve PendingAllocation and all default-OFF gates. TB-185 stays paused.

FOCUSED VALIDATION
Run only checks applicable to actual structural edits:

new/relevant Shipping structure guards and existing touched-boundary/namespace/source-size guards;
.slnx parse/membership and manifest validity/set checks when those change;
affected project compilation and production DI/resource resolution after actual namespace/composition changes;
W1 error/resource and edition/tenant regressions if those sources/references move;
existing endpoint/validator ownership/discovery checks only as impacted.
No full solution build/test, whole Shipping suite rerun just to reproduce the known unrelated failure, broad architecture sweep, browser/server/DB/container/package startup or shared cache cleanup.
Use explicit <=180-second bounds per command and respect hard45m. Isolated build outputs must be genuinely task-owned and must not change shared output/.next or stop locked processes. No fake check success if output isolation cannot be made safe.
One deterministic local repair and affected rerun only; persistent failure or scope expansion => stop and report. Structure readiness is not all-module test success or behavioral certification.

REQUIRED EVIDENCE / VERDICT
Persist received artifact verbatim: docs/ai/tasks/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2.task.md.
Under docs/architecture/evidence/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2/ provide the Structure skill's required evidence: physical-tree-before.md, physical-tree-after.md if changed, folder-granularity.md, capability-map.md, solution-explorer.md, path-namespace.md, root-allowlist.md, stale-duplicate-copy.md, cohesion-balance.md, manifest-structure.md if applicable, validation.md, plus concise RESULT.md.
Include exact offending paths and before/after type/file mappings, applicable exemptions with authority, command/test counts, preserved semantic matrices, changed files, code/evidence/final SHAs, remaining nonstructural blockers and isolation state. Do not overwrite W0/W1 history.
Report all Structure classifications. READY_FOR_CERTIFY only if every applicable Structure gate passes. REPAIR_REQUIRED/BLOCKED/RECOVERY_CONFLICT remain valid honest outcomes; never claim readiness by downgrading a prerequisite. Known unresolved business requirements D1–D7 and the operational compatibility failure remain unimplemented/unclosed and must not be misrepresented as structurally solved.
PASS only for verified structural closure of this existing baseline; PARTIAL for missing mandatory evidence/checks/time exhaustion; BLOCKED for unsafe recovery or architectural conflict. No Certify verdict, manifest certification or release acceptance here.
Commit/push only scoped files; fetch and verify parity, preserve unrelated dirty/untracked work/stashes and report scoped cleanliness. No dummy/probe /api/results; post one complete result. Stop for Architect. Do not author/execute another task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2
Parent-Task: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_AMSC_STRUCTURE_EXISTING_SHIPPING_FOUNDATION
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Applicability: HTTP_OWNING|MIXED_OR_UNKNOWN
Folder-Granularity-State: ...
Solution-Explorer-State: ...
Path-Namespace-State: ...
Physical-Copy-State: ...
Root-Allowlist-State: ...
File-Cohesion-State: ...
Structure-State: READY_FOR_CERTIFY|REPAIR_REQUIRED|BLOCKED|RECOVERY_CONFLICT
Validator-Evidence-Preservation: ...
Error-Resources-And-Authorization-Parity: ...
Host-Closure-And-Contracts-Boundaries: ...
Manifest-State: NOT_CERTIFIED
Focused-Checks-Actually-Run: ...
Known-Nonstructural-Blockers: ...
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