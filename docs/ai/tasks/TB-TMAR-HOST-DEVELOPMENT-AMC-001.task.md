PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-HOST-DEVELOPMENT-AMC-001
Parent-Task: TB-TMAR-HOST-CUSTOMERPROFILE-EVACUATION-001
Parent-Commit: 8060ba603f96af3f4f5da88da801539faf46af65
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER
Shortcut: AMC
Target-Host-Folder: src/backend/Host/Tooba.Host/Development
Title: Analyze → Migrate → Certify Host/Development with explicit retained-Host exception
ARCHITECTURE DECISION
Host/Development is an explicit Host ownership exception when code is genuinely:
- Development-only;
- platform/composition/runtime bootstrap;
- cross-module startup orchestration;
- free of module business policy/state ownership.
Therefore this folder is NOT required to reach ZERO merely because it is under Host.
For this exact folder, PASS means:
- every production file is completely read and dispositioned;
- every retained file is proven to be a legitimate Development Host seam;
- no module-specific business/persistence responsibility remains unless it is explicitly part of startup migration/bootstrap composition permitted by TMAR-HOST-EVACUATION-PROTOCOL.md;
- no unrelated cleanup or next-folder work begins.
Do NOT invent a Development module.
Do NOT move files to another Host folder merely to make this folder empty.
Do NOT create a generic shared/god bootstrap layer.
Do NOT scan the whole Host.
CURRENT VERIFIED INVENTORY
Architect verified exactly these current production files:
1. MarketplaceDevelopmentBootstrap.cs
2. MarketplaceAdminDevBootstrap.cs
3. MarketplaceSellerDevBootstrap.cs
Re-enumerate the exact folder yourself before work; repository reality wins.
CURRENT ARCHITECT CLASSIFICATION
A. MarketplaceDevelopmentBootstrap.cs
Expected disposition: RETAIN IN HOST if repository reality confirms it remains only a Development marketplace startup/composition root.
It may:
- select marketplace edition/context;
- establish marketplace CommerceContext for bootstrap;
- orchestrate module schema migration/bootstrap calls;
- invoke module-owned development seeds;
- sequence development-only setup.
It must NOT:
- implement module business rules;
- mutate module domain state by embedding module-specific business decisions;
- contain module-specific CQRS/business policy;
- become a permanent home for seed logic that a module can own itself.
Direct DbContext migration calls are accepted for THIS Development composition seam under the canonical protocol's explicit allowance for migration/development bootstrap, provided the file only orchestrates Database.MigrateAsync() and does not inspect/mutate module tables/domain state itself.
B. MarketplaceAdminDevBootstrap.cs
Expected disposition: RETAIN IN HOST if it is only a Development platform/admin runtime authorization bootstrap.
Permitted:
- fixed Development actor identity;
- generic IAuthorizationTupleWriter;
- platform synthetic tenant relationship required by Host admin runtime.
  Not permitted:
- Admin module business policy/state;
- module persistence;
- domain mutations;
- production behavior.
C. MarketplaceSellerDevBootstrap.cs
Expected disposition: RETAIN IN HOST if it is only a Development seller runtime/context bootstrap.
Permitted:
- fixed Development seller actor/party identifiers;
- generic authorization tuple write;
- publishing Host seller development snapshot through the existing Host seller runtime seam.
  Not permitted:
- Seller business policy/state ownership;
- foreign module persistence;
- production behavior.
The above are Architect expectations, not blind instructions. If actual repository evidence contradicts them, return RECOVERY_CONFLICT rather than inventing architecture.
REQUIRED SKILLS
1. .cursor/skills/tooba-architecture-analyze/SKILL.md
2. .cursor/skills/tooba-architecture-migrate/SKILL.md
3. .cursor/skills/tooba-architecture-certify/SKILL.md
MANDATORY SOT
Read:
- AGENTS.md
- docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
- docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
- docs/architecture/TMAR-architecture-locks.md
- docs/architecture/tmar-current-state.json
- current active Skills
SCOPE RULE
Analyze/migrate/certify ONLY:
src/backend/Host/Tooba.Host/Development/
Outside-folder reads are allowed only for:
- direct references/call sites of the three discovered files;
- direct types they depend on when needed to classify ownership;
- focused tests/guards/evidence/SoT.
Do NOT perform a whole-Host sweep.
Do NOT inspect the next Host folder.
Do NOT broaden into Seller/Admin recovery.
PHASE A — ANALYZE
1. Enumerate the exact folder.
2. Read every production file completely.
3. Build member-level Content Disposition Map.
4. For each file classify:
   - DEVELOPMENT_HOST_COMPOSITION
   - DEVELOPMENT_HOST_RUNTIME_SEAM
   - MODULE_SPECIFIC_RESPONSIBILITY
   - MIXED_RESPONSIBILITY
   - DEAD_ARTIFACT
5. Verify exact environment gating and production reachability.
6. Verify all direct dependencies and direct consumers.
7. Explicitly separate:
   - orchestration of module-owned work;
   - implementation of module-owned work.
8. Flag any code that:
   - reads/writes module tables beyond generic schema migration;
   - creates module domain entities;
   - embeds module-specific seed data;
   - owns module business decisions;
   - uses foreign persistence for anything beyond startup migration orchestration.
PHASE B — MIGRATE / CLEAN ONLY IF NEEDED
For each file:
- If classification is pure DEVELOPMENT_HOST_COMPOSITION or DEVELOPMENT_HOST_RUNTIME_SEAM, RETAIN it in Host/Development.
- If module-specific responsibility is embedded, extract ONLY that responsibility to its natural owner and leave the Host file thin.
- If mixed, split by responsibility.
- If dead, delete only with explicit zero-consumer/no-behavior proof.
Do not move legitimate retained development seams out of Host.
Specific constraints:
MarketplaceDevelopmentBootstrap
- preserve Edition=Marketplace gating;
- preserve marketplace connection/context assignment semantics;
- preserve migration ordering;
- preserve seed/bootstrap ordering;
- preserve cancellation behavior as currently observable;
- preserve Development-only invocation;
- no schema/migration content changes;
- no module data/business logic added;
- module-specific seed logic must remain module-owned where already available.
MarketplaceAdminDevBootstrap
- preserve DefaultAdminActor;
- preserve synthetic marketplace tenant semantics;
- preserve authorization tuple behavior;
- preserve current unavailable-writer behavior;
- no production behavior.
MarketplaceSellerDevBootstrap
- preserve DefaultSellerParty and DefaultSellerActor;
- preserve authorization tuple behavior;
- preserve snapshot publication semantics;
- preserve current unavailable-writer behavior;
- no production behavior.
QUALITY CHECKS
Even retained Host exception files are not quality-exempt.
Verify:
- no ex.Message classification;
- no unexpected exception swallowing beyond already accepted development fail-soft semantics;
- no module business policy;
- no module-owned seed data in this folder;
- no direct module domain entity creation;
- no foreign DbContext use except the explicitly allowed Database.MigrateAsync() startup migration composition in MarketplaceDevelopmentBootstrap;
- no cross-module SQL/EF joins;
- no duplicated guest actor authority;
- no production route/API ownership;
- no schema/migration changes;
- no frontend changes;
- path↔namespace consistent with current Host convention;
- no stale/duplicate physical copies.
CERTIFY FINAL FOLDER STATE
Re-enumerate Host/Development at the end.
PASS may retain files, but each retained file MUST have:
- exact retained classification;
- exact architecture justification;
- proof that it is Development-only;
- proof that it contains no misplaced module business/persistence authority outside the explicit migration-bootstrap exception.
Expected healthy end-state if no hidden defect is found:
- MarketplaceDevelopmentBootstrap.cs
  → RETAINED_ALLOWED_DEVELOPMENT_COMPOSITION
- MarketplaceAdminDevBootstrap.cs
  → RETAINED_ALLOWED_DEVELOPMENT_RUNTIME_SEAM
