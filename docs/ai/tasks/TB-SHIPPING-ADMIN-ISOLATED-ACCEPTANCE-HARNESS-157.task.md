Persisted claim artifact — full Architect body received via Bridge
`GET /api/tasks/next?channelId=tooba-main&workerId=tooba-worker-01&agentType=cursor`
at Bridge task UUID `b14563ef-a320-4c9d-89cd-e4b9f6dcce21` (createdAtUtc 2026-10-16T20:13:23.6580299Z).


PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157
Parent-Task: TB-SHIPPING-METHOD-FINANCIAL-EDIT-156
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 2b798560b9d3dd18e5d5a8c92c919c5fcbf82838
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_REUSABLE_PRIVATE_ADMIN_RUNTIME_AND_REAL_MONEY_WRITE_READ_ACCEPTANCE
Timebox: HARD 45 minutes; stop and report PARTIAL/BLOCKED if not provable; no unbounded retries.

MISSION
Close TB-155/156's missing REAL authorization/HTTP/browser evidence and produce a reusable, safe, documented private Admin acceptance harness, without using shared Host/DB/browser. This is implementation and acceptance, not another design or wireframe task.

FIRST: verify exact origin/main HEAD, worktree boundaries, task and recovery documents; STOP if base differs. Read docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md, docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md and TB-155/156 evidence. Reuse the TB-152/154 scratch recipes rather than inventing a parallel app architecture.

IMPLEMENTATION (single bounded task)

Create a durable, rerunnable task-scoped harness script and concise README under docs/architecture/evidence/TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157/ (or an existing approved developer-tooling location if better). Launch a new disposable Postgres 16 scratch container and databases, private Host built from THIS HEAD with SingleStore and scratch tenant, and private Next frontend bound only to private Host. Reserve unused loopback ports, prove identity/isolation before any mutation; no shared :3000/:5088/:5432, tooba_alpha, tenant-alpha, existing store-alpha database, RabbitMQ, Redis mutation. Do not disable authorization or use fabricated access headers.
Seed minimal Development-only ADMIN ACTOR authorization through the actual supported permission/tenant grants (including tenant#view and fulfillment.view and the normal rate-edit grant as genuinely required); existing sanctioned dev actor may be used only with legitimate persisted grants in scratch. No privilege fallback, unsafe global allowlist or permission bypass. If proper grants cannot be seeded, stop PARTIAL and document exact missing authority.
Load real flat one-level catalog, its server-owned method bindings and language registry. Authenticate the private Admin UI via sanctioned dev-only workflow. Verify GET /v1/admin/shipping-services/{serviceCode}/financial-status?optionCode=... live HTTP 200 and authorization fail-closed for missing permissions (401/403); capture sanitized response.
For canonical tipax:standard, inspect its actual initial status (NeedsConfiguration/LegacySharedUnresolved/FinanciallyConfigured/Conflicting). Using the existing Shipping module canonical rates POST/PUT seam (never new overlapping endpoint), create/update one genuinely owned fixed IRR rule ONLY when the verified gate permits; verify GET reflects persisted amount, correct currency and source, then reload real private Admin Rates & Coverage/General screens. Confirm tipax:express remains byte-identical, legacy provider-level rule unchanged; screenshot real UI only if captured by actual browser. Confirm rejected conflicting/unauthorized writes do not mutate either method. Never interpret stored 0 as FREE and never make a missing tariff free.
Where safe and supported in this bounded run, verify authoritative storefront read/quote for that selected method reflects updated owned rate; if the calculator does not consume this owner seam, report EXACT dependency gap with evidence (do not modify checkout/pricing opportunistically or claim financial readiness). Never invoke final payment, PSP, SMS or settlement payout.
Capture focused backend/frontend tests and critical HTTP/browser assertions with actual statuses; redact credentials, OTP, bearer/cookie/connection strings; no secrets in commits. Ensure cleanup ALWAYS stops only task-owned PIDs matched by executable path, removes only task-owned containers and proves private ports released/shared services still healthy. If timeout hits, cleanup and STOP.

INVARIANTS
Four flat method tabs, exactly three row actions, multilingual real languages and RTL remain; methodCode identity always bound by authoritative server, optionCode never parsed client-side; sibling provider options never share write authority or pricing; activation != financial readiness; no production DB writes, migration, global CSS or Host business authority. No edits to unrelated dirty files, stashes, TB-075/079 logs. Do not rebase/reset/autostash/force push.

ACCEPTANCE & VERDICT
PASS only with authentic private Admin authorized save -> Shipping DB read -> status GET -> real private browser reload plus sibling isolation and 401/403 guard, and a reusable safe harness. If a component cannot be proved within timebox, PARTIAL with exact blocker and next smallest fix. Include explicit storefront-quote status as PASS/FAIL/UNVERIFIED; absence of quote proof does not authorize invented production rates. Cite measured assertions and real screenshots, no mock UI or fabricated samples. Commit only scoped scripts, necessary smallest safe fixes (only if strictly necessary and tested), and evidence; push fast-forward main. Report Base/Code/Evidence/HEAD/Origin SHA, tests, changed paths, isolation/cleanup, remaining gaps, Status and STOP_FOR_ARCHITECT; Automatic-Next-Task: NONE.
END_TOOBA_TASK
