PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-INVENTORY-001
Parent-Task: TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: AccessControl Host folder — first bounded inventory slice
Backend-Only: YES

Architect verdict:
TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001 is ARCHITECT-ACCEPTED at
634155b9f11352d13e301e0a058011d656229f65.

TIMEBOX:

Target <= 15 minutes.
AUDIT/MAP ONLY.
If scope exceeds the timebox, return INCOMPLETE and STOP.
No production refactor in this task.

Canonical workflow:
HOST-FIRST / FOLDER-BY-FOLDER / FILE-BY-FILE.
Do not search the whole Host for AccessControl. Work only inside the current Host folder.

Current Host AccessControl files:

AccessControlDemoSnapshot.cs
AccessControlDevelopmentSeed.cs
AccessControlEndpoints.cs
HostPlatformEffectiveAccessReader.cs

THIS TASK COVERS ONLY:

AccessControlEndpoints.cs
AccessControlDemoSnapshot.cs
HostPlatformEffectiveAccessReader.cs

Do NOT analyze AccessControlDevelopmentSeed.cs beyond recording that it remains for the next slice.

OBJECTIVE:
Produce a concrete member-level Content Disposition Map for these three files and determine the exact implementation slices needed next.

Required audit:

A. AccessControlEndpoints.cs

Inventory all route groups and endpoint families.
Classify each helper/private type/member by owner.
Identify what belongs in:
AccessControl.Endpoints
AccessControl.Application CQRS
AccessControl.Contracts
generic platform security seam
Identity.Contracts
OperatorProfile.Contracts
Catalog.Contracts
Development-only surface
Explicitly identify direct dependencies on:
AccessControl.Application/Domain,
Catalog.Application,
Identity.Application/Domain,
OperatorProfile.Application,
Host.Admin,
Host.Seller,
BuildingBlocks.
Determine the minimum new AccessControl project/layer structure required before migration.
Do not move code yet.

B. AccessControlDemoSnapshot.cs

Inventory all state/types/members.
Decide whether each item belongs to AccessControl development support, generic Development orchestration, or another owner.
Do not delete or move yet.

C. HostPlatformEffectiveAccessReader.cs

Inventory all behavior.
Decide whether this generic seam should:
remain generic Host/platform infrastructure,
move to AccessControl.Infrastructure,
or be split.
Record exact reason.
No code change.

D. Usage check
For symbols defined by these three files, perform targeted repo-wide usage search only to establish consumers/registrations/call sites.
Do NOT perform a broad module-completion scan of all Host files.

E. AccessControl module readiness
Record current physical state of:

Tooba.AccessControl.Domain
Tooba.AccessControl.Application
Tooba.AccessControl.Infrastructure
whether Contracts project exists
whether Endpoints project exists
MediatR/CQRS state
validator state
folder/namespace state

Do not repair in this task.

Evidence:
Create:
docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-INVENTORY-001/accesscontrol-first-slice-map.md

The map must contain:

file
symbol/member/responsibility
current dependencies
exact owner/destination
migration risk
consumer/call-site evidence
next bounded implementation slice

No production code changes.
No tests required.
No build required unless parsing/compile inspection absolutely requires one; prefer none.

Protected:

Fulfillment remains certified
Checkout paused
frontend frozen
no unrelated Host folder touched

PASS only if:

all three covered files are fully mapped
DevelopmentSeed is explicitly deferred, not partially modified
exact next implementation slice is small enough for <=15 minutes
no production code changed

On PASS:
Next recommendation should be ONE bounded implementation task for the first AccessControl migration slice, not the whole folder.

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-INVENTORY-001
Parent-Task: TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Covered-Files:
Deferred-File:
AccessControlEndpoints-Map-State:
DemoSnapshot-Map-State:
PlatformEffectiveAccessReader-Map-State:
Usage-Search-State:
AccessControl-Module-Physical-State:
Contracts-Project-State:
Endpoints-Project-State:
CQRS-State:
Validator-State:
Folder-Namespace-State:
Production-Code-Changes:
Evidence:
Residual-Risks:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP
Do not implement the migration automatically.
Wait for Architect verification.

END_TOOBA_TASK
