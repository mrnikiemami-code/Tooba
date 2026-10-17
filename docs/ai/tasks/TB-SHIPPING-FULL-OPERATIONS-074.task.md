PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-FULL-OPERATIONS-074
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-073
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_TIERED_RATE_ADMIN_UI_AND_PACKAGING_FOUNDATION
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: d6c9d5ec2d21331c34cf22f414ecdc1bea5d4606
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD

ARCHITECT RULING
073 ACCEPT in its defined scope: Shipping now owns half-open weight-grams and physical shipment-item-count price conditions and a tier-aware index, with 192 focused Shipping tests passing. This is task 4 of MAXIMUM 10 tasks (071-080). The user requires practical, comprehensible Persian UI plus packaging costs, not merely backend code. The currently proven 073 rate condition API is canonical: DO NOT recreate it, redefine its semantics, or add a competing rule engine. Existing tooba_alpha is disposable Development/test data. Use it directly only when necessary; no parallel database, broad test loops, or unrelated Host restarts. No permission bypass. No Checkout authority cutover.

PREFLIGHT - STOP ON CONFLICT
After BRIDGE-WAKE claim exactly one Pending task on tooba-main and preserve its received .task.md verbatim, UNSTAGED. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, 070-073 planning/evidence, current frontend shipping-rates-coverage-{api,model,dialog}.ts* and tests, current Shipping Contracts/Admin/ShippingAdminContracts.cs, Domain/Aggregates/ShippingRatePolicy.cs, rate engine, validation and migration. Fetch origin/main; Expected-Base-SHA must match, or intervening commits must be proved path-by-path documentation/evidence-only with zero overlapping source changes. Otherwise RECOVERY_CONFLICT/STOP. Preserve all unrelated dirty/untracked files. Never reset/stash/rebase/force-push. Positively identify Development tenant/store and tooba_alpha before any writes; NEVER touch Production/staging.

MISSION A - COMPLETE TIER EDITOR USING EXISTING API, PRIORITY

In the existing per-service «نرخ و پوشش» dialog, add an understandable compact «شرایط مرسوله» section for creating/editing rates. Permit unconditioned pricing, weight-only (integer grams in wire, kilograms shown to humans with explicit exact conversion), item-count-only, and BOTH intersected axes. Label boundaries clearly: از شامل، تا غیرشامل; optional upper bound means no upper limit. Do not equate physical shipment units with cart lines or inferred cart Quantity. Avoid float boundary mistakes; use integer gram representation on the API. No new admin page or giant monolithic file.
Read and render the 073 persisted condition for every existing rate row, and preserve it losslessly when editing existing rates. Send the exact contract shape condition: {weightGrams:{min,max}, itemCount:{min,max}} or null for unconditioned rules. Never transform a previously unconditioned rate to a restricted tier on incidental save. Keep scope, optionCode, free threshold and currency behavior intact.
Show the effective service option + geography scope + exact weight/count interval in each rate row and in validation errors. Create and edit must both support conditions. Map stable errors shipping.rate_policy.tier_overlap / scope_duplicate and tier_range_invalid / tier_min_negative / tier_out_of_range to adjacent Persian guidance. Do not obscure rate-vs-coverage distinction: coverage still authorizes a destination, not a weight/count tariff. No magic client-side price previews that claim live Checkout quotes.
Use existing read/create/update endpoints; existing admin permissions only. A real browser against Development frontend and Host must create two adjacent synthetic weight tiers [0,2000) and [2000,5000) at a real service, option and destination scope not colliding with active rows; read/reopen/edit/toggle, check 1999/2000 boundary description, and show a deliberate overlap refusal with no write. Do not invent business approval. Cleanly deactivate or remove ONLY task-owned synthetic fixtures using approved API; if no delete surface, deactivate and ledger IDs. Do not damage 068/069 parity fixtures. Prefer a different real destination scope or inactive test identity; do not assume duplicate tiers are allowed under any identity.

