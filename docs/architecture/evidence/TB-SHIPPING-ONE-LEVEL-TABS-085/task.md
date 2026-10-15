PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-085
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-084
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_AUTHORIZED_ONE_LEVEL_ADMIN_READ_BRIDGE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: f3ce208e7df0c471d318f6e7a40fea9c173889bd
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
084 is ACCEPTED for code-level five-tab shell and tab 1, NOT for live one-level rollout. Evidence commits d3d76ec69a60d5044e652ace408c639ffd654af5 (code) and f3ce208e7df0c471d318f6e7a40fea9c173889bd (evidence). Current one-level gate is OFF and Development one-level tables have NOT been migrated or backfilled. New UI must not remain a disconnected demonstration forever. The priority of 085 is a minimal, trustworthy, server-authorized configuration/read bridge for the new Fulfillment one-level catalog and its gated frontend consumer. DO NOT enable the gate, modify Development schema, run backfill, switch customer pricing, or misrepresent dormant shell as operational. In the 11-task ceiling, 085 focuses on the blocking read bridge; rate-and-coverage tab wiring moves to 086 if not safely possible. Do not hide any unfinished work.

PREFLIGHT — STOP ON CONFLICT

Claim exactly ONE Pending task from tooba-main via BRIDGE-WAKE. Receive and preserve the task artifact as UNSTAGED. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, 081–084 planning notes, 082 Fulfillment Contracts/reader/config gate, 084 page.tsx, shipping-one-level-gate.ts, shipping-one-level-tabs.tsx, relevant Host composition/admin-auth conventions, and existing shipping admin endpoint patterns.
git fetch origin; validate the expected SHA or prove nonoverlapping results/bookkeeping drift only. Preserve all pre-existing dirty/untracked files; NO reset/stash/rebase/force push. Any overlapping source drift => RECOVERY_CONFLICT / STOP.
Confirm the live Development one-level tables are still absent before any attempt to use them. This is READ-ONLY inspection only; no migration or seed. Existing legacy service/option authority is unchanged.

MISSION A — TRUSTED BACKEND ADMIN READ BRIDGE (PRIORITY 1)

Add a minimal module-owned read-only admin capability endpoint (or existing canonical endpoint extension) that reports the effective Tooba:ShippingOneLevelCatalog gate state as evaluated from the authoritative backend configuration. Apply established ADMIN permission and auth conventions; no public/config-secret disclosure, no broad feature flags dump, no new custom auth bypass. Explicit false and missing section => disabled; only explicit true => enabled. This is ONLY a catalog/UI read gate; NEVER infer or change ShippingRateAuthority or ShippingOperationalShadow. Host remains composition-only; no Host business logic.
Reuse IShippingOneLevelCatalogReader (Fulfillment Contracts) for an authenticated, read-only method-list endpoint returning canonical stable method_code, localized label/translation where present, actual activation, and narrowly necessary admin metadata; provider_kind and legacy link may be retained internally but MUST NOT create a second merchant level. Treat missing table/migration as a controlled NOT_READY/disabled outcome (no 500/blank pretending success). Preserve inactive rows in full admin list, distinguish them from bookable active rows, do not synthesize optionless methods from Shipping fallback policies.
Strictly preserve the old Fulfillment catalog endpoints and nested options for compatibility. Neither endpoint writes data or updates legacy service/options. No automatic fallback that pretends a legacy tree is already populated one-level catalog.

MISSION B — FRONTEND TRUSTED CONSUMER (PRIORITY 2, ONLY IF SAFE)
4. Wire existing /admin/shipping-services page/server read seam to the real authorized gate status. Fail closed on network/auth errors. Server/credential context must match existing Next admin architecture; do NOT expose bearer credentials to HTML, log them, or use client query strings/localStorage to activate the feature. Where an RSC cannot safely forward auth, follow the project's existing supported pattern; if none, report the blocker, leaving OFF legacy screen intact.
5. Inject a real loadMethods implementation for tab 1 ONLY when the authoritative gate permits and the catalog is ready; show truthful loading/empty/denied/error states. Keep tab 1's independent rows, stable codes, Persian labels, inactive status, RTL and keyboard tabs. Tabs 2–5 remain honest placeholders until their own tasks; do not rename a placeholder as completed.
6. If completing both A and B risks the hard timebox, deliver A and focused tests, mark B PARTIAL explicitly. Do not exceed time or create speculative integration.

MISSION C — FOCUSED VERIFICATION
7. Focused tests: absent/false/true gate; denied unauthenticated/unauthorized read; 1-level list includes inactive and stable composite codes; code invalid/unavailable fails closed; when OFF or endpoint unavailable the old admin UI and actions are preserved; no ability to enable via URL; NO shipping pricing/Checkout mutation. Run relevant backend focused tests, frontend feature tests and typecheck only if touched.
8. Source audit verifies Checkout still uses Legacy, two original rate/shadow gates OFF. No live Development DDL/DML, no scratch database unless needed for a strictly isolated test with verified provenance, no Host restart or config mutation. If live on/off browser proof is impossible, say so accurately rather than fabricating it. Record pre-existing FE-SIZE failure separately from any new regression.

ALLOWED CHANGE SURFACE

src/backend/Modules/Fulfillment/Tooba.Fulfillment.Contracts/Shipping/** only if a minimal DTO is needed
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Shipping/** only for read-only module-owned query/handler when needed
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/** only for canonical authorized admin read endpoint
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Shipping/** and Infrastructure/DependencyInjection/FulfillmentModule.cs only for necessary read wiring
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/** for narrow tests
src/frontend/app/admin/shipping-services/page.tsx and src/frontend/app/admin/shipping-services/** for gate/read consumer and focused tests
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-085.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-085/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-085.task.md receipt ONLY, UNSTAGED
If canonical routing requires editing a module-owned registration file, enumerate and justify narrowly. No broad guard rebaseline; no unrelated admin-file edits.

FORBIDDEN
No enabling Tooba:ShippingOneLevelCatalog; no live one-level migration/backfill; no option/service destructive conversion, dual-write, schema/paid-order code alteration; no Host business authority; no unapproved permission; no Checkout rate or paid snapshot modification; no enabled ShippingRateAuthority/ShippingOperationalShadow; no fake packed weight, reserved capacity, COD settlement, or delivery promise; no unsafe service restart, unapproved Development writes or production/staging contact. No automatic next task.

ACCEPTANCE / STOP
PASS only if a real authenticated backend read bridge is implemented, its effective gate evaluated securely with default OFF, existing read authority intact, focused assertions green, and any frontend readiness is reported precisely. Mark PARTIAL if frontend cannot be securely connected, tests cannot be run, or environment prevents the end-to-end proof. BLOCKED/RECOVERY_CONFLICT on unsafe target/drift. Hard stop at 30 minutes; report actual elapsed. Finish with ONE truthful canonical result, STOP_FOR_ARCHITECT, automaticNextImplementationTask NONE.

RESULT CONTRACT — BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-085
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-084
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_AUTHORIZED_ONE_LEVEL_ADMIN_READ_BRIDGE
Bridge-Task-Row: <actual>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: f3ce208e7df0c471d318f6e7a40fea9c173889bd
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<exact changed paths, gate/auth/read behavior, readiness, focused tests actually run, limitations, database untouched proof, elapsed time, rollback>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK