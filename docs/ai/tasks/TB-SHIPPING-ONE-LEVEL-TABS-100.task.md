PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-100
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-099
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_STOREFRONT_ONE_LEVEL_LABEL_PARITY
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ec5e693376b56d94441e017df4783e136c63525a
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD. Stop at deadline; return PARTIAL/BLOCKED instead of overrunning.

ARCHITECT RULING
099 ACCEPTED: the authorized option-backed activation API was proved with genuine isolated writes/revert and zero shared-state mutation. The next merchant-facing mismatch is factual: the 094 one-level method name is persisted in fulfillment.shipping_method_translations, but the customer-facing method label is still assembled from legacy shipping-service/option translations. Align labels ONLY, via the owning Fulfillment module and Contracts-only cross-module seam. Preserve the legacy storefront eligibility, enabled codes, shipping rates, checkout charge authority, methodCode, historical paid snapshots and all financial values byte-for-byte. A display-name edit MUST NOT silently activate a service or change a customer's price. This is a bounded read-side operation, not a pricing cutover.

PREFLIGHT — STOP ON CONFLICT

Claim one pending task through Bridge, preserve its receipt UNSTAGED, fetch origin/main and validate Expected-Base-SHA ancestry. Allow only non-overlapping result-bookkeeping drift; conflicting src drift => RECOVERY_CONFLICT. Preserve every dirty/untracked file; no reset, stash, rebase or force push.
Read AGENTS.md and controller/pipeline, 094 and 095 activation/name findings, 098–099 reports, Fulfillment Contracts, and Order StorefrontShippingService (legacy label/eligibility construction). Inspect exact DTOs, language selection, method code composition and any existing Fulfillment contract for one-level catalog/labels before coding.
Explicitly distinguish the labels from pricing and eligibility. Inspect consumers of the storefront shipping projection and whether names are captured in order snapshots. If the smallest read-only seam cannot preserve the contracts and historical semantics within 30 minutes, STOP with BLOCKED_LABEL_PROJECTION_AUTHORITY or PARTIAL; no invasive Order or Checkout refactor.

MISSION A — SMALL, OWNERSHIP-CORRECT LABEL PROJECTION

Design a Fulfillment-owned READ contract returning the configured one-level display name for an exact existing canonical methodCode and requested language; use the established Contracts-only cross-module boundary and module DI convention. Never query Fulfillment tables from Order, and never infer one-level names by splitting codes in the browser. Reuse an existing contract if it is suitable; do not duplicate catalog readers.
In the legacy storefront's existing method projection, keep the exact eligibility source (legacy service/option is_active, EnabledCodes, rate config) and immutable code as-is. Substitute ONLY the final human-facing display label when a valid matching one-level translation is available AND the explicitly enabled one-level catalog is ready; otherwise use the EXACT prior legacy label. Gate-OFF/missing table/missing language/unknown code must fail closed to the legacy string, not break Checkout.
The 094 admin-edited full label for post:standard or post:express should be the displayed label when present. Do not concatenate the old service and option label a second time. Honor actual language with a documented fallback; never invent a language or alter the default market. Keep existing API response shape unless a separately approved contract requires a change.
Do not edit one-level method identity, activation, translations, historical order snapshots, rate or packaging policy, payment or settlement records. Do not enable rate or operational-shadow gates, create a new payment path, or change Checkout calculation.

MISSION B — FOCUSED PROOF

Test BOTH gate OFF (byte-identical legacy customer label, method set and amounts) and gate ON with a task-owned one-level display name (customer label only changes). Cover option-backed and optionless, unknown method, missing translation, disabled method, and requested language. Prove a legacy inactive method remains ineligible even if one-level catalog is active. Prove no customer price/methodCode mutation.
Use existing unit/integration fixtures; only an isolated disposable DB if absolutely needed and demonstrably within the timebox. NO shared Development DML, no merchant label edit on tooba_alpha, no shared :5088/:3000 restart, no live order/checkout write. Narrow essential tests + module guard; no broad loops or known unrelated failing suites.
If the exact Order cross-module Contracts-only dependency cannot be wired safely without an architecture violation, produce a concrete read-side adapter design and report PARTIAL; NEVER add a direct Infrastructure, DbContext or endpoint dependency across modules.

MISSION C — EVIDENCE AND HANDOFF

docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-100.md plus docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-100/** must show actual before/after label examples, method-set+price invariants, gate OFF/on results, tests, changed files, SHA, true elapsed duration and unproven items.
Commit/push ONLY task-owned files. Stop for architect. No automatic next task; no evidence of success should be fabricated.

ALLOWED PATHS

src/backend/Modules/Fulfillment/** (module-owned read contract, implementation, tests and necessary module DI)
src/backend/Modules/Order/** (ONLY existing storefront shipping label projection and corresponding narrowly scoped tests)
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-100.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-100/**
Existing architecture guard files ONLY if a genuinely additive dependency requires precise manifest update; record each exception.
No frontend, Host, migration, schema, configuration or financial-module edits.

FORBIDDEN

Storefront eligibility/rate/amount changes; client methodCode translation; introducing a second provider/option level; changing paid order snapshots.
Any shared DB write or real activation; ShippingRateAuthority or ShippingOperationalShadow enablement; Host/Next restart; production/staging contact.
Cross-module direct EF/Infrastructure use, business logic in Host, Results.Json, authorization bypass.
Timebox overrun or broad speculative refactor.

ACCEPTANCE
PASS only when label-only gate-ON projection is proven with gate-OFF legacy parity and unchanged method eligibility, IDs, pricing, orders, plus focused tests and architecture boundaries. PARTIAL for proven bounded progress without safe end-to-end seam. BLOCKED when authority/consumer semantics cannot safely be preserved. STOP_FOR_ARCHITECT in all cases.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Program, Mode, Bridge-Task-Row, Status PASS/PARTIAL/BLOCKED, Stop-State STOP_FOR_ARCHITECT, Expected-Base-SHA, Base-SHA, Code-SHA, Evidence-SHA, automaticNextImplementationTask NONE, exact changed paths, actual tests/results, gate OFF/on label parity, method/price invariants, scope blockers, measured elapsed and rollback.
END_TOOBA_TASK
