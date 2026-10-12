PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-SHIPMENT-METADATA-OPTIONALS-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR
Repo: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: f9b66c15754188f6d9614130978f459a883110d6

Architect-Intent: Act as a senior software architect. Eliminate exactly the four TypeScript diagnostics for undefined optional metadata in src/frontend/app/admin/admin-create-shipment-modal.tsx without sacrificing frontend/backend boundaries, backend request contracts, or meaningful omission semantics. This is a small, isolated repair. Architect estimate: 15-20 minutes; bridge worker protocol currently hard-stops at 15 minutes, with 10-12 minutes as execution target. Do not run a clock loop; if blocked or over budget, return INCOMPLETE and STOP.

Preflight: Fetch origin and verify clean tracked working tree and HEAD == origin/main == Expected-Starting-HEAD; on divergence STOP and report without rebasing or touching concurrent work. Read AGENTS.md and the applicable .cursor/skills/tooba-frontend-feature/SKILL.md, plus only nearby metadata producer/consumer and existing focused tests necessary to establish semantics. Do not consult the deferred findings register.

Problem: The recorded tsc baseline contains four TS2322 diagnostics at admin-create-shipment-modal.tsx:251,253,263,265. buildMetadata() promises Record<string,string|number> while post/tipax object literals explicitly put undefined in weightKg and declaredValue; later submission already filters undefined and empty values. Verify the exact metadata wire contract and any validators before choosing repair. Do not make up default weight or declared value, and do not serialize null/undefined just to quiet tsc.

Allowed production scope: src/frontend/app/admin/admin-create-shipment-modal.tsx only. If an existing focused test adjacent to this module can protect the optional-metadata omission semantics, amend it minimally; otherwise add at most one focused directly relevant test only if necessary and discovery-compatible. Do not edit generic form controls, API/DTO/backend contracts, types outside this module, Home/PDP/Shopeiva, tsconfig, guards, CI or baselines. Do not introduce any, ts-ignore, unsafe assertions or broad refactors. If exact runtime contract requires cross-owner change, STOP with evidence and decision options.

Acceptance: Optional weightKg/declaredValue are omitted when blank/invalid per existing behavior; supplied valid numbers remain numeric; required fields, post/tipax/snapp paths, request JSON and error handling remain intact. Four targeted TS2322 diagnostics disappear without new diagnostics or altered unrelated flows. Preserve architecture and current frontend/backend request ownership. Choose the smallest contract-honest implementation (e.g. construct optional keys only when values exist, or accurately type an intermediate before canonical filtering); do not widen final request type without proof.

Validation: Run one post-change local TypeScript tsc --noEmit and compare against the committed 38-error baseline evidence at docs/architecture/evidence/TB-FE-ADMIN-MENUS-HELPER-SIGNATURES-001/typecheck-after-tsc.txt. Expected approximately 38 -> 34, zero new errors. Run only the directly relevant pre-existing focused shipment test(s), or a single concise targeted regression if necessary; no full npm test, broad build, backend suite, AMSC waves or repeated retries. For an unrelated baseline failure, record once and stop chasing it. If tests cannot run due to environment, report accurately. Do not claim typecheck is green while diagnostics remain.

Deliverables: Commit only scoped changes, exact task receipt docs/ai/tasks/TB-FE-SHIPMENT-METADATA-OPTIONALS-001.task.md, and concise evidence under docs/architecture/evidence/TB-FE-SHIPMENT-METADATA-OPTIONALS-001/. Push normally to main only if preflight/scope/validation gates allow. Include changed files, before/after error count, focused test results, evidence location, exact SHA, origin/main equality and known risk in canonical BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT reply. No next task, no automatic repair chain, no baseline reset. STOP.
END_TOOBA_TASK
