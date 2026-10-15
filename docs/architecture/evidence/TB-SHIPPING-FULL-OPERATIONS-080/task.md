PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-FULL-OPERATIONS-080
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-079
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_FINAL_OPERATIONAL_ACCEPTANCE_AND_RECOVERY
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 247f784c6a4f6d4f0cdd564467c64937be22abfd
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD

ARCHITECT RULING
This is implementation task TEN of TEN, the final authorized task in the user-defined 071–080 budget. 079 has been ACCEPTED for its bounded read-only storefront operational-shadow scope. DO NOT imply that technical observation makes customer pricing commercially ready. Finish bounded integration proof, essential safe repairs if objectively detected and within time, and issue a complete, honest final recovery/readiness record. No autonomous task 081. No Checkout authority cutover, capacity-reservation fiction, or real COD collection/settlement. Merchant-approved tariffs/destination rules are NOT established. There is no trustworthy provenance for measured packed weight/physical shipment-item count on the real storefront path. If material dependencies cannot be implemented in the hard timebox, report a precise NOT READY verdict and remaining actionable work, not an invented PASS for production. Development tooba_alpha is disposable but preservation of unrelated fixtures is required. No scratch DB, no unnecessary Host restarts, no broad-suite loops.

PREFLIGHT — STOP ON CONFLICT
Claim exactly one Pending task on tooba-main after BRIDGE-WAKE. Preserve docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-080.task.md verbatim, UNSTAGED. Read AGENTS.md; docs/ai/TOOBA-PIPELINE-PROTOCOL.md; 070–079 Shipping planning/evidence; latest Order storefront projection/operational shadow; Shipping Contracts operational quote; rate tiers, packaging, schedule and admin inspector; recovery/master architecture tracking. Fetch origin/main and verify exact Expected-Base-SHA or only explicitly proved nonoverlapping evidence/bookkeeping drift. Any overlapping source change => RECOVERY_CONFLICT/STOP. Keep existing dirty/untracked files untouched. No reset/stash/rebase/force-push. Before any Development probe, verify actual GET /__platform-commerce identifies dev-singlestore/store-alpha/tenant-alpha and tooba_alpha; do not assume old host PID. No staging or production contact.

MISSION A — FINAL OPERATIONAL ACCEPTANCE MATRIX (PRIORITY, MANDATORY)

Independently reconcile all 071–079 deliverables against actual source and recorded evidence, not merely worker PASS. Provide a concise checklist: service and option ownership; geography selection; rates plus half-open weight/count tiers; packaging and payer modes; weekly operating windows; capacity-config-only status; admin UI entry points; operational diagnostic; read-only real storefront shadow; current checkout authority. Cite concrete file paths and code/evidence commits in final documentation.
Verify the actual 079 shadow flags remain OFF by default (Tooba:ShippingOperationalShadow:Enabled and PrerequisitesConfirmed); earlier Tooba:ShippingShadow remains unchanged; both are observation-only. Check current Dev process options or actual OFF probe without indiscriminate restarts. Show, as directly measurable, legacy storefront response parity and zero customer pricing/paid-snapshot mutation. Prefer reusing 079's byte-level evidence plus one focused current-state probe over rerunning its whole matrix.
Check Shipping admin UI discoverability and interoperability: existing /admin/shipping-services row actions for rates and coverage, operating schedule, packaging and freight payer, operational quote inspector. Confirm tier-condition CRUD, schedules, packaging settings remain readable and no legacy panel action is broken. If no browser access/time, say explicitly what is source-inspected rather than claiming real interactive verification.
Audit requested user capabilities ONE BY ONE and mark IMPLEMENTED_AND_PROVEN, CONFIGURED_ONLY, FUTURE_FACING, NOT_IMPLEMENTED or BUSINESS_APPROVAL_REQUIRED: weekdays; multiple time windows; timezone; dispatch vs promised delivery; capacity limit/reservation; weight intervals; physical item-count intervals; rate options; geography; packaging; prepaid; COD; weight/count provenance from real packed shipment; final checkout pricing; COD financial ledger/settlement; delivery promise; commercial tariff/coverage approval. Identify OWNER for every incomplete capability and whether it blocks a safe customer cutover.
Explicit business gate: a merchant must validate all active commercial rates, packaging fees, destination coverage, collection duties and seller preparation promises. Synthetic Alborz fixtures and deactivated tier/packaging rows from tests are not merchant approved. Document the minimum data and approval actions needed; do not activate test policies. Highlight missing Fulfillment tree row for snapp_courier if still observable, or call it unverified.

