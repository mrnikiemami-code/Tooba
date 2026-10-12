PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Parent-Task: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: AUDIT_READ_ONLY

Objective: Establish a reproducible, evidence-based root-cause inventory and a bounded repair sequence for the reported ~155 pre-existing TypeScript errors in src/frontend. This task is ANALYSIS ONLY. No implementation, cleanup, baseline changes, test expectation changes, commits of production code, or AMSC waves.

Mandatory governance: Read AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md, docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md, and relevant authoritative typed API/design-system contracts. Respect ARCH-FE-FREEZE-001 and critical Shopeiva Home/PDP locks. Do NOT read the growing architecture findings register: it is opt-in and this task is not a register update.

Preflight: Verify branch main, safe working tree, and HEAD == origin/main; document pre-existing dirty/untracked files and never remove, stash, or rewrite them. Do not disrupt running development services.

Execute the repository's actual frontend typecheck command (cd src/frontend && npm run typecheck). Capture unabridged raw stdout/stderr, exit code, tool/compiler version, and exact HEAD as local evidence. If the declared script runs more than one check, distinguish their results. Do not assume the number of errors is still 155.
Parse every diagnostic into path, code (TSxxxx), line, message, owning subsystem, and a root-cause cluster. Record exact total errors, unique files, error-code distribution, and cluster counts. Distinguish direct root errors from cascading symptoms. Ensure the sum of clusters equals the total; preserve exact examples.
Audit the top clusters by reading the real declarations and uses, not by assuming the fix. Prioritize GridColumnDef.maxWidth / admin-languages / admin-receipts, SectionDisplayConfig versus shared-composition-renderer, i18n/middleware-locale, and other actual high-frequency roots; inspect nearby focused tests and architecture guards. Identify canonical owner and allowed dependency direction for each.
For each cluster, propose the smallest safe repair task: exact owned files and public contracts, expected type-error reduction, possible impacts on Home/PDP/other shared surfaces, test commands, rollback criteria, and whether it can be fixed without contract/schema changes. Separate genuine bug from stale tests/contracts and unclear product decisions; flag STOP questions rather than inventing semantics.
Produce a short ordered recovery plan: first shared-contract roots that eliminate cascades, then local consumers, then final green typecheck and CI enforcement. Plan parallel work ONLY where dependencies are independent; otherwise sequence tasks. Avoid broad rewrites, any, @ts-ignore, tsconfig exclusion, weakening types, disabling gates, or editing baseline snapshots.
Validation: No production mutations. If any new diagnostics appear because of concurrent main changes, pin and report exact revision, then re-evaluate. State explicitly which tests were run and not run; do not declare overall typecheck green.

Deliverable: Commit and push only concise audit evidence under docs/architecture/evidence/TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001/ (summary.md and diagnostic inventory, preserving raw output if reasonably sized) and the exact task receipt under docs/ai/tasks/; no other changed tracked files. If the task file is already persisted through Bridge, do not duplicate it. Follow the repository's safe commit/push protocol and stop on unsafe divergence.

Result contract: BEGIN_TOOBA_WORKER_RESULT / END_TOOBA_WORKER_RESULT with Task-ID, BASE SHA, final SHA, actual error count, diagnostics by TS code, cluster totals, top 3 root causes, recommended FIRST bounded repair Task ID/scope, command results, evidence paths, changed files and STOP/USER_REVIEW. Do not automatically execute the recommended repair.

Stop: USER_REVIEW_AFTER_TYPECHECK_AUDIT
END_TOOBA_TASK
