PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-137
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-136
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_GROUPED_PAYMENT_SUMMARY_DISPLAY_CLOSURE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 5c32619bec45fe1adafc50c000c820be696e8d35
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD; prioritize source change within first 25 minutes and targeted proof/commit by deadline. STOP PARTIAL on time; no loop or expansive investigation.

ARCHITECT ASSESSMENT
TB-136 provides genuine isolated browser + DB evidence: ONE seller, TWO real products/cart lines, TWO distinct methods, TWO persisted draft groups, fee sum 380000 IRR, payment-page payable 3977000 IRR, valid cold reload, RTL desktop/mobile. Its code SHA 504aeb1166038946ca3b9e314afa46bedcb29f28 and evidence SHA fd340f600916485bbe25e62417f742c08bfdfe7e are verified in GitHub. The remaining customer-visible defect is a blank value after «روش ارسال:» on multi-group /fa/payment, caused by the intentionally blank legacy scalar method field. This must NOT be repaired by pretending there is a single method. Full payment/capture and final commercial Settlement allocation remain OPEN and must not be claimed closed.

MANDATORY AUTHORITIES

docs/planning/shipping/SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md
docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-136.md
Existing frontend payment summary, Order-owned StorefrontCheckoutPage.ShippingGroups and Cart/Order membership contracts.

PREFLIGHT
Claim precisely one bridge task; verify origin/main and source drift. If current source overlap, RECOVERY_CONFLICT and STOP; documentary-only drift is acceptable after source comparison. Preserve two pre-existing dirty admin screens and unrelated evidence. Do not reset/stash/rebase/force-push. Task receipt per bridge contract.

MISSION A — REPAIR EXACT CUSTOMER DISPLAY DEFECT

Identify the existing payment /fa/payment rendering of «روش ارسال:» and the already-present per-group shipping breakdown. For genuine multi-group drafts, render a meaningful, localized plural/grouped summary of the ACTUAL authoritative methods from checkout.shippingGroups. Do not render an empty value; do not invent an aggregate shipping method, rate, origin, tax, commercial share, ETA or new method identity. Prefer a concise heading such as «روش‌های ارسال» and the existing per-group method + amount rows already rendered; ensure no duplicate fees or duplicated method labels. If groups are empty, preserve the exact legacy scalar display.
Reuse existing UI markup/classes/design tokens, component hierarchy, typography, cards, spacing, RTL and responsive styles; NO changes to CSS, theme, global classes, token definitions or unrelated routes. Keep the grand total based on server authority; no newly computed charges. Smallest targeted change in storefront-checkout.tsx or its existing view mapping only.
Maintain correct read-only payment navigation: no submit/commit/reservation/cart clear/capture on open or reload. Keep multi-seller and same-seller grouped cases truthful. No change to Order/Payment/Settlement financial policy, no data migration.

MISSION B — ESSENTIAL PROOF AND POSSIBLE ONE BOUNDED BROWSER CHECK
Targeted storefront checkout tests for: multi-group heading not blank, two real different methods/charges remain visible, legacy scalar unchanged, payable/shipping displayed once, RTL/class usage unchanged. Run frontend typecheck + only closest impacted tests; max ONE rerun for a specific fix. If source implementation done with time remaining, reuse the TB-136 sanctioned isolated fixture/procedure (Development-only companion offer) for one browser validation of the actual /fa/payment grouped summary and inspect desktop/mobile within time; otherwise STOP and report runtime unproven. Do not restart shared :5088/:3000, touch tooba_alpha, or use any shared DB. No extensive acceptance loop or new fixture engineering.

MISSION C — HANDOFF/REPORT
Commit implementation, minimal targeted evidence and docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-137.md. Normal push and verify HEAD==origin/main. Report SHA, measured duration, exact UI before/after, proof counts, any remaining blocker. Do not claim final Payment transaction, commercial split, production readiness or migration certification. Next priority after this UI closure: assess legally safe isolated checkout-to-order/payment completion and unresolved PendingAllocation policy by a separately authorized task, with clear commercial-policy decision rather than fabricated allocation.

ACCEPTANCE
PASS only if the customer no longer sees an empty «روش ارسال» on real multi-group checkout projection, retains correct per-group methods/fees, legacy readback stays intact, and existing visual identity is unchanged. Otherwise PARTIAL/BLOCKED and STOP_FOR_ARCHITECT.
END_TOOBA_TASK