- MarketplaceSellerDevBootstrap.cs
  → RETAINED_ALLOWED_DEVELOPMENT_RUNTIME_SEAM
Expected final production file count may therefore remain 3.
Do NOT force ZERO.
DURABLE GUARD
Add/strengthen the smallest focused guard proving:
1. the exact retained Development file allowlist;
2. no new production file can appear in Host/Development without explicit classification;
3. retained files remain Development-only/composition-runtime oriented;
4. MarketplaceDevelopmentBootstrap does not create module domain entities or query/mutate foreign module tables beyond Database.MigrateAsync();
5. Admin/Seller bootstraps do not access foreign DbContexts/DbSets;
6. no production endpoint mapping exists in this folder.
Do not encode brittle line numbers.
FOCUSED VALIDATION
Build only:
- Host
- Host.Tests / directly relevant guard project
Run only:
- new/existing Host Development folder guard;
- direct bootstrap behavior tests if already present;
- smallest startup/bootstrap regression tests needed.
No full solution build.
No broad architecture suite.
No retry loop.
One deterministic local repair per focused failure; then one affected rerun.
If unresolved, STOP as INCOMPLETE/RECOVERY_CONFLICT.
EVIDENCE
Create:
docs/evidence/TB-TMAR-HOST-DEVELOPMENT-AMC-001/
At minimum:
- analyze.md
- content-disposition.md
- validation.md
- closure.md
Evidence must include:
- exact before tree;
- exact after tree;
- per-file retained/moved/deleted disposition;
- per-file architecture justification;
- Development-only proof;
- direct dependency classification;
- persistence exception proof for migration-only DbContext usage;
- focused validation;
- residual debt for THIS FOLDER only.
SOT
Update docs/architecture/tmar-current-state.json with:
hostDevelopmentAmc
Fields:
- task
- parentTask
- parentCommit
- state
- productionFileCountBefore
- productionFileCountAfter
- retainedFiles
- retainedClassifications
- developmentOnlyState
- migrationBootstrapExceptionState
- moduleBusinessAuthorityState
- modulePersistenceAuthorityState
- directForeignDbContextState
- schemaMigrationState
- focusedValidations
- remainingBlockers
- residualDebt
- nextHostFolderStarted = false
- workflowStop = USER_REVIEW_HOST_DEVELOPMENT_CHECKPOINT
- evidence
Persist canonical task:
docs/ai/tasks/TB-TMAR-HOST-DEVELOPMENT-AMC-001.task.md
PASS CRITERIA
PASS only if:
- exact folder fully enumerated and re-enumerated;
- every production file has explicit disposition;
- every retained file is explicitly architecture-allowed;
- no misplaced module business responsibility remains;
- no module-specific seed implementation remains in this folder;
- migration bootstrap's foreign DbContext use is limited to generic Database.MigrateAsync() orchestration;
- Admin/Seller bootstraps contain no foreign persistence;
- Development-only semantics preserved;
- no production behavior introduced;
- no schema/migration changes;
- no frontend changes;
- focused guards/tests pass;
- evidence/SoT/task artifact persisted;
- next Host folder not started;
- commit pushed;
- HEAD == origin/main.
CANONICAL WORKER RESULT
Return ONLY:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-DEVELOPMENT-AMC-001
Parent-Task: TB-TMAR-HOST-CUSTOMERPROFILE-EVACUATION-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Analyze-State:
Host-Development-Production-File-Count-Before:
Host-Development-Production-File-Count-After:
Discovered-Files:
MarketplaceDevelopmentBootstrap-Disposition:
MarketplaceAdminDevBootstrap-Disposition:
MarketplaceSellerDevBootstrap-Disposition:
Development-Only-State:
Migration-Bootstrap-Exception-State:
Module-Business-Authority-State:
Module-Persistence-Authority-State:
Foreign-DbContext-State:
Cross-Module-Join-State:
Production-Endpoint-State:
Path-Namespace-State:
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
After returning the result, stop completely.
Do not inspect/start the next Host folder.
Do not self-issue another task.
Do not poll.
END_TOOBA_TASK
