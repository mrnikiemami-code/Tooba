PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-STOREFRONT-PERF-SCALE-ACCEPTANCE-179
Parent-Task: TB-STOREFRONT-EF-LOGGING-RELEASE-178
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 26b9c90daf79fa61493e91c9dba725e72562ae0c
Program: TOOBA-STOREFRONT-PERFORMANCE-RELEASE-CLOSURE
Mode: BOUNDED_TWO_SIZE_STOREFRONT_PERFORMANCE_AND_PARITY_ACCEPTANCE
Hard-Timebox: 45 minutes HARD; stop with PARTIAL at the limit, never exceed it.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Close the precise remaining evidence gap from TB-178 / PERF-CRIT-001: reproducible storefront catalog performance at TWO meaningfully different NONEMPTY catalog sizes, plus product/name/price/availability/seller/tenant payload parity and bounded warm-run latency. This is a measurement-and-targeted-repair task, not a new architecture proposal. Do not mark the release blocker CLOSED yourself: Architect review and user-perceived responsiveness are separate gates.

MANDATORY FIRST CHECK

Verify HEAD == origin/main == Expected-Base-SHA before work; if not, STOP and report divergence. Preserve all unrelated dirty files and stashes.
Read docs/planning/MUST-FIX-BEFORE-RELEASE.md PERF-CRIT-001, docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md, TB-178 RESULT and its raw before/after evidence, and the earlier N+1 batching evidence at commit 73160e3. Read the actual Catalog/Home assembly path and existing repository scratch-catalog fixtures before designing a new seed. Check exact repository test/folder/namespace conventions and reuse existing owner-owned seed paths; do NOT add a new layer, duplicate fixture or unapproved naming style.
Keep the measurement definitions separate: executed EF DbCommands, EF log lines, stdout bytes, HTTP wall latency, Host handler timing, proxy latency and response bytes. Never equate command log lines to query count in the quiet leg.

BOUNDED EXECUTION
A. Reuse a disposable, independently guarded loopback PostgreSQL/Host setup. Construct or reuse deterministic catalog fixtures at TWO nonempty sizes, for example small and at least 4x larger, each representing meaningful seller, product, variation, price, active offer, inventory and localization relations. Use actual application-owned bootstrap or supported APIs where possible. If no safe owner-consistent fixture can be assembled in time, report PARTIAL with exact dependency; no fake measurements, no disabling product relations or shrinking the returned product list.
B. For each size, run the SAME Home endpoint with the SAME tenant, locale, cache/warmup policy, and published build: at least one warm-up and three measured warm requests. Capture HTTP status, exact catalog item count, response bytes, elapsed times, stable response content and authoritative request/DB-command counts. Use a task-owned opt-in EF diagnostics leg (EF=Information) with sanitized logs, and default EF=Warning leg, to measure command count without claiming quiet logs contain none of the commands. Bound stdout log growth in both legs. Do not expose credentials or customer data.
C. Compare returned records before/after logging configuration on identical datasets; compare product IDs, localized names, seller identity, offer/price/currency, stock/availability and media references (if present). Assert no omission/cross-tenant bleed and stable counts; use a second scratch tenant only if safely supported and necessary. An empty response or one with no products is NOT scale evidence.
D. Reconcile N+1 claim with measured trend: report commands per item, small→large ratio, fixed overhead and any remaining high-growth call sites with module owner + path. If a concrete newly discovered N+1 regression is repairable narrowly within 45 minutes, fix only the owner-correct reader with small focused tests; otherwise do not start an uncontrolled refactor. No proxy-timeout increase or cache-deletion workaround.
E. Preserve TB-178 EF>=Warning defaults in committed config (with opt-in diagnostic override) and app error/warning visibility. No production pricing, inventory, security, tenant, checkout or shipping behavior changes.

PROOF / VERDICT

PASS for this bounded task only if both nonempty sizes, three warm requests each, precise EF command accounting, byte/payload parity and item-level source comparisons are captured with reproducible sanitized evidence, no regressions, and private resources cleaned. PARTIAL if a required size/parity/measurement is missing; BLOCKED only for an identified external prerequisite.
Record exact fixture sizes, row counts, requests, medians/ranges, command totals, stdout bytes, key field comparison results, all test commands and their pass/fail counts, including any known baseline failure. Supply a compact before/after and scale table in docs/architecture/evidence/TB-STOREFRONT-PERF-SCALE-ACCEPTANCE-179/RESULT.md.
Update PERF-CRIT-001 register only with measured progress and outstanding Architect/user release gates; do not silently set CLOSED, alter unrelated shipping recovery claims or rebaseline source-size guards.

SAFETY / OWNERSHIP
ONE active task; 45-minute HARD stop; no automatic successor. No shared :3000/:5088/:5432, tooba_alpha/store-alpha, Redis/RabbitMQ, shared Next .next caches or other workers' instances touched/restarted/written. No reset/rebase/stash/force push; preserve unrelated admin-fulfillment and TB-075/079/TB-173 dirty files. Guard scratch identity, loopback ports and tenant before ANY mutation; remove only exact task-owned PIDs/container. No Host business logic, cross-module DbContext reads, Results.Json, fake permissions, PSP/SMS/payout/carrier call, sensitive EF logging or production data mutation. Follow existing module boundaries (Contracts-only, CQRS/ISender, Result), naming conventions and canonical task/evidence directory layout.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-STOREFRONT-PERF-SCALE-ACCEPTANCE-179
Parent-Task: TB-STOREFRONT-EF-LOGGING-RELEASE-178
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
Two-Size-Fixture-Provenance: ...
Measured-Scale-Table: ...
DB-Command-Accounting: ...
Product-Price-Stock-Seller-Parity: ...
Latency-And-Logging-Evidence: ...
Focused-Tests: ...
Remaining-Release-Gates: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