MISSION B - PACKAGING COST FOUNDATION (IF TIME, BUT NO FAKE COMPLETION)
5. Shipping owns explicit packaging fee policy, distinct from carrier freight rate, discount/free-freight threshold and payer mode. Introduce smallest stable Contracts/Domain/persistence representation ONLY if a bounded safe slice fits. Define currency, nonnegative fixed fee, service/option and scope ownership, zero/default semantics, deterministic precedence and whether packaging is waived by free shipping. Safe default: NO packaging fee when no configured policy; never silently charge existing Checkout. Do not hide packaging inside chargeAmount; no shared Host/Payment logic.
6. If implementing B, include Shipping-scoped additive EF migration + authorized admin API read/write and limited tests; document what is and is not integrated into quote. If B cannot be safely completed within remaining time, STOP at a clean boundary, report PARTIAL with exact gap and hand it to the next Architect decision. Do NOT dilute Mission A proof or mark packaging complete merely because UI input exists without a persisted owner.

FOCUSED VALIDATION / TIME CONTROL
Focused frontend model/API tests and npx tsc --noEmit, genuine browser UI interaction using existing running Dev host and frontend. Run Shipping domain/HTTP-focused tests and one scoped migration only IF backend changed. Do not run Host.Tests against locked DLLs, no broad full-repo guards, no new scratch databases, no polling or iterative test torture. At HARD 45-minute cutoff stop and report. Avoid logs with tokens/passwords and avoid giant stdout captures.

ALLOWED PATHS

src/frontend/app/admin/shipping-services/shipping-rates-coverage-api.ts
src/frontend/app/admin/shipping-services/shipping-rates-coverage-model.ts
src/frontend/app/admin/shipping-services/shipping-rates-coverage-dialog.tsx
src/frontend/app/admin/shipping-services/shipping-rates-coverage-model.test.ts
src/frontend/app/admin/shipping-services/shipping-rates-coverage-api.test.ts (new, only if necessary)
src/frontend/app/admin/shipping-services/shipping-rate-tier-model.ts (new, only for a clean separate model)
src/frontend/app/admin/shipping-services/shipping-rate-tier-model.test.ts (new, if model added)
src/backend/Modules/Shipping/Tooba.Shipping.Contracts/** (Mission B only)
src/backend/Modules/Shipping/Tooba.Shipping.Domain/** (Mission B only)
src/backend/Modules/Shipping/Tooba.Shipping.Application/** (Mission B only)
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/** (Mission B only)
src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/** (Mission B only)
src/backend/Modules/Shipping/Tooba.Shipping.Tests/** (Mission B only)
docs/planning/shipping/SHIPPING-FULL-OPERATIONS-074.md
docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-074/**
docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-074.task.md (Bridge receipt only, NEVER stage)

FORBIDDEN
No new page, no unrelated Fulfillment/Geography/Order/Payment/Checkout/Host source edit, no commercial tariff invention or nationwide grant, no customer pricing cutover, no schema-wide migrator, no direct SQL bypass, no Production/staging write, no parallel DB, no new permissions, no Results.Json, no changes to unrelated dirty paths, no broad suite, no autostart of task 075. Worker PASS != Architect ACCEPT.

ACCEPTANCE / STOP
PASS for Mission A only if the real editor creates, reads, edits and renders conditional tiers, stable 400 overlap handling and unchanged unconditioned rules, proven over real Development UI, with focused green tests and no leftover active synthetic fixtures. Explicitly grade Mission B as COMPLETE, PARTIAL or NOT STARTED; overall status PARTIAL if the task claims both missions but packaging is not complete. Report data-provenance, rollback, exact elapsed, source/evidence SHAs and remaining blockers. Shipping/Checkout commercial readiness must not be claimed. Hard stop at 45 minutes even when partial.

RESULT CONTRACT - FIXED CANONICAL TAIL
Return ONE actual completed Worker Result to Bridge; fill all values, no unresolved placeholders in transmitted result, body strictly after automaticNextImplementationTask and before END_TOOBA_WORKER_RESULT; nothing after END_TOOBA_TASK.
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-FULL-OPERATIONS-074
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-073
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_TIERED_RATE_ADMIN_UI_AND_PACKAGING_FOUNDATION
Bridge-Task-Row: <actual Bridge row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: d6c9d5ec2d21331c34cf22f414ecdc1bea5d4606
Base-SHA: <actual SHA>
Code-SHA: <actual SHA or NONE>
Evidence-SHA: <actual SHA or NONE>
automaticNextImplementationTask: NONE
<front-end tier CRUD evidence; adjacent/overlap proofs; packaging mission verdict; actual dev target; changed paths; tests; data cleanup; compatibility; elapsed; commit/push; explicit remaining blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
