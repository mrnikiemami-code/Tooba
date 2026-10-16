PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-134
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-133
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_PERSISTED_SHIPPING_DRAFT_RELOAD_RECOVERY
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 2f9f39cf6aad8c2fb650fb48901881fe8f579c9a
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD; source repair by minute 30, essential proof + push before limit. Stop PARTIAL if blocked; no loops, no broad test suites.

ARCHITECT DECISION
Accept TB-133 source repair and isolated proof: two seller groups, independent methods, server-derived shared dates, one authorized selection PUT, Payment page reached with shipping 380,000 IRR and total 4,347,600 IRR, cart unchanged. NOT accepted: cold reload of /fa/shipping discards visible saved address/methods/date despite persisted draft, and one seller/two methods remains unproven. This task addresses ONLY reliable persisted draft read-back and revalidation. Payment handoff must continue working. Commercial allocation policy remains blocked, no invented funding.
Mandatory documents: docs/planning/shipping/SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md, docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md, docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-133.md.

PREFLIGHT
Single Bridge claim, honor AGENTS.md, preserve receipt UNSTAGED. Fetch origin/main, confirm Expected-Base-SHA ancestry; source overlap => STOP RECOVERY_CONFLICT. No reset/stash/rebase/force push. Preserve dirty two admin screens and unrelated prior evidence. Trace exact Order-owned StorefrontShippingService projection/draft reading, frontend projection request lifecycle and existing destination identity mechanism. No redesign, no speculative new storage.

MISSION A — ROOT-CAUSE FIX, OWNER-CORRECT

Diagnose why POST /v1/storefront/shipping/projection without methodCode returns draft:null after one successful shipping/selection. Distinguish (a) Order draft retrieval, (b) shipping destination/revalidation context, (c) frontend hydration. Cite actual code paths; do not rely on conjecture.
Make the smallest additive Order-owned read/revalidation contract or projection fix so an authorized guest cart with a persisted shipping draft can retrieve a trustworthy draft on initial shipping-page reload WITHOUT a selected methodCode. Recover authoritative address/destination, stable seller-to-line shipping groups, selected method per group, delivery date/time, and server price snapshots. Never expose someone else's draft or trust a client-supplied amount. If address is owned elsewhere, use its existing trusted owner seam; do not duplicate address authority or create public draft leaks.
On frontend, hydrate selection/address/date/time only from validated persisted state; ensure the existing per-method server date-horizon re-projections revalidate the recovered date and do not overwrite the saved groups. Invalid or stale selection must fail closed with a useful existing-style message and no automatic commit. Preserve legacy single-method behavior.
Repeated reload must not issue /shipping/selection, /shipping/commit, checkout submit, capture, or cart clear; back navigation from /fa/payment -> /fa/shipping recovers the same selected groups. No new writes on GET/projection. Prevent async stale-response races.
No new route, no CSS/theme/tokens/layout/RTL/typography changes. Prefer small cohesive file changes; avoid enlarging oversized storefront-shipping.tsx with complex logic. Keep existing visual design byte-identical as much as possible.

MISSION B — BOUNDED REAL PROOF
Focused tests for persisted draft read with methodCode absent, multi-seller method identity and address hydration, persisted date/time revalidation, legacy single-group compatibility, invalid draft fail-closed, and no side effects. Run changed-project focused tests and frontend typecheck only; at most one targeted rerun after concrete failure.
If implementation done by minute 30, use ONE isolated disposable scratch target and ONE browser journey to create two-seller real cart, select distinct methods and valid slot, Continue once to payment, go back then cold reload /fa/shipping. Compare the restored two group methods, address, date, time and fees with authorized API and Payment totals, and ensure cart remains Active. Prefer reusing safe TB-133 scratch scripts with new distinct ports/DB IDs. If runtime unavailable, report PARTIAL or unproven browser check rather than claim PASS for browser. No shared :5088/:3000, tooba_alpha or live DB mutation. Cleanup own PIDs/containers only.

DELIVERABLE / ACCEPTANCE
Commit owner-correct minimal fix + focused tests + concise docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-134.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-134 evidence. Normal push, verify SHA. PASS only if persisted draft is retrievable/revalidated after reload, grouped choice is preserved, access control remains authoritative, and no premature checkout write happens; explicitly separate source-tested and real-browser claims. Report time, exact source files, tests, code/evidence SHAs, drift, remaining blockers (single-seller/two-method UX, merchant allocation policy, migration proof) and STOP_FOR_ARCHITECT.
END_TOOBA_TASK
