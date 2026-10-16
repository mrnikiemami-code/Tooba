PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-INDEPENDENT-GROUP-FRONTEND-150
Parent-Task: TB-SHIPPING-INDEPENDENT-GROUP-BACKEND-149
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 0fde15c5e5d7d223ae4a8825ccb49f8fd17ccff3
Program: TOOBA-SHIPPING-INDEPENDENT-SELLER-COMMITMENTS
Mode: BOUNDED_REAL_STOREFRONT_GROUP_CARDS_AND_SCHEDULE
Status-Target: PASS_OR_PARTIAL_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD — reserve final 10 minutes for tests, evidence, push and report.

USER DIRECTION
Continue autonomously, do not ask for approval between sequential tasks until shipping flow becomes USABLE. Only one worker task active at once; STOP_FOR_ARCHITECT then architect reviews result and issues next task. User has APPROVED the TB-145 per-shipment wireframe: no new wireframe or design-only task.

AUTHORITATIVE CONTRACTS — READ FIRST

docs/architecture/shipping/SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT.md
docs/planning/shipping/SHIPPING-GROUP-SCHEDULE-WIREFRAME-145.md
docs/planning/shipping/SHIPPING-GROUP-SCHEDULE-IMPLEMENTATION-145.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md
docs/planning/MUST-FIX-BEFORE-RELEASE.md
Backend implementation TB-149 at 90e2dc166efac312af7a73f4485f39d6a702e9e3; verify current projection/request signatures from source, NOT assumptions.

OBJECTIVE — SHIP REAL CUSTOMER UI
Refactor /fa/shipping to render one independently schedulable visible card per REAL ShippingGroupId, with each group’s seller, member products/quantities, canonical shipping service, fee, own earliest authorized date, eligible dates, eligible time windows and selected date/slot. Two sellers selecting identical tipax:standard with different preparation (+2 / +3) MUST display TWO independent cards, never one method-only card. If same seller has multiple methods, also separate groups. No page-wide shared date or time picker; no date/window intersection across groups. Reuse existing storefront layout, styling, RTL, CSS and payment summary; no redesign.

MANDATORY IMPLEMENTATION

Read TB-149 additive StorefrontShippingProjection.ShippingGroupHorizons and ShippingGroupSelection.SelectedDeliveryDate/SelectedDeliveryTimeWindow. Use server authoritative group horizon and stable ShippingGroupId, NOT methodCode dedup or client-calculated promises. Connect group identity to draft line membership without guessing; if server DTO lacks necessary trusted group identities before first save, implement minimal additive owner-correct read projection or STOP with exact gap. Avoid using cart sellerId/method as permanent surrogate ID.
In src/frontend/app/storefront/storefront-shipping.tsx replace global deliveryDate/deliveryTime with per-group keyed state; put existing date chips + slot picker INSIDE each shipping group card. Preserve selected per-group method, schedule and fee on roundtrip/refresh/back where valid. Group-specific invalid date/slot should not erase unrelated group choices. Show loading/empty/stale/validation states scoped per group.
Replace old deriveGroupDateHorizon/resolveShippingDateHorizon intersection and distinctSelectedMethodCodes fetch semantics in storefront-shipping-group-horizon.ts and use-storefront-group-shipping-horizon.ts with per-group authoritative eligibility; no fake date, invented carrier slot or shared MAX. Ensure one-group legacy shorthand and original single-group appearance still work.
Continue/payment is enabled ONLY when EVERY active group has valid method, date and time window. Send Groups[] with per-group dates and slots; no global selected date in multi-group write. Keep all exact line IDs/quantities, server-fee totals and group identity. Never mutate cart/order/payment prematurely during navigation.
Preserve tenant/seller isolation, existing auth, pricing contracts, shipping calendar limitations, unchanged Fulfillment bridge. Keep UI localized Persian RTL, responsive and consistent with existing storefront. No modification to Admin/Settlement/Payout, no new CSS system.
Respect code-size guard: split into small feature components/hooks as necessary instead of growing already-oversized storefront-shipping.tsx; do not rebaseline guard to hide size violations.

VERIFICATION (FOCUSED)

Test two sellers, SAME canonical service code, DIFFERENT per-seller earliest dates: distinct cards, independent day/time selections, independent persisted payload and validation; change group A leaves B untouched. Test same seller/different methods, one-group backwards compatibility, refresh/back restoration, Continue disabled when group B incomplete, no date intersection. Use authorized deterministic fixture/hook only; no invented real carrier commitment.
Frontend typecheck plus focused shipping tests and one READ-ONLY browser smoke if existing running shared :3000 is available. User-visible proof must not be fake mockup; if an isolated writable scratch environment is not available, mark HTTP write/read roundtrip UNVERIFIED and retain PARTIAL.
Record concise evidence (tests, rendered DOM/cards, backend contract exact fields) in docs/architecture/evidence/TB-SHIPPING-INDEPENDENT-GROUP-FRONTEND-150/ without secrets or oversized stdout.
Preserve existing unrelated dirty files, stashes and running processes. No .next purge, shared :5088/:3000 restart, store-alpha/tooba_alpha write, schema migration or financial policy changes.

PASS = REAL, correct independent per-group UI and payload + focused tests/typecheck; if complete runtime write/read is required but prohibited report PARTIAL with precise blocker. No PASS for a mockup, generic seller-only cards, common date selector or client-derived promises.
Commit/push to origin/main fast-forward; verify HEAD == origin/main; report implementation/evidence SHA, changed paths, test counts, limitations and next single bounded step. STOP_FOR_ARCHITECT.
END_TOOBA_TASK