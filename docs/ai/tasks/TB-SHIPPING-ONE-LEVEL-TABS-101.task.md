PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-101
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-100
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_SHARED_DEVELOPMENT_STOREFRONT_LABEL_READ_ONLY_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 5e7c9c14117da4e94ae0e459f054d37d3b312124
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD. Stop at deadline and report PARTIAL or BLOCKED; do not overrun.

ARCHITECT RULING
100 accepted for the narrow Fulfillment Contracts-only label projection and Order label substitution, NOT for end-to-end live rendering. The immediate goal is to prove that the already-authored one-level translation is actually displayed by the real Development storefront projection when the catalog flag is explicitly on, with the SAME eligible methods, immutable codes, prices and lead times. This is deployment/read-only proof, not a Checkout cutover. Prior three tasks overran the timebox; do not repeat oversized exploration or unrelated tests.

PREFLIGHT / FAIL-CLOSED

Claim exactly one Bridge pending task; preserve task receipt UNSTAGED; fetch origin/main, validate Expected-Base-SHA ancestry and inspect any intervening commit. Non-overlapping result-bookkeeping drift only; conflicting src drift => RECOVERY_CONFLICT and STOP. Preserve pre-existing dirty/untracked files; no reset/stash/rebase/force-push.
Read AGENTS.md, pipeline/controller, 093 host lifecycle, 098 deployment, 100 projection design, and scripts/dev/host-stack.ps1. Positively verify the running shared Development Host listener, managed PID ownership, /health and /__platform-commerce tuple (SingleStore, dev-singlestore, store-alpha, tenant-alpha); ensure storefront Next target is the same :5088. STOP if the target differs, prior state cannot be recovered, or ownership/rollback is not proven.
Before deploying, capture a READ-ONLY baseline: the active ShippingOneLevelCatalog flag, one-level name/translation for post:standard in the correct requested language, legacy customer label, current eligible method codes and money values. Record the exact HTTP request/preconditions required to reach the storefront projection (authentication, cart scope, destination). Do not invent session cookies or create orders. If no safe existing read-only path reaches projection, report BLOCKED_REAL_STOREFRONT_PROOF with exact reason.

MISSION A — CONTROLLED CURRENT-CODE HOST / NO DATA WRITES

Determine if current Host already contains the 100 Order label seam. If yes, do not restart. If stale, build once in a task-owned isolated output directory, then use ONLY scripts/dev/host-stack.ps1 with proven managed-PID ownership, explicit provenance, documented rollback and the SAME ShippingOneLevelCatalog gate state as preflight; do not restart Next unless positively necessary and separately justified. No blind kill, no force, no takeover of an unowned process.
The only permitted gate is ShippingOneLevelCatalog (Development process environment only). Never enable ShippingRateAuthority or ShippingOperationalShadow. Never edit appsettings, migrations or seed data. Do not create a second shared Host. Capture exact build hash, PID, gate and provenance after rollout.
If a safe owned restart cannot be proved within the timebox, STOP and report the precise blocker; keep the shared stack as found.

MISSION B — REAL STOREFRONT LABEL PROOF, READ-ONLY

Use an existing legitimate storefront read-only shipping projection (or existing customer-facing method-list screen/API) with known safe preconditions, without order/checkout submit, payment, draft creation or any DML. Prove for an eligible, configured method such as post:standard: returned methodCode is unchanged and the displayed label equals the Fulfillment one-level translation for the requested language (e.g., currently persisted «معمولی»); explicitly distinguish it from the legacy combination «پست — معمولی». Do not assume «پست معمولی» is currently persisted: 094 rolled that sample name back.
Compare against an independently grounded gate-OFF baseline: use existing baseline evidence or a genuinely reversible gate-OFF process restart ONLY if safe and time permits. Document where a comparison is unit-test-only instead of claiming live gate-OFF proof. Assert no eligibility-set, method-code, quoted amount, isFree, lead-day or sort change. Inactive post:express/snapp_courier must not become newly eligible. If language translation is absent, the exact legacy fallback is expected, not a failure.
Prefer real HTTP evidence; use a real browser screenshot only if the storefront method-list UI can be reached safely and read-only. Do not use a guessed/test-only actor in a shared browser, alter an order/cart, or call payment-start. Inability to reach the page is PARTIAL, not an invented PASS.
One bounded diagnostic pass only. No broad Host test suite (100 reported 45 pre-existing failures), no unrelated architecture repair, no price/rate changes, no activation writes.

MISSION C — EVIDENCE, RESTORE, HANDOFF

Record docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-101.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-101/** with time-stamped baseline, exact read-only requests and results (redact credentials), methodCode / label / price comparison, gate/provenance, host lifecycle or reason none, DB parity read-only if needed, elapsed time, and rollback procedure. Do not store tokens/session IDs, passwords or personal information in git.
Restore any task-induced temporary process or flag changes; if preflight gate was ON, leave it ON. Preserve shared :3000 and all existing data. Commit/push task-owned docs/evidence only; no src/** changes authorized. STOP_FOR_ARCHITECT, no next task.

ALLOWED PATHS

docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-101.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-101/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-101.task.md as UNSTAGED receipt ONLY
scripts/dev/host-stack.ps1 EXECUTE only, DO NOT EDIT
No source, test, frontend, schema, migration, configuration or recovery-master changes.

FORBIDDEN

Shared Development DDL/DML or activation/name edit, checkout/payment/order/draft write, frontend code patch, fake fixture in real DB, unauthorized auth bypass, standalone replacement of main process, staging/production access, timebox overrun.

ACCEPTANCE
PASS: real current-code Development HTTP/browser storefront shipping method list proved with an existing eligible method showing exact language-specific one-level name and stable code/price/eligibility; safe deployment and unchanged shared state documented. PARTIAL: Host updated/safe and projection unable to be exercised, or only partial read evidence. BLOCKED: no safe authenticated read path or no safe deployment ownership. Never mark commercial Checkout cutover READY based only on a label test.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Program, Mode, Bridge-Task-Row, Status PASS/PARTIAL/BLOCKED, Stop-State STOP_FOR_ARCHITECT, Expected-Base-SHA, Base-SHA, Code-SHA NONE, Evidence-SHA, automaticNextImplementationTask NONE, changed paths, actual requests/results and tests, price/method invariants, gate/provenance, elapsed time, rollback, blockers.
END_TOOBA_TASK
