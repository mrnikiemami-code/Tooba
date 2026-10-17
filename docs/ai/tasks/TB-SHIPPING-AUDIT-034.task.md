Task-ID: TB-SHIPPING-AUDIT-034
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ARCHITECTURE-PLANNING
Parent-Task: TB-P0-CHECKOUT-DEV-LIVE-CUTOVER-033
Mode: READ_ONLY_SOURCE_AUDIT_AND_REPORT_ONLY
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: d5f74da6cd820950dbea2c47d7e87929619d7f91
Repository: mrnikiemami-code/Tooba
Branch: main
Timebox: 30 minutes HARD
Automatic-Next-Implementation-Task: NONE

PREFLIGHT / REPOSITORY GUARDS

Verify local HEAD == origin/main == Expected-Base-SHA and record worktree status before editing. If drift exists, STOP with actual SHAs; do not rebase, reset, stash, force-push, discard or overwrite unrelated work. If another task is active, STOP. Read AGENTS.md, the established BRIDGE-WAKE-V1 task/result contract, and docs/architecture/SHIPPING-ARCHITECTURE.md (Draft v0.1). Existing Architecture guards remain binding.
This task is an AUDIT, not permission to create a Shipping module or refactor production. Read-only examine the current source and docs on main. No service restart, new environment, migration, DB write, seed, PSP/wallet transaction, deployed Host change or end-to-end paid order. Do not expose secrets.

OBJECTIVE — VERIFY CURRENT SHIPPING AUTHORITY BEFORE DESIGN
Establish what Tooba ALREADY implements, where each authority resides and precisely which changes the proposed Shipping architecture would require. Distinguish verified implementation facts from unimplemented proposals. Preserve previously certified module boundaries, current Cart Active until final Pay behavior, and Order/Fulfillment shipping commitments.

BOUNDED AUDIT SCOPE

Trace storefront-shipping.tsx -> POST /v1/storefront/shipping/projection -> endpoint/service -> Shipping catalog/options/config -> pricing, service eligibility, date windows and time windows. Verify the actual class names, paths, code lines, DB/config authority and effective activation filters; do not assume the supplied earlier analysis is complete or current.
Trace Catalog Product/SKU weight, dimensions and logistics category; Offer seller preparation/origin and who is authorized to choose shipping responsibility. Classify each item as exists/partial/missing/unknown.
Trace Checkout grouping of seller/order lines and shipping selections; Order snapshot and immutable monetary/delivery promises; treatment of different sellers, services, dates, zones and logistics compatibility. Check current single max-seller preparation behavior and if it postpones unrelated delivery groups.
Trace Fulfillment delivery groups vs physical Shipment/parcel ownership, seller- and Tooba-performed dispatch, consolidation rules and customer isolation; note UX dependencies without implementing layout.
Trace Payment's ShippingAmount / StoreShipping allocation, any carrier payable, collection by Tooba/seller/carrier, prepaid vs collect-on-delivery, subsidies and Settlement contracts. Never equate generic shipping allocation to a carrier liability without explicit proof.
Trace shipping service coverage: country/city/sub-city or actual geographic schema, address localization/translation, province constraints, enabled catalog options and effective rate source. Find existing cart amount/weight/count constraints, packaging fee, calendar rules, working days, windows/capacity and free-shipping policies at platform/seller/offer levels.
Identify duplicated behavior and authority collisions specifically between currently certified Fulfillment, Order and any Shipping capability. Propose boundary candidates only; do not move code or invent contracts in this task.

REQUIRED DELIVERABLE — ONE DOCUMENT ONLY
Create docs/planning/shipping/SHIPPING-CURRENT-STATE-AUDIT.md as the sole task-owned file (unless that exact file already exists; then STOP rather than overwrite unrelated work). Include:

Branch, audited HEAD and inspection date; methodology and any gaps.
A compact evidence matrix: capability, current owner, exact existing paths and line ranges, effective DB table/config/constant, EXISTING/PARTIAL/MISSING/UNVERIFIED and risks.
A verified source-to-sink trace: Catalog/Offer -> Cart/Shipping projection -> Checkout/Order -> Fulfillment -> Payment/Settlement; mark uncertain links explicitly.
Current paid/collect-on-delivery behavior and carrier liability evidence (or explicit absence in inspected boundaries).
The most important contradictions to the proposed docs/architecture/SHIPPING-ARCHITECTURE.md; do not edit that architecture draft or declare it certified.
A priority-ordered list of up to five confirmed architecture blockers and ONE recommended next bounded slice for Architect review only; no execution.
Concise evidence and stop report; avoid speculative rewrites and extensive tests.

HARD CONSTRAINTS

ZERO feature implementation, endpoint/DTO/business-model edits, tests that mutate persistent data, migrations, database changes, runtime intervention, UI/wiring, config changes, shipping module creation or code refactors.
No edits to docs/architecture/SHIPPING-ARCHITECTURE.md, AGENTS.md, Host, frontend, Order, Fulfillment, Payment, Settlement or any other module files.
No new tasks generated, no unapproved follow-on execution, no indefinite polling. The audit report is the only permitted repository write.
No heavy or unrelated test suites. Source inspection and lightweight read-only commands only.
Respect 30-minute HARD timebox: if insufficient, report verified partial evidence, blockers and STOP rather than overrun or invent PASS.

ACCEPTANCE / VERSION CONTROL

Verify each cited path and line range against audited HEAD; separate proven facts from recommendations. Confirm only the audit report changed and no ignored live state was modified.
Commit and push only the report to main if preflight passes and existing repo governance permits it. Record base SHA, report commit SHA, actual elapsed time and full changed-path list. If a push is blocked or HEAD drift occurs, STOP with honest BLOCKED state, no force push.
Do not certify target architecture, begin implementation, perform checkout/payment smoke, or assume permissions from prior task 033.

REQUIRED WORKER RESULT — EXACT BRIDGE-WAKE-V1
Return exactly BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT using the established complete Result contract; include Task-ID, Parent-Task, Pipeline-Protocol, Channel, WorkerId, AgentType, Program, Mode, status, Stop-State, Base-SHA, Code-SHA, Evidence-SHA where applicable, audited paths and report path, source-backed findings, changed paths, commands/checks, non-actions, remaining unknowns, elapsed minutes and actual push result. automaticNextImplementationTask=NONE. Final state STOP_FOR_ARCHITECT. Do not auto-run the next task or send any test/probe payload to Bridge /api/results.
