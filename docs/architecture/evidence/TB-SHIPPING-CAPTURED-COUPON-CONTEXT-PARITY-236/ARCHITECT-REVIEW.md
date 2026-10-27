# Architect review — TB236 — 2026-10-28

Reviewed source36c44a35079c931d3a26cd780a8d20ec29ff5486 / main6d820e9ae7566652d34c7c615860680b3b9b2f0d,
actual patches, complete coupon tests, facade and CheckoutDirectory.Reservations quote path.
Root did not execute the worker's Windows tests. Verdict: VALID-COUPON MECHANICS RETAINED WITH TWO
CONCRETE INTEGRATION BLOCKERS; proceed to practical frontend connection with those small fixes inline.
No separate generic audit or helper-only repair task. No release/PSP/Visual ACCEPT.

## What is real
Coupon intent now reaches actual shipping projection body -> query -> handler -> service -> planner ->217 command.
Selection uses that same quote/facts path, stores root SelectionCouponCode + per-group ConfirmedGoodsAmount inside
existing232 CAS/transaction. Final compares exact normalized intent and actual final quoted goods per group;
payment preview shares it. Two additive nullable draft scalars, no JSON/full snapshot/per-line shipping duplication.
Current Order line discounts/tax remain financial history. Preserve this compact design.
Nullable override now branches on object presence (explicit absence retained), per-line assigned type is required.
New wrapper genuinely invokes writer after outer draft read before core read with changed dates; prior235-R1
claim corrected. Worker8/8+5/5+173/173 are execution evidence, not root execution or actual network proof.
Valid coupon/partial applicability, changed intent/goods, original authorized replay and isolated DB mapping
mechanics may be retained within controlledContracts/realOrder scope.

## Two blockers to close IN the frontend connection task
1. StorefrontCapturedSelectionWriter.AssertGoodsEcho returns immediately when ExpectedGoodsAmount is omitted/
blank. This bypasses read->save confirmation precisely when coupon/promotion goods changed but row/snapshot
tokens stayed the same. Required task236 condition is not met by an OPTIONAL echo. Require exact nonnegative
canonical decimal WIRE TEXT for EVERY selected group (explicit0 lawful); no blank/omitted fallback. Parser
NumberStyles.Number also accepts grouped/signed forms: use repository exact-token validation, no invented value.
Mismatch refuses before draft mutation; actual server facts remain authority.
2. CheckoutDirectory.Reservations calls Promotion but never checks promotion.RejectionReasons; the existing
PromotionDecisionEngine can return COUPON_NOT_APPLICABLE with zero discount, and unrelated automatic promotions
may still apply while supplied coupon is unmatched. No code introduced by236 closes this. "Invalid/rejected coupon
is surfaced by existing policy" is therefore UNPROVEN and not supported by this inspected quote path.
Use authority's actual coupon application/rejection evidence for the WHOLE cart (partial applicability lawful),
not amount==0 or any single line rejection, and surface a stable localized owning refusal. No new discount
matching/allocation rule. Add narrow tests including provider exception/caller cancellation on that same path.
This is necessary for the practical coupon UI, not another entire backend certification wave.

## Delivery priority / authorization update
Latest user explicitly says connect faster; test data is not a concern. Task237 may now prepare normal local
nonproduction runtime/migrations/test data and CONNECT THE EXISTING UI. Do not ask again for routine local setup.
Production/payment with real money is not authorized. USER dedicated review window still contains UNSAVED WORK;
protect by implementing/running frontend in an isolated worktree/ports, not by preserving unconnected read-only UI.
Keep checked-in gatesOFF; enable coherent gates ONLY in named task-owned local runtime.231/232/236 pending shared
migrations may be applied to the verified nonproduction target via MigrationRunner after actual plan inspection.
No shared migration history rewrite/reset. A separate Marketplace fixture runtime may use a new task-owned DB.
No fake auth/session/prices, no direct business SQL seed, no mocked browser responses.
Required final BOTH editions/all business owners/coupons/real practical trial and Order-only history still stand.
Historical cross-owner label-map claim is stale:235 repaired map,235-R1 proved distinct owner labels; remaining
decoder strictness is separate. Record known230empty-key/inflight/etc without undoing accepted owner work.
No independent time compliance acceptance; report's approximate23:5xZ is not an exact timestamp.
