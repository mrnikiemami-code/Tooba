PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-GOLDEN-WAVE-FINAL-CLOSURE-001
Parent-Task: TB-TMAR-INVENTORY-APPLICABILITY-REVERIFY-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: FAST-SAFE
Track: GOLDEN_WAVE_FINAL_CLOSURE
Title: Final Golden Wave Closure + Durable Recovery Snapshot + User Review Handoff
Backend-Only: YES

Architect decision

Inventory applicability reverify is accepted after direct repository verification.

Architect directly confirmed:

tmar-current-state.json now contains Inventory as:

COMPLETE_REFERENCE_PATTERN

httpApplicability = INTERNAL_ONLY

endpointOwnership = NOT_APPLICABLE

cqrs = INTERNAL_USE_CASE_BOUNDARIES

no Inventory Endpoints project exists by explicit design

no Host/Inventory directory exists

InventoryArchitectureGuardTests now include an explicit internal-only applicability guard

Offer owns seller offer-inventory HTTP and crosses into Inventory through Inventory.Contracts

current repo HEAD is the result/recovery tip after accepted Inventory implementation commit

current nextTask is this exact final-closure task

This task is FINAL CLOSURE ONLY.

Do NOT refactor product behavior.
Do NOT reopen completed modules without a concrete verified defect.
Do NOT touch Checkout/Tax/Pricing/frontend.

1. Scope

Perform a final repository-wide architecture closure for this golden wave covering:

Cart

Settlement

Fulfillment

Returns

Notification

Support

Wallet

Payment

Promotion

Offer

Inventory

Goal:
prove the current recovery state is internally consistent, durable, bootstrap-safe, and compatible with future microservice extraction.

This task is primarily verification + recovery-state closure + durable guards/evidence.

No broad rewrite.

2. Canonical expected states

HTTP-owning COMPLETE_REFERENCE_PATTERN:

Cart

Settlement

Fulfillment

Returns

Notification

Support

Wallet

Payment

Promotion

Offer

For each:

real module Endpoints project

Host maps module endpoint module

module Endpoints → ISender

real MediatR 12.5 Application handlers

Host has no legacy module-owned business endpoint implementation

Result/SemanticError expected failure flow

foreign dependencies Contracts-only

physical path↔namespace verified

Internal-only COMPLETE_REFERENCE_PATTERN:

Inventory

Inventory:

no Endpoints project by design

no Host HTTP ownership

Contracts-only participant

explicit internal-only architecture guard

microservice extraction ready without business rewrite

3. Recovery state consistency

Audit and synchronize ALL of:

docs/architecture/tmar-current-state.json

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md

They must agree on:

ARCH-COMPLETE-001

HOST-MODULE-ENDPOINT-001

ARCH-CQRS-001/002

backend-only execution mode

Checkout paused at W5

frontend frozen

current complete module set

Inventory internal-only classification

no stale reopened module in this wave

no stale next task from earlier waves

4. Final wave state

After successful closure, set:

goldenWaveState = COMPLETE

or equivalent durable field in tmar-current-state.json.

Add:

goldenWaveClosedBy = TB-TMAR-GOLDEN-WAVE-FINAL-CLOSURE-001

goldenWaveClosedCommit = <accepted closure commit>

nextTask = USER_REVIEW_GOLDEN_WAVE

Do NOT point nextTask to a code task.

The user explicitly asked:
complete this reopened module set first, then stop and call them to inspect.

Therefore final recovery state must explicitly indicate:
USER_REVIEW_REQUIRED_BEFORE_NEXT_TMAR_WAVE

No worker may automatically continue beyond this closure.

5. Durable complete-module manifest

Audit HostModuleEndpointOwnershipTests.

It must include exactly the current HTTP-owning COMPLETE modules:

Cart

Settlement

Fulfillment

Returns

Notification

Support

Wallet

Payment

Promotion

Offer

Inventory must NOT be in HTTP manifest.

Ensure each manifest entry has:

module

Endpoints project

Host map call

CQRS required

Fix stale comments/names if any still imply Offer special-case or incomplete historical state.

Keep Offer-specific legacy-route test if useful, but the canonical rule is the multi-module manifest.

