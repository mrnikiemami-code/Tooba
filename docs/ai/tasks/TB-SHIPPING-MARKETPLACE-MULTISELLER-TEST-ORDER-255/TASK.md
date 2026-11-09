PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MARKETPLACE-MULTISELLER-TEST-ORDER-255
Parent-Task: TB-SHIPPING-ORDER-OPERATIONS-PROFESSIONAL-UI-254-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — create the requested reviewable Marketplace test order
Mode: CREATE_REAL_MULTISELLER_ORDER_WITH_SPLIT_METHODS_AND_MARKETPLACE_RESPONSIBILITY
Expected-Base-SHA: ab4ebd3ee1b28d20250291187695020ae3693729
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

1. Request and naming correction

This is the pipeline-formatted replacement for the incorrectly named non-envelope draft TB-MARKETPLACE-MULTISELLER-TEST-ORDER-255. It is the SAME work, not a second task. Do not execute both.

The user needs an actual new order visible in the application for their own testing. Do not turn this request into a feature-development project or another Store-owned substitute order.

Read AGENTS.md and the three current recovery TOPs first. Expected base is the last independently reviewed 254-R1 evidence commit. Verify current HEAD/origin/main and any intervening task/result before proceeding; disclose legitimate descendants and actual Base-SHA. Do not reset, overwrite concurrent work or pretend stale parity.

2. Exact order scenario
One new order containing at least TWO distinct real sellers.
Seller A supplies THREE DIFFERENT products: products A1 and A2 use the same accepted shipping method in one shipping group; A3 uses a different shipping method in another group.
Seller B supplies at least one product.
EVERY accepted group has actual Marketplace shipping responsibility, not Store relabelled as Marketplace.
Use existing configured methods. Report exact method/service/option identities; differing titles alone do not prove two methods.
Select valid dates and time windows. Where otherwise eligible, choose the same method/date/hour across sellers to make grouping inspectable.
Cross-seller operational merging follows existing Marketplace-only rules with PROVEN physical origin and exact compatible commitment. Owner identity, tenant, carrier city code and recipient address are not origin evidence. Do not force a merge by inventing origin facts.
3. Real environment and exact authority boundary

Original source: D:\Users\User\source\repos\SarvNewVer
Canonical FE: http://127.0.0.1:3000
Canonical Host: http://127.0.0.1:5088
Current installation: SingleStore / store-alpha / tooba_alpha, according to the latest runtime report; verify rather than assume.

The prior report says SingleStore resolves Platform responsibility to Store and current captured selection writes no proven origin context. First inspect the existing Marketplace configuration/path. Do not simply repeat a failed SingleStore checkout.

Authorization is limited to creating the requested NEW test order through existing application commands/APIs in the current environment if a valid Marketplace context is already available.

NOT authorized:

changing this installation's edition, tenant resolution or connection mapping;
creating a parallel database, replacement runtime or alternate final port;
adding Marketplace semantics to SingleStore;
relabelling or rewriting existing accepted Store evidence;
direct business SQL, manual DDL, reset/reseed or fabricated origin/payment facts.

If a valid Marketplace context is unavailable without such a change, STOP before creating another nonconforming order. Return BLOCKED with the one concrete environment decision required, the affected configuration and expected access/data impact. Do not ask several redundant questions, propose a new feature, or claim the scenario is complete. This limitation is explicit; no workaround may silently cross it.

4. Execution if the existing environment supports the scenario
Use sanctioned existing admin/buyer access and normal storefront/application commands, preserving user browser tabs and profiles.
Use a fresh cart/order for this scenario. Do not modify the previous order 01a1bd30-… or other existing orders/carts.
Create only the minimum new test data required through supported application paths; disclose new identifiers and any new supporting records.
Complete checkout to the point supported by the existing official test/manual payment path. No real charge, invented payment success, payout or settlement.
If payment approval is required but no sanctioned test path is available, retain and report the actual created state; do not report fulfillment-ready success.
Do not pack, create parcels, dispatch, deliver or cancel the new order: leave those operations for the user's review.
No live Tipax call, credentials request or provider acceptance claim.
No UI redesign, grouping-rule changes, backend feature work or unrelated guard cleanup.
5. Bounded proof and review handoff

Use the stored order detail response and an authorized rendered order page to verify:

distinct seller identities;
three distinct Seller A products with the exact 2+1 shipping-method split;
actual accepted owner kind Marketplace on all groups;
accepted method identities, dates/windows, fees and operational grouping consistent with physical origin evidence;
no unsupported substitution or rewrite of prior accepted history.

No screenshots, full-suite loop or disposable-database proof is needed for this order-creation task. A shell 200 alone is not an authorized page proof.

Success requires the requested stored order plus its usable review URL and honest payment/readiness state. If only part is achieved, report PARTIAL; an environment barrier before creation is BLOCKED, never PASS.

Preserve DataProtection and local CapturedPreview ON (Enabled/SelectionEnabled/CheckoutEnabled; AvailableDateCount=7), checked-in defaults OFF. Do not rebuild/restart merely to create data.

6. Recovery and complete worker result

Persist this exact TASK.md and RESULT.md/RECEIPTS.md in:
docs/ai/tasks/TB-SHIPPING-MARKETPLACE-MULTISELLER-TEST-ORDER-255/
docs/architecture/evidence/TB-SHIPPING-MARKETPLACE-MULTISELLER-TEST-ORDER-255/

Update all three recovery TOPs with the actual outcome and review locator or blocker:
AGENTS.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md

Preserve prior technical vs visual acceptance boundaries, Tipax deferred, TB185 paused/default-OFF and retained unrelated issues. Record this naming correction so only the pipeline ID is used.

Commit/push task/evidence/recovery normally; no force/amend/reset/stash/rebase. Verify actual local/origin parity; disclose unrelated work rather than commit it.

Return PIPELINE-PROTOCOL: BRIDGE-WAKE-V1 / BEGIN_TOOBA_WORKER_RESULT envelope containing:
Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Repository, Branch,
Status, Decision, actual Base/Code/Evidence SHA (Code-SHA NONE if no code),
runtime edition/tenant/database, new order/cart IDs, review URL and actual order/payment state.

Include a concise seller/product/accepted-group/method/day/hour/responsibility table. If blocked, state the exact single decision required instead of a false review URL.

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK