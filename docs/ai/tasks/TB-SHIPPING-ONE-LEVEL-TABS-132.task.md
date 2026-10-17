PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-132
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-131
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_ISOLATED_MULTISELLER_SHIPPING_CHECKOUT_BROWSER_ACCEPTANCE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 8e3aa1bf2a9c8ce74325843572d13fdb8a29fc69
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD. By minute 30 stop setup/investigation, reserve time for cleanup and concise evidence. One Host launch, one frontend launch, one browser flow. No retries/relaunch loops. If prerequisite cannot be met, PARTIAL/BLOCKED with precise cause.

ARCHITECT RULING
TB-131 implemented customer-facing grouped shipping and payment UI (typecheck and 76 storefront tests passed), but no real browser/DB proof was obtained. This task is OBSERVATION-ONLY: prove or falsify real multi-seller grouping, durable draft readback, payment summary and faithful reuse of storefront appearance. Do not mistake worker PASS for release acceptance. Keep commercial allocation PendingAllocation untouched. Read docs/planning/shipping/SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md, docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md and docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-131.md.

PREFLIGHT / NON-INTERFERENCE
Claim one Pending Bridge row and heartbeat; read AGENTS.md and pipeline instructions. Leave task receipt UNSTAGED. Fetch origin/main and check drift. Source overlap or ambiguous drift -> RECOVERY_CONFLICT STOP. Preserve existing dirty admin frontend files and prior evidence. No reset/stash/rebase/force push. Shared Host :5088, Next :3000, tooba_alpha and unrelated containers READ-ONLY; no shared writes or service restarts.

MISSION A — ONE ISOLATED LIVE PROOF
Use proven TB-122/123 isolated recipe: disposable no-volume postgres and unique loopback ports, isolated Host with safe validated scratch tenant/references and existing Development-only one-level catalog gate overlay, isolated frontend using TOOBA_HOST_ORIGIN to the scratch Host. Enforce offline fail-closed configuration; never reference shared DB/tenant, leak secrets, disable auth or run reset-and-seed. Unique PID recording and watchdog <=180s for Host and <=600s for frontend. Launch each ONCE. Pre-check /health, /__platform-commerce and frontend binding discriminator before any mutation.
Use only sanctioned module Development fixtures/API: workspace-live-shirt with two genuine seller offers, whose standard-category tax rule is expected (but not yet runtime-proven). Confirm through authorized API/readonly scratch evidence that the product, both offers and applicable tax rule really exist. Create the real guest cart via customer storefront actions, with lines from BOTH sellers. If any prerequisite fails, stop and report blocker; do not fabricate offers/identities, patch Tax, or manually insert SQL.

MISSION B — FOCUSED CUSTOMER ACCEPTANCE
In one real browser flow, capture the existing storefront visual baseline (representative legacy single-seller shipping and payment design if reachable), then inspect grouped /fa/shipping and /fa/payment on desktop and one mobile viewport. Verify group labels, seller identity, exact REAL line membership, chosen per-group methods (prefer distinct method codes), localized catalog labels, each authoritative shipping fee and sum, and original hero/step semantics. Confirm the existing styling (CSS classes/tokens/cards/spacing/RTL/responsiveness) is preserved; no screenshot loops or subjective invented parity. Record minimal DOM/screenshot evidence only.
Perform ONE explicit Continue click, not commit/payment submit. Assert PUT shipping/selection occurs before routing to Payment, saved draft shippingGroups match the browser choices after page refresh or back navigation, cart stays Active, and payment draft contains the correct groups/fees/totals with tax-applicable fixture. Cross-check API group IDs and line IDs, no arbitrary cart-wide method projected over multi-group order. Observe GET/POST only as sanctioned; do not create paid orders, capture money, invoke shipping commit or settlement, or mutate the shared environment.
If missing per-seller eligibility or current UI grouping blocks accurate multiple methods, record exact endpoint/state and STOP rather than forcing an acceptance. Do not turn source-only proofs into browser claims. Grouping by seller alone must NOT be represented as proof of one-seller/two-method customer UI support: record this separately as an outstanding acceptance case.

MISSION C — CLEANUP / RESULT
No source/frontend/CSS/config/migration edits. If an actual defect is found, return PARTIAL with a narrow proposed fix; do not repair inside this observation task. Stop only recorded task-owned PIDs after path validation; remove only task-owned scratch container/temp dirs/secrets; prove ports released and shared pre/post parity. Save concise docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-132.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-132/* with evidence, bounded timestamps, provenance, redacted screenshots, exact blocker and non-claims. Commit docs/evidence only, push normally and verify HEAD==origin/main. No automatic next implementation.

ACCEPTANCE
PASS only for actually observed isolated browser/API equivalence across two sellers, successful payment-draft display, preserved selection and financial totals with desktop/mobile appearance unchanged. Explicitly distinguish any untested one-seller/multiple-method use case, unverified group DB migration, and pending commercial allocation. Otherwise PARTIAL/BLOCKED. HARD STOP at 45 min.

RESULT CONTRACT
Task-ID/Parent/Channel/Worker/Bridge row, Status, Stop-State, Expected and Actual Base SHA, Code-SHA NONE, Evidence-SHA, elapsed minutes, launch counts, readiness, exact API/DOM grouped results, visual non-regression, payment-draft/tax outcome, cart/order mutation guards, shared parity, cleanup, deviations, first blocker and rollback.
END_TOOBA_TASK