6. Recovery guard hardening

Update TmarDurableGuardTests so a future stale recovery cannot regress this wave silently.

Required assertions:

tmar-current-state.json parses

all 11 modules appear with correct final state

10 HTTP modules have MODULE_ENDPOINTS + MEDIATR_12_5

Inventory has INTERNAL_ONLY + NOT_APPLICABLE + INTERNAL_USE_CASE_BOUNDARIES

reopenedModules is empty for this wave

internalApplicabilityReviewModules is empty

nextTask = USER_REVIEW_GOLDEN_WAVE

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT

frontendFrozen = true

ARCH-COMPLETE-001 marker remains

MASTER and BOOTSTRAP contain USER_REVIEW_GOLDEN_WAVE

MASTER/BOOTSTRAP contain current COMPLETE list

no authoritative stale Payment/Promotion/Offer/Inventory next-task markers remain

Do not reject historical chronology references.
Only current authoritative state must be clean.

7. Final Host authority scan

Repository-wide scan Host production code for the completed modules.

For each completed HTTP module verify Host does NOT:

implement module business HTTP route

call module business Directory directly from HTTP

own module-specific composer/query engine that performs business authority

reference module DbContext for production business path

perform module-specific Result/error translation

own cross-module business adapter masquerading as composition

Allowed Host:

Program/DI

middleware

auth/session/tenant

tiny security authorizers

hosted-service lifecycle/scheduler-only

development bootstrap explicitly allowlisted

global health/platform endpoints

Any real business authority leak:
return INCOMPLETE with exact path and module.
Do NOT hide it in final closure.

8. Cross-module reference scan

For all 11 modules:

no foreign Infrastructure reference

no foreign Application reference unless explicitly legacy and prohibited from COMPLETE (there should be none in final accepted paths)

no foreign DbContext

no cross-module SQL ownership

cross-module dependencies through Contracts/Gates/Events

Specially verify newly introduced:

Media.Contracts

Order.Contracts.Payments

Inventory.Contracts supply/retry seams

Payment.Contracts external consumers

Offer ↔ Inventory seller boundary

Do not refactor clean code merely for aesthetics.

9. Physical structure scan

For each module:

no TypeForwardedTo concealment

no namespace masquerading

no root dumping grounds

physical folder matches namespace/responsibility

Endpoints folders exist only for HTTP-owning modules

Inventory has no ceremonial Endpoints

Do not create empty folders/projects.

10. CQRS scan

For each HTTP-owning COMPLETE module:

Endpoints contains actual ISender

Application contains real IRequestHandler

MediatR version = 12.5.0 via canonical foundation

use-case folders are real

no endpoint direct Directory/business gateway bypass for HTTP use case

no fake handler that merely bounces back to Host business logic

Inventory:

explicitly exempt from HTTP CQRS because INTERNAL_ONLY

real internal application boundaries remain permitted

11. Result/error/time/id/tracing scan

Across all 11 modules verify:

no message/prose Contains/StartsWith business error classification

no HTTP ex.Message leakage

no generic unknown InvalidOperationException swallow

no PlatformHttpException in module business HTTP flow

no direct DateTimeOffset.UtcNow/DateTime.UtcNow in protected orchestration

no Guid.NewGuid/UuidV7.New bypass

no raw StartActivity in protected business flow

IClock/IIdGenerator/tracing abstractions used where applicable

Do not inspect generated migrations for these rules unless they are runtime handwritten code.

12. Behavior preservation posture

This task must make NO intentional business behavior changes.

If a guard/docs mismatch is found:
fix guard/docs.

If a real production behavior bug is found:
do NOT silently change it in final closure.
Return INCOMPLETE and identify exact repair Task-ID.

Final closure is not a feature/refactor task.

13. Validation — focused, not excessive

Required:

HostModuleEndpointOwnershipTests

TmarDurableGuardTests

architecture guard tests for:

Cart

Settlement

Fulfillment

Returns

Notification

Support

Wallet

Payment

Promotion

Offer

Inventory

final dotnet build src/backend/Tooba.slnx

Do NOT run every behavioral test suite again if architecture guards already cover closure invariants.
Do NOT run:

broad Host suite

