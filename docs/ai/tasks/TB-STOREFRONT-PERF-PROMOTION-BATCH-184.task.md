PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-STOREFRONT-PERF-PROMOTION-BATCH-184
Parent-Task: TB-STOREFRONT-PERF-QUERY-SCALE-183
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 47c818686bc0b668eaf8896cc894920e251a4fce
Program: TOOBA-STOREFRONT-PERFORMANCE-RELEASE-CLOSURE
Mode: BOUNDED_PROMOTION_OWNER_BATCH_READ_AND_HOME_N_PLUS_ONE_REPAIR
Hard-Timebox: 45 minutes; HARD STOP and report PARTIAL if unfinished.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Remove the remaining owner-identified Home N+1: StorefrontComposer.ComposeProductAsync calls _promotions.EvaluateForCheckoutAsync per product, and PromotionDirectory.EvaluateAsync loads Promotions per call. Preserve promotion decisions, price/discount/currency, eligibility, ordering, tenant isolation, customer/cart identity, and storefront JSON/appearance. This is ONE focused implementation task, not another general benchmark or a blanket promotions cache.

MANDATORY FIRST CHECK
Verify HEAD == origin/main == Expected-Base-SHA before writing, else STOP. Read docs/planning/MUST-FIX-BEFORE-RELEASE.md, TB-183 evidence and the actual Promotion Contracts/Application/Infrastructure and Order/Storefront composer integration. Examine the complete EvaluateForCheckoutAsync decision inputs and promotion scope, side effects, concurrency, auth, time boundaries, and whether checkout evaluation uses cart-specific data. Inspect current batch preparation code and existing promotion tests. Confirm any proposed batch optimization is behavior-equivalent for Home and does not weaken the real checkout path. Respect existing repository naming, canonical path/namespace, owner-module contracts, MediatR 12.5/ISender and Result conventions. Do not introduce a technical-axis root, Host business logic, Results.Json or cross-module DbContext use.

BOUNDED IMPLEMENTATION

Prefer an owner-defined batch/read-only Promotion contract or safe request-scoped snapshot only IF actual semantics support it. Promotion owns retrieval, policy evaluation and eligibility. Order/Storefront supplies existing typed product/seller/offer context and receives typed per-product results through Contracts; no access to Promotion entities or DbContext. Do not expose mutable EF entities across modules or shortcut authorization. Keep the production checkout evaluation contract and behavior unchanged.
Eliminate repeated Promotion table reads for Home cards. Handle empty promotions, active/inactive promotions, scope, current time, seller/cart/tenant applicability, and ambiguous/malformed evaluation fail-closed. If some properties genuinely require per-item owner evaluation, batch evaluation inside Promotion owner rather than caching the final outcome across distinct products. Never silently skip promotions, zero a nonzero discount, or mutate pricing.
Keep changes confined to the smallest Promotion owner contract/application/infrastructure adapter and Storefront Home consumer slice required. No unrelated shipping/admin/payment/settlement changes, no global caches, no migration or flag default change, no storefront visual change. If a safe batch contract cannot be established promptly, STOP PARTIAL with exact source-backed blocker rather than using a misleading shortcut.

FOCUSED ACCEPTANCE

Add narrowly targeted owner tests for batch vs existing per-item decisions: empty, applicable, non-applicable, competing/priority, tenant/seller/product variation, and time boundaries as supported by existing behavior. Prove checkout per-item semantics unchanged and no shared mutable state; compare normalized Home response product identifiers, titles, offers, prices, currency, discounts, seller, stock and promotion fields before/after on a stable fixture.
Provide real request-scoped command accounting on two legitimate published-catalog sizes if the reusable TB-182/183 isolated fixture can be assembled INSIDE the hard timebox. Target no promotion-driven +1 query per Home item (e.g. 3 vs 12 published). Distinguish startup/background commands from Home TraceId, and logging visibility from actual query count. If only one size feasible, report PARTIAL for scale evidence, never claim 0 actual queries from quiet logs.
Focused unit/integration tests, targeted architecture guards and typecheck/build only for touched projects; no broad loop or historical baseline rebaseline. Capture exact commands/results, payload comparisons, query counts and any remaining differences in docs/architecture/evidence/TB-STOREFRONT-PERF-PROMOTION-BATCH-184/RESULT.md. Do not change PERF-CRIT-001 to CLOSED without architect and user browser acceptance.

ISOLATION / SAFETY
Only task-owned disposable loopback Host/PostgreSQL/test processes where necessary. No touching/restarting shared :3000/:5088/:5432, tooba_alpha, store-alpha, Redis, RabbitMQ, .next, shared Host, or unrelated dirty admin/evidence/log files or stashes. No reset/rebase/stash/force push, no arbitrary SQL product clones, fabricated authorization, disabled publication validators, real payments/SMS/payouts, or user-visible data changes. Keep secrets out of committed evidence; cleanup exact task-owned processes/containers. One task; no automatic next.

PASS / PARTIAL / BLOCKED
PASS requires source-backed behavior-equivalent Promotion owner batch consumption by Home, focused tests/guards green, positive evidence repeated owner table reads are removed, and no price/promotion/payload regression; report A/B scale evidence separately with honesty. PARTIAL if implementation, runtime parity or essential proof remains incomplete. BLOCKED if ownership/semantics require a new architect decision. Enforce the 45-minute HARD limit even if remaining proof is unfinished. Push only scoped fast-forward commits, verify HEAD/origin parity, STOP_FOR_ARCHITECT.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-STOREFRONT-PERF-PROMOTION-BATCH-184
Parent-Task: TB-STOREFRONT-PERF-QUERY-SCALE-183
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-STOREFRONT-PERFORMANCE-RELEASE-CLOSURE
Status: PASS|PARTIAL|BLOCKED
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Naming-And-Architecture-Guards: ...
Owner-Batch-Design-And-Semantics: ...
Scope-Delivered: ...
Focused-Tests: ...
Query-Counts-And-Payload-Parity: ...
Remaining-Release-Gates: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
