PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-129
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-128
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_FULFILLMENT_GROUP_SCOPED_SHIPMENT_EXECUTION_M3
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 8261804c6e5976440099e5c13e3f2bc59daf9c5e
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD. Stop implementation at minute 32; focused test/commit/report by minute 45. No broad suites, polling loops or repeated retries. If over budget, return PARTIAL.

ARCHITECT RULING
TB-126 (M1 draft), TB-127 (M2 accepted Order shipping groups) and TB-128 (Payment customer shipping-group allocations) are accepted as bounded code changes, NOT end-to-end certified. Now complete M3: Fulfillment must execute each persisted OrderShippingGroup independently while keeping one FulfillmentUnit per SellerOrder. Preserve existing storefront appearance in its entirety; NO frontend/CSS work. Source of truth:

docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md
docs/planning/shipping/SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-125.md
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-127.md
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-128.md

PREFLIGHT — FAIL CLOSED
Claim one Pending Bridge task on tooba-main; keep task receipt UNSTAGED. Read AGENTS.md and protocol. Fetch origin/main; verify Expected-Base-SHA ancestor and inspect drift. Any overlapping source drift or unowned dirty files -> RECOVERY_CONFLICT/STOP. Preserve existing dirty admin frontend and unrelated evidence. No reset/stash/rebase/force push.
Trace the exact existing Order->Fulfillment Contracts bridge, accepted OrderShippingGroup and OrderShippingGroupLine membership, SellerOrder, FulfillmentUnit and Shipment persistence, idempotency and existing shipment creation paths. Do not redesign other module boundaries.

MISSION A — GROUP-SCOPED EXECUTION

Keep the unique FulfillmentUnit-per-SellerOrder invariant and existing SellerOrder ownership. Add the smallest Fulfillment-owned group identity and item membership seam required to create/associate one shipment per authorized OrderShippingGroup within a seller's unit. Multiple groups for the SAME seller must work; groups from different sellers must never merge. Use stable ShippingGroupId from accepted Order, not labels or browser values.
Extend Order->Fulfillment through Contracts-only with group id, canonical method/service/option identity, group order-line membership (and quantity if necessary), seller/order references and scheduling snapshot; preserve historical single-method flow. No direct cross-module DbContext, new Host business rules, synthetic origin or fabricated carrier allocation.
Revalidate that each group belongs to the specified seller order and that its lines are neither omitted nor duplicated. Map order-line identity to fulfillment membership through authoritative existing seams. Shipment state/tracking/cancel/dispatch must identify its actual shipping group; do not reassign unrelated groups. Persist any new Fulfillment field with a Fulfillment-owned additive migration and correct indexes, keeping older shipments readable. Respect current origin limitation: NULL/unknown, never invented.
Retry/replay of a paid-order event must NOT create duplicate shipments or duplicate group rows. Do not create any shipment on merely opening shipping/payment. Preserve existing authorization, tenant boundaries, status transitions, and feature gates. If safe shipment creation requires a new owner-correct event/contract outside timebox, implement only the bounded atomic layer and report PARTIAL rather than silently widening the scope.

MISSION B — NECESSARY PROOFS
Focused cases only: (a) one seller with two methods -> two separate group-linked shipments in the same unit; (b) two sellers with same method -> isolation; (c) exact member-line coverage/wrong seller refusal; (d) replay idempotency; (e) legacy single-group order unaffected; (f) independent group transition if supported by current APIs. Test only the directly changed projects/guards, once plus at most one targeted rerun after a concrete fix. Build changed projects. If disposable DB migration/runtime cannot fit, disclose unproven runtime DB behavior. Never touch shared :5088/:3000/tooba_alpha or run broad integration environments.

MISSION C — MINIMAL EVIDENCE
Commit ONLY bounded Contracts/Order bridge/Fulfillment source, migration, focused tests and concise docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-129.md + docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-129/. Push normally, verify HEAD == origin/main. Give actual elapsed minutes, exact files, tests, fail-closed guards, remaining blockers, rollback, Code-SHA/Evidence-SHA, and STOP_FOR_ARCHITECT. No automatic follow-up task.

FORBIDDEN
No frontend/UI/CSS/theme edits. No Settlement M4 allocation policy invention, no fake seller/platform shares, no payment/checkout redesign. No shared DB/config/container/process mutation, no non-owned SQL, no broad test/retry loops, no weakening one-unit-per-seller or tenant isolation.

ACCEPTANCE
PASS only if supported M3 path durably associates every accepted shipping group with correct seller and exact item membership, supports independent shipment execution, preserves retries/legacy cases, and passes necessary focused tests/build. Otherwise PARTIAL/BLOCKED with first exact limitation. Commercial readiness remains unproven until Settlement, UI, and isolated end-to-end validation.
END_TOOBA_TASK