Checkout workflow

Tax/Pricing

frontend

No duplicate reassurance testing.

14. Final evidence

Create:
docs/evidence/TB-TMAR-GOLDEN-WAVE-FINAL-CLOSURE-001/

Required:

recovery-start.md

final-module-state-matrix.md

final-host-authority-scan.md

final-cross-module-boundary-scan.md

final-cqrs-endpoint-scan.md

final-result-determinism-scan.md

final-physical-structure-scan.md

durable-guard-audit.md

recovery-state-sync.md

user-review-handoff.md

recovery-sot.md

final-module-state-matrix.md must include columns:

Module |
HTTP Applicability |
Endpoint Ownership |
CQRS |
Result Semantics |
Cross-Module Boundary |
Host Authority |
Physical State |
Microservice Extraction |
State |
Accepted Task

15. User review handoff

Create user-review-handoff.md with a concise checklist for the user.

It must say the golden wave is ready for user inspection and list the 11 final modules.

Do not claim the entire Tooba project is finished.
Only this targeted golden/reopened module wave is complete.

Explicitly preserve:

Checkout paused

Tax/Pricing outside this repair wave

frontend frozen

16. Protected state

No production behavior changes unless required to fix a verified architecture defect, in which case RETURN INCOMPLETE instead of broad repair.

Protected COMPLETE:

Cart

Settlement

Fulfillment

Returns

Notification

Support

Wallet

Payment

Promotion

Offer

Inventory

Checkout:
PAUSED_AT_SAFE_W5_CHECKPOINT

Tax/Pricing:
untouched

Frontend:
frozen

No reset.
No clean.
No force push.
No broad git add.
No stash manipulation.
Preserve user files/untracked evidence.

17. Success criteria

PASS only if ALL:

Golden-Wave-State: COMPLETE
Golden-Wave-Modules: 11_COMPLETE_REFERENCE_PATTERN
Golden-Wave-HTTP-Modules: 10_MODULE_ENDPOINTS_MEDIATR_12_5
Golden-Wave-Internal-Modules: INVENTORY_INTERNAL_ONLY
Golden-Wave-Host-Business-Authority: NONE_FOR_ACCEPTED_MODULES
Golden-Wave-CrossModule-Boundaries: CONTRACTS_ONLY
Golden-Wave-Result-Semantics: STABLE
Golden-Wave-Physical-State: VERIFIED
Golden-Wave-Microservice-Extraction: READY
Durable-Endpoint-Guard: ENFORCED
Durable-Recovery-Guard: ENFORCED
Recovery-State: CURRENT_AND_MACHINE_READABLE
Recovery-Next-Task: USER_REVIEW_GOLDEN_WAVE
Checkout-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Frontend-Production-Changes: NONE
User-Review-Handoff: READY

18. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Golden-Wave-State
Golden-Wave-Modules
Golden-Wave-HTTP-Modules
Golden-Wave-Internal-Modules
Golden-Wave-Host-Business-Authority
Golden-Wave-CrossModule-Boundaries
Golden-Wave-Result-Semantics
Golden-Wave-Physical-State
Golden-Wave-Microservice-Extraction
Durable-Endpoint-Guard
Durable-Recovery-Guard
Final-Module-State-Matrix
Final-Host-Authority-Scan
Final-CrossModule-Boundary-Scan
Final-CQRS-Endpoint-Scan
Final-Result-Determinism-Scan
Final-Physical-Structure-Scan
Focused-Validation
Skipped-Validation
Full-Validation
Residual-Defects
Recovery-State
Recovery-Next-Task
Current-State-Manifest
User-Review-Handoff
Checkout-State
Tax-State
Pricing-State
Frontend-Production-Changes
Git
Blockers
User-Work-Preserved
Next-Recommended-Task

If clean:
Next-Recommended-Task: USER_REVIEW_GOLDEN_WAVE

If defect found:
Next-Recommended-Task: TB-TMAR-GOLDEN-WAVE-FINAL-CLOSURE-001-R1

After Result:
STOP completely.
Do NOT start any next TMAR wave.
Do NOT poll.
Do NOT fetch another task.
Do NOT write Worker IDLE.

END_TOOBA_TASK