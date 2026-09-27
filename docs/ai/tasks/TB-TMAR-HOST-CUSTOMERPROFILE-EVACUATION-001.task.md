PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-HOST-CUSTOMERPROFILE-EVACUATION-001
Parent-Task: TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001-R1
Parent-Commit: 34fca8c6e89e30f007354cc15f2fadc84a64ae69
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER
Target-Host-Folder: src/backend/Host/Tooba.Host/CustomerProfile
Title: Complete Host/CustomerProfile folder evacuation and closure
Required-Skills:
1. .cursor/skills/tooba-architecture-analyze/SKILL.md
2. .cursor/skills/tooba-architecture-migrate/SKILL.md
3. .cursor/skills/tooba-architecture-certify/SKILL.md
Canonical-SoT:
- AGENTS.md
- docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
- docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
- docs/architecture/TMAR-architecture-locks.md
- docs/architecture/tmar-current-state.json
- docs/architecture/tmar-module-structure-manifests.json
- parent task/evidence for TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001
- parent repair/evidence for TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001-R1
BOUNDARY / NAVIGATION RULE
This task is strictly folder-by-folder.
Primary discovery scope:
src/backend/Host/Tooba.Host/CustomerProfile/
Do NOT scan every Host folder for CustomerProfile-related text.
Do NOT perform a capability-wide Host sweep.
Do NOT start the next Host folder.
Allowed expansion outside the target folder is ONLY:
- direct symbol/call-site references to production types found inside this folder;
- the natural destination CustomerProfile module projects required to rehome this folder's responsibilities;
- the minimum bootstrap/composition call site needed to preserve behavior;
- focused tests/guards/SoT/evidence for this folder.
The target folder must be read completely before migration.
Current known production file:
CustomerProfileDevelopmentSeed.cs
Do not assume it is the only file; enumerate this exact folder first and disposition every production file found there.
CURRENT VERIFIED DEFECT
Architect verified the current CustomerProfileDevelopmentSeed.cs:
- is physically under Host;
- directly resolves CustomerProfileDbContext;
- directly creates Tooba.CustomerProfile.Domain.CustomerProfile;
- owns CustomerProfile-specific Development seed behavior;
- references the legacy Order.Application guest actor:
  Tooba.Order.Application.Storefront.Services.StorefrontCheckoutService.StorefrontGuestActorId.
This is module-specific infrastructure/development behavior, not legitimate Host platform ownership.
Expected natural destination, unless repository evidence proves a more canonical existing path:
src/backend/Modules/CustomerProfile/Tooba.CustomerProfile.Infrastructure/Development/CustomerProfileDevelopmentSeed.cs
Expected namespace:
Tooba.CustomerProfile.Infrastructure.Development
Canonical guest actor:
Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId
PRIMARY OBJECTIVE
Finish EVERYTHING required for the exact Host/CustomerProfile folder and leave it closed:
- every production file in Host/CustomerProfile dispositioned;
- live module-specific behavior rehomed to its natural CustomerProfile owner;
- dead artifacts removed only with explicit zero-consumer/no-behavior proof;
- target Host folder production .cs count = ZERO;
- no behavior loss;
- no schema/migration changes;
- no frontend changes;
- no next Host folder started.
PHASE A — ANALYZE THIS FOLDER COMPLETELY
A1. Enumerate every production file under exactly:
src/backend/Host/Tooba.Host/CustomerProfile/
A2. Read every discovered file completely.
A3. For every type/member in the folder classify:
- responsibility;
- current dependencies;
- direct production consumers/call sites;
- runtime environment gating;
- persistence/domain authority;
- destination owner;
- behavior-preservation risk.
A4. Build a Content Disposition Map.
No file may be deleted merely because it looks like seed/residue.
A5. For CustomerProfileDevelopmentSeed specifically verify:
- all direct call sites;
- whether execution is Development-only;
- idempotency condition;
- exact actor id source;
- exact seed values;
- timestamp;
- SaveChanges behavior;
- current exception/bootstrapping behavior at its call site.
A6. Do not search unrelated Host folders. Direct symbol reference search for the target folder's discovered types is allowed because it is required to reconnect/remove them safely.
PHASE B — MIGRATE / REHOME
B1. If CustomerProfileDevelopmentSeed is live:
- move it into Tooba.CustomerProfile.Infrastructure/Development/;
- namespace must align exactly with physical path;
- keep CustomerProfile-specific DbContext/domain behavior module-owned;
- replace legacy Order.Application guest actor dependency with:
  Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId;
- add only the minimum lawful Order.Contracts project reference if not already available;
- preserve exact idempotency and seed semantics;
- preserve exact Development-only execution semantics;
- preserve exact seed data and timestamp unless a repository lock requires canonical resource extraction;
- do NOT introduce a new Host adapter just to call it.
B2. Repoint every direct production call site to the rehomed module-owned seed.
- Call count must remain semantically equivalent.
- Ordering must remain equivalent.
- Cancellation behavior must remain equivalent.
- Environment gating must remain equivalent.
- Existing try/catch/logging behavior at the bootstrap boundary must remain equivalent unless it is inside the target file itself and demonstrably noncanonical.
B3. If analysis proves the seed has ZERO production consumers:
- do NOT automatically delete it;
- first prove no runtime behavior, no intended bootstrap contract, no test/SoT dependency, and no compatibility requirement;
- only then removal-without-rehome is allowed.
  Otherwise preserve behavior by rehoming.
B4. Delete the old Host file only after all live responsibilities are rehomed/reconnected.
B5. Final target:
src/backend/Host/Tooba.Host/CustomerProfile/
contains ZERO production .cs files.
If the directory becomes empty, leaving/removing the physical empty folder is immaterial; production file count must be ZERO.
PHASE C — CUSTOMERPROFILE MODULE QUALITY FOR THIS MOVE
Audit only what this folder migration touches. Do not reopen the entire CustomerProfile module.
C1. New/moved file path↔namespace = EXACT.
C2. No Host namespace/dependency in the rehomed seed.
C3. No Order.Application dependency for guest actor.
C4. No foreign DbContext.
C5. No duplicate guest actor constant.
C6. No schema/migration changes.
C7. Do not move the seed to Application/Endpoints; Development persistence seed belongs Infrastructure.
C8. Do not create Tooba.Customer.
C9. Preserve the already-repaired CustomerProfile Result pipeline from parent R1:
- profile/dashboard business requests remain Result<T>;
- business endpoints remain api.From(result);
- business raw Results.Json(page) remains ZERO.
  C10. Preserve existing Visual Studio grouping:
  /Modules/CustomerProfile/
  with all 5 CustomerProfile projects exactly once.
  Do not flatten it.
PHASE D — DURABLE GUARDS
Add/strengthen the smallest focused guard(s) proving:
1. Host/CustomerProfile has ZERO production .cs files.
2. CustomerProfileDevelopmentSeed.cs exists in the canonical CustomerProfile Infrastructure/Development path if live.
3. Rehomed seed namespace matches physical path.
4. Rehomed seed contains no Tooba.Host.
5. Rehomed seed contains no Tooba.Order.Application.
6. Guest actor authority is StorefrontGuestActor.ActorId.
7. Old Host seed path does not exist.
8. No duplicate physical seed copy exists.
9. Parent R1 CustomerProfile Result pipeline remains canonical.
10. /Modules/CustomerProfile/ solution grouping remains exact.
Do not broaden guards to unrelated Host folders.
PHASE E — FOCUSED VALIDATION
Build only what this folder move requires:
- Tooba.CustomerProfile.Infrastructure
- Tooba.Host
- focused relevant test project(s)
Run only focused tests/guards for:
- seed idempotency / foundation if existing;
- target Host folder ZERO;
- seed ownership/path/namespace;
- direct call-site/bootstrap preservation;
- CustomerProfile parent R1 Result pipeline regression;
- CustomerProfile Solution Folder regression.
No full solution suite.
Tests are evidence, not navigation.
No open-ended repair/test loop.
At most one deterministic local repair per focused failure, then one rerun.
If a broader unrelated failure appears, report it and STOP.
PHASE F — CERTIFY THIS EXACT HOST FOLDER
PASS requires ALL:
- exact target folder fully inventoried;
- every production file dispositioned;
- Host/CustomerProfile production .cs count = 0;
- old CustomerProfileDevelopmentSeed.cs Host path absent;
- live seed behavior module-owned under CustomerProfile.Infrastructure/Development;
- direct callers reconnected;
- Development-only semantics preserved;
- idempotency preserved;
- seed values/timestamp preserved unless explicit canonical evidence says otherwise;
- legacy Order.Application guest actor dependency removed;
- canonical StorefrontGuestActor.ActorId used;
- CustomerProfile module has no new Host dependency;
- no schema/migration change;
- no frontend change;
- parent R1 Result pipeline preserved;
- parent R1 Solution Folder grouping preserved;
- focused validations PASS;
- evidence and SoT updated;
- next Host folder NOT started;
- commit pushed;
- HEAD == origin/main;
- working tree clean except explicitly reported pre-existing artifacts.
EVIDENCE
Create:
docs/evidence/TB-TMAR-HOST-CUSTOMERPROFILE-EVACUATION-001/
At minimum:
- analyze.md
- content-disposition.md
- validation.md
- closure.md
Evidence must include:
- exact before tree of Host/CustomerProfile;
- exact after tree;
- direct call-site inventory;
- before→after file/type destination;
- guest actor dependency before→after;
- idempotency/behavior parity;
- schema/migration state;
- focused validations;
- residual debt for THIS FOLDER only.
RECOVERY SOT
Update docs/architecture/tmar-current-state.json with a bounded section:
hostCustomerProfileEvacuation
Fields:
- task
- parentTask
- parentCommit
- state
- hostCustomerProfileProductionFileCountBefore
- hostCustomerProfileProductionFileCountAfter
- seedState
- seedDestination
- guestActorAuthority
- orderApplicationDependency
- behaviorParity
- schemaMigration
- resultPipelineRegressionState
- solutionGroupingRegressionState
- focusedValidations
- remainingBlockers
- residualDebt
- nextHostFolderStarted = false
- workflowStop = USER_REVIEW_HOST_CUSTOMERPROFILE_CHECKPOINT
- evidence
Do not rewrite unrelated SoT history.
Persist canonical task:
docs/ai/tasks/TB-TMAR-HOST-CUSTOMERPROFILE-EVACUATION-001.task.md
CANONICAL WORKER RESULT
Return ONLY:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-CUSTOMERPROFILE-EVACUATION-001
Parent-Task: TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Analyze-State:
Host-CustomerProfile-Production-File-Count-Before:
Host-CustomerProfile-Production-File-Count-After:
Discovered-Files:
Content-Disposition-State:
CustomerProfileDevelopmentSeed-State:
Seed-Destination:
Seed-Call-Site-State:
Development-Gating-State:
Idempotency-State:
Seed-Data-Parity-State:
Guest-Actor-Authority:
Order-Application-Dependency-State:
Host-Dependency-State:
Path-Namespace-State:
Duplicate-Seed-Copy-State:
Parent-Result-Pipeline-Regression-State:
Parent-Solution-Grouping-Regression-State:
Schema-Migration-State:
Frontend-State:
Focused-Validation:
Known-PreExisting-Failures:
Remaining-Blockers:
Residual-Debt:
SoT-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree:
User-Work-Preserved:
Next-Host-Folder-Started: false
STOP
END_TOOBA_WORKER_RESULT
STOP RULE:
After returning this result, stop completely.
Do not inspect/start the next Host folder.
Do not self-issue a follow-up task.
Do not poll.
END_TOOBA_TASK
