PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-STOREFRONT-PERF-SCALE-CLOSURE-180
Parent-Task: TB-STOREFRONT-PERF-SCALE-ACCEPTANCE-179
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 174dd8171dcb6d233c1a6c1ba8fd56440f988d9e
Program: TOOBA-STOREFRONT-PERFORMANCE-RELEASE-CLOSURE
Mode: BOUNDED_TWO_SIZE_CATALOG_SCALE_AND_RESPONSE_PARITY_PROOF
Hard-Timebox: 45 minutes HARD; report PARTIAL and STOP rather than overrun.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Close the TWO measured gaps of TB-179 for PERF-CRIT-001, using reproducible, owner-correct, isolated evidence: (1) a nonempty small catalog versus a >=4x larger catalog, with comparable request-level command and latency measurements; (2) identify the 14,851 vs 16,279-byte cross-logging-leg response delta by semantic field comparison, excluding only verified nondeterministic fields and NEVER silently declaring parity. Do not redo N+1 refactoring or the already measured EF logging repair. Deliver runtime proof, not a fresh speculative report.

MANDATORY FIRST CHECK / NAMING AND OWNERSHIP GUARDS
Fetch origin/main and verify HEAD == origin/main == Expected-Base-SHA before doing anything; otherwise STOP_FOR_ARCHITECT. Read docs/planning/MUST-FIX-BEFORE-RELEASE.md PERF-CRIT-001, docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md, TB-178 and TB-179 evidence/commands and existing storefront Home fixture + seeding ownership. Preserve established repository path<->namespace and capability-first folder conventions; run existing relevant architecture/naming guards if production source is changed. Do not add generic technical-axis roots, duplicate demo-seed API, or Host business logic. Any new test follows exact owner/module naming convention. Note explicitly that TB-179's POST /v1/admin/catalog/demo/seed-only stalled for 7 minutes and produced no scale growth: DO NOT repeat it without first establishing a bounded cause and termination control.

BOUNDED EXECUTION
A. Inspect the canonical owner-approved Catalog/Offer/Pricing/Inventory/Party/Localization demo bootstrap and existing test fixtures. Find a deterministic and safe way to generate an additive >=4x realistic catalog in a disposable scratch instance, with valid owner-managed product/variant/offer/seller/price/stock/localizations and no cross-tenant errors. Prefer existing bounded owner seed/test helpers and supported public APIs. An explicit SCRATCH-ONLY fixture may be created if it uses owner-correct schema/constraints and records precise provenance/validation; never clone arbitrary rows, fake missing business invariants, edit production/shared data or expand an unrelated generic seeder. If this cannot be done safely within timebox, STOP PARTIAL with the exact missing owner fixture and do not fake a scale ratio.
B. Use a fresh task-owned loopback Postgres + Host (and Next only if essential). Fail-closed isolation guard: unique database/container/ports, one Active scratch tenant, no :5432/tooba_alpha, no shared :3000/:5088/store-alpha, no shared RabbitMQ/Redis changes. Canonical MigrationRunner for owner schemas. Ensure all task-owned resources are cleaned by exact PID/container identity. Never purge shared .next caches.
C. Measure same /v1/storefront/home?locale=fa request with identical tenant, host header, binary, locale and explicit settings. For EACH size A and B record actual Catalog products/variants, Offers, active Prices, Stock, sellers and localizations; perform one warm-up plus >=3 warm requests; capture wall times, payload sizes, status, hashes, structured per-request command counts (correlation IDs or explicit bounded diagnostics). EF Information may be enabled ONLY in isolated diagnostic leg. Differentiate full-process startup logs from per-request commands. Keep default EF Warning leg for stdout volume comparison; never call command-log lines a query count without proper request scope.
D. Recheck TB-179 cross-leg delta with controlled identical database state and comparable snapshots, and compare parsed responses by stable product/offer/price/stock/seller fields. Identify exact changing JSON paths and root provenance (caches, timestamps, request-dependent fields, data mutation, etc.); do NOT edit product behaviour solely to equalize hashes. If no reliable parity is possible, report exact mismatches and reason, with issue OPEN. Never assume byte inequality means product-price inequality or vice versa.
E. Prove not merely fewer rows: record per-item and fixed-overhead DB command growth A->B, median/slowest warm latency, HTTP 200, and product/price/stock/seller identity parity with source DB for both sizes, and any missing fields. Do not hide products, change query limits, raise proxy timeouts, or weaken tenancy/auth. If customer UI performance is not tested, state it is unverified. Minimal focused tests only for newly changed code.

SAFETY AND ACCEPTANCE
No unapproved financial/packaging charge changes; TB-177 remains BLOCKED pending payer/tax/allocation authority. No Shipping/group/Admin, Order, Payment, Checkout, Fulfillment, Settlement, PSP or payout change unless demonstrably essential to owner fixture and expressly within bounded scope. No Results.Json, no direct cross-module DbContext consumers, no migration of shared DB, no global logging suppression, no stashes, rebase, reset or force push. Preserve unrelated dirty Admin and evidence files. Keep the current committed EF Warning defaults and active shared services unchanged.
PASS ONLY if genuine >=4x two-size scale comparison AND normalized item-level price/stock/seller parity AND the cross-leg delta is explained/proven, with sanitized reproducible evidence and zero shared writes. Otherwise PARTIAL/BLOCKED with precise blocker and no fabricated claim. Do NOT close PERF-CRIT-001 unilaterally: Architect/user acceptance and user-perceived responsiveness remain separate release gates. Hard stop at 45 minutes, no repeated slow retries.

EVIDENCE / RESULT
Save task receipt at docs/ai/tasks/TB-STOREFRONT-PERF-SCALE-CLOSURE-180.task.md and sanitized evidence under docs/architecture/evidence/TB-STOREFRONT-PERF-SCALE-CLOSURE-180/. Include two-size row counts, request trace/command matrix, response field diff, payload parity, method and seed provenance, any focused tests, exact git diff, timing and cleanup. Push fast-forward only. Report worker result following the exact envelope.
END_TOOBA_TASK
