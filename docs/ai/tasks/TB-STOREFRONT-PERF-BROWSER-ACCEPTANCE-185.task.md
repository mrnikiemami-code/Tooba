PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-STOREFRONT-PERF-BROWSER-ACCEPTANCE-185
Parent-Task: TB-STOREFRONT-PERF-PROMOTION-BATCH-184
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 96f9c0876e81bd0c05270aa0ef0d6cac0db31062
Program: TOOBA-STOREFRONT-PERFORMANCE-RELEASE-CLOSURE
Mode: BOUNDED_READONLY_REAL_BROWSER_PERFORMANCE_ACCEPTANCE
Hard-Timebox: 45 minutes HARD; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Close the remaining technical browser evidence for PERF-CRIT-001 following TB-184 Promotion-owned batching, without treating a private test as the user's visual acceptance or overwriting their running apps. Use the already proved TB-182/183/184 published fixture recipe; avoid extensive new fixture generation unless necessary. Confirm that a genuine Next browser renders Home and its product/card content, keeps Persian storefront visual integrity, and measures Home network/proxy and browser rendering on a safe isolated loopback stack. Distinguish what is technically demonstrated from the user's still-required subjective responsiveness verdict. This task is ONE bounded acceptance slice, not a storefront redesign.

MANDATORY FIRST CHECK
Verify HEAD == origin/main == Expected-Base-SHA and dirty files; if mismatch STOP_FOR_ARCHITECT without edits. Read docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md, docs/planning/MUST-FIX-BEFORE-RELEASE.md, TB-183 and TB-184 evidence and relevant live Home reader/Next proxy paths. Inspect current code, published fixture strategy and available browser harness before starting containers. Do not blindly repeat slow owner seed/publish; if bounded setup cannot fit remaining time, record precise blocker.

BOUNDED ACCEPTANCE

Use a disposable Postgres/Host/Next stack on unique free loopback ports and tenant fixture, with fail-closed isolation guard BEFORE any write. No change to shared :3000/:5088/:5432, tooba_alpha, store-alpha, shared .next, Redis, RabbitMQ or existing process. Authoritative owner routes and genuine auth only for fixture prep; no cloned or manually published SQL rows, fake permissions or test bypass.
With enough genuinely Published items to inspect actual Home cards (reuse accepted TB-184 recipe and stop when adequate), run a real headless browser through the existing Next Home page at /fa, not only direct API calls. Record authentic screenshots, DOM checks for prices, seller, IRR, availability, product identity and any existing promotional cards, failed requests and rendering/runtime errors. Compare visible identity and amounts to same fixture's authoritative /v1/storefront/home output; do not claim user-browser approval.
Capture at least one cold navigation and three warm reload/navigations including Next->Host network timings, backend /home HTTP status and wall time, client DOM-ready/render completion and meaningful loading/error state; keep dev cold-build time distinct from warm runtime. Compare to documented TB-183/184 observations only with environment/fixture caveats. Do not infer p95 or production SLA from three requests.
Validate no changed storefront look/layout or product count, discount/price/stock/seller regressions; capture full screenshot only if genuinely obtained. Run only focused tests IF code must change; prefer evidence-only. No production flags toggled, no shared restart/cache purge. If Next dev browser cannot be made robust without crossing isolation boundary, report PARTIAL and stop.
Architect may update PERF-CRIT-001 only with measured evidence. Do not mark CLOSED absent actual user validation of responsiveness in their real browser and an explicit architect decision; document exactly what remains, provide simple reproducible user steps. If no code changes, do not run broad suites. Do not add a future task or schedule automatically.

OWNERSHIP / SAFETY
Promotion batch uses the Promotion module per-scope candidate source; preserve Contracts-only crossings, CQRS/ISender, existing Result conventions, no Host business code or Results.Json. Do not alter Checkout, Shipping, payment, delivery windows, global CSS or translations. Preserve unrelated dirty admin-fulfillment-work-queue-screen.tsx, TB-075/079 logs, TB-173 evidence edits and all stashes. No reset/stash/rebase/force-push; only scoped fast-forward commit. Never retain credentials in traces or screenshots. Cleanup exact task-owned listeners, scratch data and processes, confirm shared stack untouched.

ACCEPTANCE VERDICT
PASS only for genuine isolated Next browser Home end-to-end with measured navigation/network, authenticated source-backed card parity, no rendering errors, genuine evidence, cleanup and honest remaining user acceptance gate. PARTIAL if required browser or seeded fixture evidence is missing. BLOCKED if isolation/auth/source authority cannot be preserved. Never infer all-release approval or production deployment from PASS.

EVIDENCE
Commit task receipt in docs/ai/tasks/TB-STOREFRONT-PERF-BROWSER-ACCEPTANCE-185.task.md and sanitized records in docs/architecture/evidence/TB-STOREFRONT-PERF-BROWSER-ACCEPTANCE-185/ (RESULT.md, browser screenshot/DOM assertions, network measurements, isolation guards, fixture provenance, cleanup). Report exact commands, metrics, baseline caveats, test counts, changed files, SHA and time; no fabricated browser proof.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-STOREFRONT-PERF-BROWSER-ACCEPTANCE-185
Parent-Task: TB-STOREFRONT-PERF-PROMOTION-BATCH-184
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
Real-Browser-Proof: ...
Measured-Next-Host-Timings: ...
Product-Price-Seller-Promotion-Parity: ...
User-Acceptance-Still-Open: ...
Focused-Tests: ...
Remaining-Release-Gates: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
