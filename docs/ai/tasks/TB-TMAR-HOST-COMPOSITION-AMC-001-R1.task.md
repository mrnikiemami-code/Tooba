PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-COMPOSITION-AMC-001-R1
Parent-Task: TB-TMAR-HOST-COMPOSITION-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Composition AMC Repair
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_COMPOSITION_NAMESPACE_REPAIR
Title: Enforce exact path↔namespace for Host/Composition and reconcile Recovery

BASELINE
Parent implementation:
aa6c98c212769a1f860e5d6cf51265ef0c32f433

Parent docs/stamp:
58e7911d9bce5bc17ee070acc212ca0fa63a15f9

PRESERVE

disposition = KEEP_AS_GENERIC_HOST_COMPOSITION_ROOT
Host/Composition remains PRESENT
exact 5-file allowlist:
ContentDevelopmentSeedHost.cs
SettingsFoundationDevelopmentSeedHost.cs
SupportDevelopmentSeedHost.cs
ToobaModuleComposition.cs
WalletDevelopmentSeedHost.cs
binder DbContext = ZERO
binder Database.MigrateAsync = ZERO
Content migrate/seed ownership remains Content.Infrastructure.Development.ContentDevelopmentSeedBootstrap
LocalizationModule remains registered as IModuleSchemaMigrator at canonical order
no business HTTP ownership
schema unchanged
frontend unchanged

R1 BLOCKER
src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs
currently declares:

namespace Tooba.Host;

This violates exact path↔namespace.

All production files directly under:
src/backend/Host/Tooba.Host/Composition/
must use:

namespace Tooba.Host.Composition;

REQUIRED REPAIR

NAMESPACE
Change ToobaModuleComposition.cs to exact namespace:
Tooba.Host.Composition

Repoint all references/usings required by this namespace change.

No shim.
No alias.
No duplicate forwarding type.
No second ToobaModuleComposition.

EXACT FOLDER INVARIANT
All five retained production files under Host/Composition must declare:
namespace Tooba.Host.Composition

Do not special-case the composition root.

BEHAVIOR PRESERVATION
Preserve exactly:
module registration order
IToobaModule composition behavior
AddToobaModules behavior
Development seed binders
Content seed bootstrap delegation
Settings/Support/Wallet composition behavior

No module ownership changes in this repair.

GUARD
Strengthen HostCompositionAmcGuardTests to prove:
exact 5-file allowlist
every retained .cs file under Host/Composition has exact namespace Tooba.Host.Composition
namespace Tooba.Host; is absent from Host/Composition
ToobaModuleComposition still contains no MapGet/MapPost/business HTTP
seed hosts remain DbContext ZERO / Database.MigrateAsync ZERO
NO REGRESSION
no new Host folder
no sink-folder regression
no foreign business authority introduced
no schema migration changes
no frontend changes
preserve user file accesscontrol-first-slice-map.md

RECOVERY / SOT — MANDATORY DoD
Update:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

Required final state:

lastAcceptedTask = TB-TMAR-HOST-COMPOSITION-AMC-001-R1
lastAcceptedCommit = <actual R1 implementation SHA>
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-COMPOSITION-AMC-001-R1
currentHostEvacuation.currentTask = TB-TMAR-HOST-COMPOSITION-AMC-001-R1
currentHostEvacuation.activeModule = Composition
currentHostEvacuation.currentHostCheckpoint = Composition
workflowStop = USER_REVIEW_HOST_COMPOSITION_AMC_001_R1_KEEP_GENERIC_HOST_COMPOSITION_ROOT
nextTask = USER_REVIEW_HOST_COMPOSITION_AMC_001_R1_KEEP_GENERIC_HOST_COMPOSITION_ROOT
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO

If docs/stamp is separate, lastAcceptedCommit MUST remain the R1 implementation SHA.

FOCUSED VALIDATION ONLY
Build:

Host
directly affected Host tests

Run:

HostCompositionAmcGuardTests
TmarDurableGuardTests

No solution-wide test run.

EVIDENCE
Create:
docs/evidence/TB-TMAR-HOST-COMPOSITION-AMC-001-R1/

Required:

blocker.md
namespace.md
behavior-parity.md
recovery.md
validation.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-COMPOSITION-AMC-001-R1.task.md

SUCCESS CRITERIA
PASS only if:

Host/Composition remains PRESENT
disposition unchanged
exact 5-file allowlist preserved
every retained file namespace = Tooba.Host.Composition
ToobaModuleComposition namespace = Tooba.Host.Composition
namespace Tooba.Host residue inside Host/Composition = ZERO
module registration behavior unchanged
binder DbContext = ZERO
binder Database.MigrateAsync = ZERO
schema/frontend unchanged
Recovery reconciled to R1
lastAcceptedCommit = actual R1 implementation SHA
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO

GIT
Work from latest origin/main.
No reset.
No clean.
No rebase.
No force push.
Preserve unrelated user work.
Commit/push main only on PASS.

CANONICAL RESULT
Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-COMPOSITION-AMC-001-R1
Parent-Task: TB-TMAR-HOST-COMPOSITION-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Composition-Disposition-State:
Host-Composition-State:
Host-Composition-File-Count:
Path-Namespace-State:
ToobaModuleComposition-Namespace-State:
Old-Namespace-Residue-State:
Module-Registration-Behavior-State:
Binder-DbContext-State:
Binder-MigrateAsync-State:
Sink-Folder-Regression-State:
Schema-Change-State:
Frontend-State:
Guard-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
Last-Accepted-Commit-State:
Stale-Current-Pointer-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Certification-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.
Do not start another Host folder.
Wait for Architect/user review.

END_TOOBA_TASK