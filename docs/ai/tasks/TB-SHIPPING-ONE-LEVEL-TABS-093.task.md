PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-093
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-092
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_REPRODUCIBLE_DEV_HOST_AND_ADMIN_AUTH_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 4bdd9901a38101fb4160a45cec7de7dee8306684
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
The user requested a task/result/next-task loop until the Shipping one-level UI is genuinely usable in shared Development, not just an isolated pair. Task 092 proved all five tabs with a current-code isolated Host on :5100, frontend :3100, real Development database (7 methods/14 translations), and authenticated browser. But shared :5088 remains a stale orphaned Tooba.Host and the :3000 frontend development actor receives 403 there. Close precisely these two dependencies with a reproducible, safely reversible DEVELOPMENT-ONLY operator procedure and truthful end-to-end evidence. This is not approval for broad production/staging actions, commercial Checkout cutover, arbitrary actor spoofing, security bypass, or blind killing of a shared process. Strictly one task, then STOP.

PREFLIGHT — MANDATORY
Claim exactly one Pending task on tooba-main; save this exact artifact to docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-093.task.md, receipt UNSTAGED. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, 090–092 planning/evidence and 092 live logs, master recovery, existing development hosting scripts, launchSettings, BFF auth/host-client, existing admin identity/permissions code. Fetch origin/main; require Expected-Base-SHA or prove only nonoverlapping 092 result-bookkeeping drift. On overlapping source change STOP RECOVERY_CONFLICT. Preserve ALL dirty/untracked files; no reset/stash/rebase/force push, no speculative cross-module rewrite.

MISSION A — MAKE RESTART REPRODUCIBLE, NO BLIND STOP

Positively revalidate GET http://127.0.0.1:5088/__platform-commerce -> dev-singlestore/store-alpha/tenant-alpha/tooba_alpha; :5088 PID/command/binary fingerprint, :3000 frontend owner, :5088 health, DB migration once/7 methods/14 translations, and the hash-verifiable 090 backup. Do not assume remembered PIDs. This authorization applies to this positively proven DEVELOPMENT stack ONLY.
Implement the smallest repo-owned scripts/dev/start-host.ps1 and scripts/dev/stop-host.ps1 (or one script with start/stop) with deterministic project path, working directory, ASPNETCORE_ENVIRONMENT=Development, explicit http://127.0.0.1:5088, PID file, logs, health/provenance verification, refusal when the port is owned by an unrelated process, and a recoverable rollback path. Avoid hardcoded credentials and never stage PID/log/secret files. If a previously running orphan lacks trustworthy relaunch environment/rollback, STOP before terminating it; do not invent operator authority or use Stop-Process -Force as a shortcut. Building in isolated OutDir is preferred to bypass locked in-tree DLLs.
If verified safe stop/restart cannot be achieved in this timebox, still commit the bounded repeatable scripts and evidence (if useful), give PARTIAL with exact blocker, and STOP. Do not disguise another isolated success as shared-stack success.

MISSION B — REAL ADMIN AUTH FIX, NO BYPASS
4. Reproduce the existing BFF/shared Host discrepancy with real supported Development login. Determine why the frontend actor aaaaaaaa-aaaa-4aaa-8aaa-000000000009 is denied (403) while the bootstrapped real admin is authorized. Correct the DEVELOPMENT bootstrap/session identity wiring through the established authorization path, not through arbitrary X-Admin headers, an AllowAnonymous endpoint, permission-broadening, a hardcoded production admin, or disabled ACLs. Do not put passwords/tokens or actor identifiers in committed settings/logs. Ensure failed login and unauthorized requests remain 401/403. Prove the authorized BFF session is the same actor recognized by Fulfillment admin read endpoints.
5. Prefer an existing safe configuration/identity source and a minimal fix. Any new development-only setting must fail closed outside Development and must not affect the certified Host composition-only rule or module ownership.

MISSION C — SHARED DEVELOPMENT UI PROOF / STOP CONDITIONS
6. ONLY if A establishes safe controlled deployment and B establishes legitimate admin authority, start current-code Host on shared :5088 with the repo-owned procedure and enable ONLY Tooba:ShippingOneLevelCatalog=true in a reversible Development process-scoped config. ShippingRateAuthority and ShippingOperationalShadow remain OFF. Preserve :3000 unless an independently authorized, reversible frontend restart is essential. No Checkout price switching.
7. Prove shared http://127.0.0.1:3000/fa/admin/shipping-services with a real authenticated browser (not isolated :3100): visible 5 Persian RTL tabs, 7 flat methods, inactive post:express and snapp_courier, no second-level editor, selected method preserved; tabs 2 rates/coverage, 3 packaging/payer, 4 timezone/windows/configured-only capacity, 5 operational quote (read-only). Capture browser screenshot + network evidence. Denial cases remain 401/403. Quote read-only POST is allowed; no merchant write, no financial collection, no order mutation.
8. Recheck live target/provenance, Checkout legacy authority, historical rows, gate isolation, stop/start repeatability and rollback instructions. Record the exact actual shared-stack outcome. If all not proven, report PARTIAL/BLOCKED; never claim LIVE READY from source tests or isolated pair only.

FOCUSED TESTS / GUARDS
Run only focused tests for changed code and necessary type/build checks; avoid Host DLL-lock retry loops and huge suites. Retain 092 proof by reference, do not redo the entire isolated test matrix. Never widen missing permissions to turn 403 into 200. No staging/production requests, new migrations/backfill, active commercial tariffs, packed-weight guesses, capacity reservation, COD settlement, or promised delivery claims.

ALLOWED PATHS

scripts/dev/** (minimal Development Host operator scripts and safe gitignore if needed)
existing frontend BFF/auth and Development identity configuration files ONLY if a demonstrable, narrowly scoped mismatch is proven; cite exact paths before editing
corresponding smallest focused tests, module-owned or frontend architecture guard only if directly affected
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-093.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-093/**
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md (append-only truthful checkpoint if warranted)
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-093.task.md (receipt only, UNSTAGED)
Any other source path requires architectural justification in the result and must not be edited if there is doubt.

RESULT CONTRACT
Return exactly one canonical BRIDGE-WAKE-V1 envelope with Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Program, Mode, Bridge-Task-Row, Status PASS/PARTIAL/BLOCKED/RECOVERY_CONFLICT, Stop-State STOP_FOR_ARCHITECT, Expected-Base-SHA, Base-SHA, Code-SHA, Evidence-SHA, automaticNextImplementationTask NONE; actual changed paths, actual elapsed minutes, precise source/HTTP/browser evidence, 401/403 proofs, rollback, remaining blockers, and whether shared :3000 genuinely shows the five tabs. Do not issue task 094 automatically.
END_TOOBA_TASK
