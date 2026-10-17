PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-118
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-117
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_READ_ONLY_STOREFRONT_SHIPPING_ONE_LEVEL_PARITY_AUDIT
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 6f0105e4c573dd7550439974b24ea2fdddd0c9db
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD; stop investigation by minute 32, reserve remainder for concise evidence and Bridge report. No loops, no open-ended exploration, no broad test sweeps. One bounded verification attempt per selected seam; if blocked, capture the exact blocker and STOP.

ARCHITECT RULING
TB-117 is accepted as isolated Development Host STARTUP proof only: all six legacy Catalog bootstraps completed; health 200; no platform.edition.unconfigured. No storefront/Shipping/Checkout behavior was certified. The next commercial question is narrow: can the currently existing one-level Shipping UI path and its actual option/label data flow be traced and proven without changing shared state? This is OBSERVATION AND READ-ONLY PROOF ONLY. Do not conflate seed row presence with actual storefront eligibility, and do not claim cutover readiness from source inspection alone.

PREFLIGHT — FAIL CLOSED

Claim exactly one Pending task on tooba-main; preserve its Bridge receipt/task file UNSTAGED. Read AGENTS.md and the relevant bridge rules, TB-117 accepted evidence, and the previous one-level Shipping UI planning/evidence. Check origin/main. Expected-Base-SHA must be an ancestor. Document any non-overlapping bookkeeping drift. If source overlap or ambiguous dirty state is relevant to this proof: RECOVERY_CONFLICT and STOP. Never reset, stash, rebase or force-push. Do not adopt pre-existing frontend dirty files.
Record read-only identity of shared :5088 Host and :3000 Next (PID, health/HTTP responses and /__platform-commerce) only if accessible. Confirm the current shipping feature flags/configuration before attempting a one-level claim. If one-level path is disabled or cannot be reached, say so explicitly; do not toggle flags.

MISSION A — TRACE THE USER-VISIBLE SHIPPING FLOW (BOUNDED)

Identify the concrete existing storefront Shipping page route and the exact component(s) rendering the shipping section/tabs, option labels, selection state and transition toward Payment. Trace at most ONE representative path from frontend call to API endpoint through the owning module/Contracts and back to rendered data. Record exact source paths/line ranges and response shapes. No global inventory of Shipping code.
Determine from direct code plus available read-only HTTP whether the one-level tab/label surface is already accessible under the current shared configuration. Separate PROVEN UI observation, SOURCE-ONLY inference and BLOCKED/NOT ENABLED. Do not synthesize a test customer, cart, order, authenticated session, actor or request context; do not replay private identifiers or mutate a cart by GET if the endpoint has side effects.
Focus on three questions only: (a) does the displayed one-level UI show the expected label/option mapping? (b) is a shipping option selected/retained only through its proper owner and not as a side effect of merely navigating toward Payment? (c) which exact gate, endpoint or state prevents a real nonmutating acceptance proof, if any? Do not broaden to Outbox, Returns/Settlement, migration, seed, inventory or unrelated Checkout defects.

MISSION B — MINIMAL VERIFICATION

Prefer direct read-only checks and inspection of existing focused tests/evidence. If targeted automated verification is essential and has a known small test filter, run it ONCE. No rerun without an identified environment-only flake and architect-authorized need; no comprehensive frontend/backend test suite or slow UI loops. Do not perform writes, create orders, select shipping options, finalize carts, simulate payment, or activate feature gates against the shared database.
If read-only proof is insufficient because a flow requires authenticated state or a mutation, STOP with an exact acceptance gap and a minimal, separately authorizable isolated-runtime test proposal. Never change production/shared data to force PASS.

MISSION C — REPORT AND STOP

Write docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-118.md and minimal redacted evidence under docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-118/. Summarize: status of each of three questions, source-to-UI chain, direct observations and provenance, flags, first concrete blocker or next minimal isolated proof, existing shared parity, tests run (if any), elapsed minutes and rollback (docs only).
No src/, test, frontend, migrations, appsettings, feature-gate or DB edits. Preserve all unrelated dirty/untracked files. Commit only task-owned docs/evidence, push origin/main normally, verify HEAD==origin/main, preserve Bridge receipt UNSTAGED.

ACCEPTANCE
PASS if the exact currently accessible one-level Shipping presentation/data/selection behavior is substantiated through direct read-only observations (not only code), with stated boundaries and no shared mutation. PARTIAL if the source path is grounded but practical proof is blocked by feature gating/auth/state; specify a single next minimal proof. BLOCKED on repository conflicts, inability to identify authoritative owner safely, or threatened shared-data integrity. A healthy Host or populated scratch templates alone never qualifies as Shipping acceptance.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Bridge-Task-Row, Expected-Base-SHA, actual Base-SHA and drift, Code-SHA NONE, Evidence-SHA, Status PASS/PARTIAL/BLOCKED, STOP_FOR_ARCHITECT, automaticNextImplementationTask NONE, measured minutes, three-question verdicts, concrete source/HTTP evidence, optional focused test counts, shared parity, exact changed paths, rollback and first remaining blocker. Never start next task automatically.
END_TOOBA_TASK