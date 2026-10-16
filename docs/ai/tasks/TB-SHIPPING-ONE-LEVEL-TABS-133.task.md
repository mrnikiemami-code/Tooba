PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-133
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-132
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_GROUPED_DELIVERY_DATE_REPROJECTION_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 4da88089ab10430d3f8360722c573fd49322c3e7
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD; implementation by minute 30, only focused proof and evidence afterwards. No loops, broad suites, repeated builds or unbounded browser retries.

ARCHITECT RULING
TB-132 proved the grouped multi-seller storefront renders correctly but its deliveryDates remain empty after choosing methods because the grouped handler changes only local state and does not re-project with methodCode. Therefore the Continue button stays disabled. Fix ONLY the root cause, preserving the existing Shipping visual system and the canonical Order shipping projection authority. Do not start unrelated per-group pricing, fulfillment-origin, tax or Settlement development. Group choices are authoritative; do not invent dates in JS.

PREFLIGHT
Claim one Pending bridge task, Working heartbeat; read AGENTS.md and task protocol. Preserve receipt UNSTAGED. Fetch origin/main and check SHA/dirty files; non-overlapping docs-only drift permissible, any source overlap => RECOVERY_CONFLICT / STOP. No reset/stash/rebase/force push. Do not touch pre-existing dirty admin screens or evidence.

MISSION A — NARROW SOURCE REPAIR

Inspect src/frontend/app/storefront/storefront-shipping.tsx grouped onSelect handler, refreshProjection, delivery date/time controls, isValid, and storefront-shipping-api.ts inputs. Compare existing working single-method path.
For grouped carts, when selected group method(s) change, use the EXISTING authorized POST shipping/projection path to obtain a server-derived deliveryDates/minimumDeliveryDate rather than leaving projection.methodCode empty. Choose a date horizon that is valid for ALL selected group methods, not only the last-clicked or fastest method. The server's existing single-method projections can be used in a bounded and deduplicated way if no group-aware projection exists: derive the intersection of server-returned allowed dates (and permitted time windows) for all distinct selected methods, or conservatively enforce the latest server-provided minimum with the existing supported dates. Never do client-side rate, promise or calendar arithmetic. If authoritative safe multi-group date selection cannot be established with current contracts, STOP BLOCKED_GROUP_DATE_AUTHORITY and state the smallest Order-owned contract enhancement; do not fake dates.
Preserve method choices across re-projections, avoid stale response races when switching methods, invalidate an already-chosen date only if no longer eligible, keep Continue disabled until all group selections + valid address/date/window exist. On Continue, submit ShippingGroups[] with exact cartLineIds and selected methods to the existing Order-owned selection endpoint exactly once; never shipping/commit or order creation on navigation.
Never alter CSS, themes, global selectors, storefront typography, layout, spacing, RTL, colors, breakpoints, group markup, or existing single-seller legacy behavior. Only a minimal state/API plumbing repair within the shipping surface. No new business authority in frontend.

MISSION B — FAST ESSENTIAL PROOF
Focused frontend tests only: multi-seller two different method choices trigger server-derived dates; chosen date valid for both methods; stale request cannot overwrite newest selections; Continue enabled when completed and persists selection before navigation; legacy single-method untouched. Run only changed-file-targeted tests and typecheck; maximum ONE targeted rerun for a concrete failure. Prefer reusing TB-132 isolated fixture recipe ONLY if source fix completes by minute 30; one bounded browser check of the actual two-seller cart with different methods, valid date and one Continue to payment. Verify persisted draft via authorized Order read and cart remains Active. Payment tax fixture/readback is a separate risk; report exact subsequent blocker, do not expand scope. If runtime cannot fit, report source PASS with runtime NOT PROVEN (PARTIAL overall).

MISSION C — NON-INTERFERENCE / DELIVERY
Shared :5088/:3000, tooba_alpha, configs, flags and existing containers READ-ONLY; scratch only if authorized by this task and safely disposable; no broad process stops, no manual SQL, no secrets in evidence. Write concise docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-133.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-133/ evidence, commit only bounded changed files and push normal origin/main, verify HEAD==origin/main. Report measured minutes, SHA/drift, test counts, exact observed date semantics, unchanged UI, rollback and first remaining blocker. STOP_FOR_ARCHITECT, no automatic next task.

ACCEPTANCE
PASS only if a genuine grouped shipping selection can acquire server-authoritative dates valid across distinct methods, Continue becomes enabled for a complete valid form, and the saved selection survives the handoff without premature checkout/order creation, with minimum focused proof. Never claim Checkout/payment/production readiness without corresponding runtime evidence.
END_TOOBA_TASK
