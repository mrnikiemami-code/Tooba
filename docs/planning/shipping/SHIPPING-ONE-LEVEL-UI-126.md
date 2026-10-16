# TB-SHIPPING-ONE-LEVEL-TABS-126 — Order-owned multi-group cart shipping draft (M1, implemented)

- Task `TB-SHIPPING-ONE-LEVEL-TABS-126` (Parent `TB-SHIPPING-ONE-LEVEL-TABS-125`, program `TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE`)
- Mode `BOUNDED_ORDER_OWNED_MULTI_SHIPPING_GROUP_DRAFT_IMPLEMENTATION`
- Base SHA `94aa51bb27336bfabea01ad6b566bd52c81b567b` (== origin/main at claim; no drift)
- Slice: **M1 only** — Order-owned persistent per-seller/method shipping groups on the cart shipping draft.
- Mandatory sources: [SHIPPING-METHOD-GROUPING-UI-CONTRACT.md](../../architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md), [SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md](SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md), TB-124/TB-125 evidence. None modified.

## What changed (additive only)

| Layer | Change |
|---|---|
| Domain | `CartShippingDraftGroup` (stable UUID, CartId, SellerPartyId, canonical `MethodCode` + separate `ServiceCode`/`OptionCode`, typed optional `OriginContext`, authoritative amount/currency, scheduling, `LeadDays`, `State`, `UpdatedAt`, whole-line `Lines`) and `CartShippingDraftGroupLine`; `CartShippingGroupFault` stable fault codes |
| Domain | `CartShippingGroupPlanner` — pure membership planner enforcing all invariants below |
| Domain | `CartShippingDraft` now OWNS `List<CartShippingDraftGroup> ShippingGroups` plus `ClearShippingGroups` / `AddShippingGroups` (replace semantics + legacy scalar projection) |
| Infrastructure | `OrderDbContext` mapping for `cart_shipping_groups` / `cart_shipping_group_lines` (house `List<T> { get; } = []` navigation pattern, matching the certified `SellerOrder.Lines` shape) |
| Infrastructure | Migration `20261016090000_AddCartShippingGroups` — additive tables, FKs to `cart_shipping_drafts(cart_id)` with cascade, `UNIQUE(cart_line_id)`, `UNIQUE(cart_id, seller_party_id, method_code)`, amount/currency checks |
| Infrastructure | `StorefrontShippingDraftStore.GetByCartIdAsync` now loads groups + lines so replacement can be tracked and flushed |
| Application | Additive `Groups[]`: `StorefrontShippingGroupSelection` (request) and `StorefrontShippingGroupView` (projection); `StorefrontShippingDraftView.ShippingGroups` |
| Application | `StorefrontShippingService.SaveSelectionAsync` plans groups from authoritative Cart ownership + authoritative eligible methods, persists them, and maps them back |

## Enforced invariants (server-side, fail-closed)

1. every authoritative cart line belongs to **exactly one** group — omitted, unknown/foreign, or duplicated lines are refused;
2. a group belongs to **one** seller taken from Cart ownership (never from the client) — mixed-seller merges are refused;
3. one seller + one canonical method = **one** group in v1 (whole-line membership; partial split deferred);
4. method identity must resolve inside the authoritative eligible set, and any declared `ServiceCode`/`OptionCode` must match the authoritative identity;
5. `OriginContext` is **only** accepted empty: no trusted fulfilment-origin authority exists at draft-selection time, so an asserted origin is refused instead of forged (decision (a));
6. amount, currency, label and lead days are taken from the authoritative method — the client cannot forge money;
7. the selected delivery date/window must satisfy the **latest** minimum-delivery promise across the selected groups.

## Backward compatibility

- `ShippingGroups` is optional; when absent the legacy scalar request expands to **exactly one** group covering every cart line, reproducing today's single-method behavior (including amount/date/window/slot validation).
- Existing accepted-order columns (`checkouts.*`) and the legacy draft scalar columns are untouched; the scalar keeps working as a projection (`ShippingMethodCode`/`Label`/`MinimumDeliveryDate`/schedule from the primary group, `ShippingAmount` = sum of group amounts).
- Drafts written before this slice have zero groups; `ProjectPaymentDraftAsync` and `CommitAsync` treat `0 or 1` group as the legacy path and only fail closed (`shipping.rejected`) when the legacy single-method payment/order conversion would silently collapse a multi-group selection. That guard is the honest boundary of this slice.
- No automatic commit, cart clear, reservation or order creation is introduced; navigation alone still mutates nothing.

## Not implemented (explicitly out of scope)

Accepted-order group conversion (M2), Fulfillment shipment-per-group (M3), per-group Settlement allocation/ledger (M4), grouped Checkout UI (M5), per-seller/origin method eligibility, and any commercial allocation policy. Frontend, CSS, templates and theme were **not touched** (visual non-regression lock honored).

## Known limitations / honest boundaries

- Draft group replacement uses a deliberate **two-phase** save (flush deletions, then insert) so that the in-batch delete/insert order cannot violate `UNIQUE(cart_line_id)`. If the second phase fails, the draft is left with the legacy scalar projection and zero groups, which fails closed rather than mis-grouping.
- The EF model is validated by convention against the certified `SellerOrder.Lines` navigation shape and by a successful Host build; no runtime database was started in this slice (shared environments untouched).
- Per-seller method eligibility does not exist in the shipping catalog; grouping by seller is grounded in Cart ownership, but method *eligibility* remains cart-wide and is recorded as an open gap.

## Next slice

M2 — durable accepted-order groups (`order_shipping_groups` + `order_shipping_group_lines`), idempotent draft→order conversion, legacy checkout read path, and one vertical DB readback proof.