MISSION B — TARGETED REMEDIATION IF FOUND, BOUNDED
6. Fix only a demonstrated SEVERE integration defect inside existing Shipping admin/frontend or operational observer that breaks user-required behavior and can be safely proven and committed within 45 minutes. Do not widen the mission or start a new architecture stream. Examples: a clear stale serviceCode request on closed dialog, explicitly reproducible wrong tier unit conversion, unexpected effect on checkout response or false positive availability. If no such defect, make NO speculative source edits.
7. Weight/count provenance is an architectural blocker: inspect Fulfillment + Order contracts to identify whether measured shipment packed units and grams exist in an authoritative path. If not, record missing contract and owner, with implementation plan scoped to a future separately authorized program; DO NOT derive from cart lines, quantities or arbitrary client input. Do not add placeholder default measurements or silently ignore a conditioned active rate.
8. Atomic capacity booking: do not implement or claim from configuredCapacityTotal. Record exact missing order↔window reservation, idempotent claim/release, concurrency constraint, expiration/cancellation and ledger ownership. Classify as NOT READY for guaranteed slot sales. COD accounting: payer-mode fact does not create a Payment/Settlement transaction. Never mutate those modules under this task.

MISSION C — RECOVERY / DELIVERY RECORD
9. Update canonical master Recovery shipping checkpoint if its owner/file is known and in allowed path, by a small append-only truthful 071–080 chronology, respecting existing style. Otherwise create a handoff ready-to-merge document and state the reason master update was not safe; do not overwrite unrelated planning decisions.
10. Write docs/planning/shipping/SHIPPING-FULL-OPERATIONS-080.md as final decision record: (A) What works today in Development; (B) observed evidence and commands; (C) what remains blocked and who owns it; (D) whether Checkout may be switched on (answer must remain NO absent separate explicit merchant/architect approval and working settlement/provenance); (E) Dev fixture inventory and restored OFF gates; (F) rollback and exact SHA references; (G) final score 10/10 task budget consumed, not 100% commercial completion. Include specific next steps but do not generate a next Bridge task. Evidence under docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-080/ only, no credentials or 8-MB logs.
11. Run only focused checks needed for changed source plus read-only Development assertions. If a change is made, run its smallest meaningful test and type/build guard; if no source changed, source inspection plus recorded 079 evidence and one current-state verification may suffice. No broad test suite, migration, restart unless strictly needed. Stop at exactly 45 minutes or earlier; partial report is better than overrun.

ALLOWED PATHS

docs/planning/shipping/SHIPPING-FULL-OPERATIONS-080.md
docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-080/**
docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-080.task.md (Bridge receipt ONLY, NEVER stage)
docs/planning/RECOVERY.md ONLY if that is verified canonical master and small append-only Shipping section is safe; otherwise no edit
src/frontend/app/admin/shipping-services/** ONLY for a reproduced severe defect in this exact task, with focused proof
src/frontend/app/admin/shipping-services-screen.tsx ONLY for a reproduced severe panel integration defect, minimal edit
src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/OperationalShadow/** ONLY for a proven severe observer defect with focused tests
src/backend/Modules/Order/Tooba.Order.Tests/Storefront/** ONLY for corresponding focused regression tests

FORBIDDEN
No new implementation program, no automatic next task, no real Checkout price cutover, no Order paid/snapshot writes, no Payment/Settlement ledger, no manual fake COD completion, no reservable capacity claim, no invented packed weight/count, no production/staging writes, no new migrations, no fabricated commercial acceptance, no Host/Geography/Fulfillment source edits, no security/permission changes, no unrelated dirty-path edits, no uncontrolled reboot/restart, no force push/rebase/stash/reset, no giant logs.

ACCEPTANCE / STOP
PASS for the FINISHING TASK only if a source-grounded, evidence-linked final acceptance and truthful commercial NO-CUTOVER decision record is committed, safety gates verified, known gaps individually assigned and any actually modified code specifically verified. Technical program closure and merchant-ready production go-live MUST be scored SEPARATELY. PARTIAL if final gate verification or recovery reconciliation cannot be completed in the bounded time. BLOCKED on missing access/unsafe target; RECOVERY_CONFLICT on overlapping source drift. Hard stop 45 minutes and report actual elapsed. STOP_FOR_ARCHITECT, automaticNextImplementationTask NONE.

RESULT CONTRACT — CANONICAL BRIDGE-WAKE-V1
Return exactly one complete worker result using this field order, actual values rather than placeholders, evidence body after automaticNextImplementationTask, no extra text after END_TOOBA_TASK.
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-FULL-OPERATIONS-080
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-079
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_FINAL_OPERATIONAL_ACCEPTANCE_AND_RECOVERY
Bridge-Task-Row: <actual>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 247f784c6a4f6d4f0cdd564467c64937be22abfd
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<scope and proof; acceptance matrix and owners; source/Code/Evidence SHAs; current OFF gates; frontend admin/real storefront parity; weight/count provenance status; configured-only capacity; COD settlement; commercial tariffs; Recovery update; targeted tests; exact elapsed; changed paths; safety rollback; cutover verdict>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK