PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-CATALOG-ATTRIBUTE-GRID-QUERY-TYPES-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: be5492c617709ceb690aff6a8424d9e12f58dd10

Architect intent: Act as a senior software architect. Fix types at the correct frontend ownership seam without compromising frontend or backend architecture, behavior, or contracts. This is ONE bounded task, estimated by Architect for a short work unit; do not implement a timer or enter a retry loop. Current recovery governance has a hard 15-minute worker limit; stop with INCOMPLETE if unsafe or too large.

Objective: Resolve exactly the two TS7006 implicit-any diagnostics in src/frontend/app/admin/catalog-attribute-ui.tsx at the definitionQueryAdapter and schemaQueryAdapter callback query parameters (approximately lines 237 and 833 on the base commit). Ground the query type in the existing canonical grid API, likely GridServerQuery, after inspecting its real exported contract. Preserve the existing createClientGridQueryAdapter, useLegacyAdminGridDirectProps and callback semantics.

Allowed production changes: src/frontend/app/admin/catalog-attribute-ui.tsx only. Add a type-only import from the actual owning design-system module and appropriately annotate the two callback inputs, or a strictly equivalent minimal type-safe local correction if verified from source. Do not create any new duplicate grid types.

Required reads: AGENTS.md and applicable frontend feature skill and architecture standard; current target file; owner of GridServerQuery and createClientGridQueryAdapter; directly relevant focused test(s). Do not read the architecture findings register: opt-in only.

Forbidden: changing grid implementation, adapters, grid visuals, columns, search/sort/filter semantics, backend/Host/schema, Home/PDP/Shopeiva locks, tsconfig, baselines, guards, unrelated TypeScript clusters; no any, casts masking errors, ts-ignore, new dependency, wide refactor, automated AMSC, automatic next task or test loop.

Verification: check git fetch/HEAD == origin/main == expected starting HEAD and tracked-tree safety; one tsc --noEmit after repair and compare diagnostics to recorded 34-error baseline at docs/architecture/evidence/TB-FE-SHIPMENT-METADATA-OPTIONALS-001/typecheck-after-tsc.txt (expect 34 -> 32 and zero introduced). Run only the nearest existing focused catalog attribute/grid test if available; otherwise say why it is not suitable. Avoid full suite, broad builds and repeated runs. If an unrelated baseline test fails, report it without fixing it.

Evidence and handoff: add this exact task receipt to docs/ai/tasks/TB-FE-CATALOG-ATTRIBUTE-GRID-QUERY-TYPES-001.task.md and minimal evidence under docs/architecture/evidence/TB-FE-CATALOG-ATTRIBUTE-GRID-QUERY-TYPES-001/. Commit and push the bounded repair only after successful preflight and scope checks. Report exact SHA, remaining typecheck count, focused test outcomes, changed files, caveats, and canonical BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT block. STOP for Architect independent acceptance. automaticNextImplementationTask = NONE. No follow-on repairs, no self-splitting.
END_TOOBA_TASK
