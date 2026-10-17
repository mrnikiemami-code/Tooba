PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-STOREFRONT-PERF-QUERY-SCALE-183
Parent-Task: TB-STOREFRONT-PERF-PUBLISHED-SCALE-182
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: b5b9644ef6c01e7b8f7ee48fac2de416d95f2beb
Program: TOOBA-STOREFRONT-PERFORMANCE-RELEASE-CLOSURE
Mode: BOUNDED_REQUEST_SCOPED_QUERY_SCALE_EVIDENCE_AND_RELEASE_GATE_REVIEW
Hard-Timebox: 45 minutes HARD; stop PARTIAL/BLOCKED instead of overrun.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Close only the outstanding technical evidence item from TB-182: reproducible, request-scoped EF DbCommand counts for both 3-Published-item Size A and >=12-Published-item Size B (at least 4x) on one disposable, genuinely owner-published catalog. Preserve the successful TB-178 log defaults. Decide whether PERF-CRIT-001 technical criteria are met, but do NOT unilaterally close the release blocker: real user browser responsiveness acceptance remains required.

MANDATORY FIRST CHECK
Verify HEAD == origin/main == Expected-Base-SHA; if not, stop and report divergence. Read docs/planning/MUST-FIX-BEFORE-RELEASE.md, docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md, and TB-178 through TB-182 RESULT/evidence plus TB-182 scratch harness. Follow repository task/naming/folder/namespace conventions; don't invent new technical-axis production roots or rebaseline guards. Identify precisely which TB-182 fixture and scripts can be reused; do not rediscover or write a second custom seeder.

ONE BOUNDED EXECUTION SLICE

Create only a disposable localhost PostgreSQL/Host environment guarded against any shared port/data access. Reproduce Size A and then Size B via the existing Development bootstrap and real owner-authorized seller offer/price/inventory and product publish endpoints (TB-182). Verify actual Published product IDs displayed in Home at both sizes and authentic actor permissions. Do NOT change publication state with SQL, clone rows, invent authorizations, suppress validators or use reset-and-seed on shared data. Limit publication attempts; if the 4x real published state cannot be produced within the timebox, report PARTIAL with exact blocker rather than repeat time-consuming steps.
For each size, use the same Host binary, tenant, route, locale and instrumentation. In a diagnostic process/session only set Logging__LogLevel__Microsoft.EntityFrameworkCore=Information; maintain production/Development committed Warning default unchanged. After startup settles, collect one warm-up + three warm request responses and COUNT only Microsoft.EntityFrameworkCore.Database.Command CommandExecuted events actually belonging to each Home HTTP request by request TraceId and request interval; independently report all startup/background commands. Do not infer zero DB commands from the quiet default leg. Record status, wall ms, payload byte size, SHA-256, Published productIds and bounded log bytes per request. Include exact counters, not only console total.
Compare A vs B: command count per request, growth ratio against Published item growth, latency and payload; detect remaining item-linear N+1 or unexpected fan-out. Re-run a quiet default leg only if needed to verify EF log suppression on the same B state; minimize redundant work. Protect seller/name/price/stock/availability and item identities. If counts grow materially per item, identify owner call site and propose a separate scoped fix; do not change production source during this measurement task.
Update PERF-CRIT-001 register with grounded A/B evidence and explicit gates: logging fix, query growth, performance, response parity, browser acceptance. Do not mark CLOSED without required architect AND user responsiveness approval; state recommended technical disposition. Record exact scripts, assertion table, sanitized traces and complete cleanup.

ACCEPTANCE / VERDICT
PASS only if both sizes are genuinely published through real owner authority, exact request-scoped command counts are produced for 3 warm requests per size, actual responses and product cardinalities validated, A/B ratio analyzed honestly, evidence published, and scratch fully cleaned. PARTIAL if query growth cannot be measured or performance concern remains unresolved. BLOCKED when authority or fixture cannot be legitimately built. Technical PASS is not release sign-off. No broad test run for evidence-only work.

SAFETY
No writes/restarts to shared :3000/:5088/:5432, tooba_alpha, store-alpha, running shared Host/Next, Redis, RabbitMQ or .next; preserve unrelated dirty Admin file, TB-075/079 logs, TB-173 files and stashes. No reset/stash/rebase/force push; no secrets in evidence. No payment, SMS, payout, carrier call, fabricated sales or cross-tenant access. No production code or config change, and no architectural bypass, Results.Json, or Host business authority. Fast-forward push scoped task/evidence only. Stop within 45 minutes.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-STOREFRONT-PERF-QUERY-SCALE-183
Parent-Task: TB-STOREFRONT-PERF-PUBLISHED-SCALE-182
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
Fixture-Provenance: ...
Published-Size-A-And-B: ...
Request-Scoped-DbCommand-Counts: ...
Latency-Payload-And-Item-Parity: ...
Query-Scaling-Assessment: ...
Release-Gate-Disposition: ...
Focused-Tests: ...
Elapsed-Minutes: ...
Isolation-And-Cleanup: ...
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
