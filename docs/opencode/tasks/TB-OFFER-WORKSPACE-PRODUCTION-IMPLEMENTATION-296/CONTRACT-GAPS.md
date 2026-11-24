# CONTRACT GAPS — TB-296 (corrected after §0A route investigation, 2026-11-25)

Re-investigated actual routes/commands (not filename absence):
- Offer price write: `POST|PUT /offers/{offerId}/price` → `SetOfferPriceCommand` (admin group + seller endpoints import it). EXISTS.
- Offer inventory write: `POST|PUT /offers/{offerId}/inventory` → `SetOfferInventoryCommand`. EXISTS (verify whether it is per-location/multi-warehouse).
- Offer fulfillment responsibility: `PUT /offers/{offerId}/fulfillment` (admin, `fulfillment.manage`). EXISTS (responsibility + shippingTypeId only).
- Offer create/get/patch/list/query: EXIST.
- Promotion: campaign-centric (`/campaigns`, `/{id}/members`, `/{id}/members/{sellerOfferId}/price`). No per-offer DISCOUNT-DEFINITION contract (percent/fixed/coupon/priority/min-qty/min-amount/customer conditions).
- Offer history: no read API/projection found.

## Scoped status per tab (per §0A)
| Tab | Status | Note |
|---|---|---|
| مشخصات آفر | IMPLEMENTABLE | create/get/patch + pickers |
| قیمت‌گذاری | IMPLEMENTABLE (price write exists) | multi-currency ROWS UI over existing price write; verify per-currency semantics |
| تخفیفات | BLOCKED (policy/contract) | per-offer discount definition not a decided capability (Promotion is campaign-based) — architect decision; campaign membership can be shown read-only |
| موجودی | PARTIAL | inventory write exists; MULTI-warehouse ADJUST+reason per-position may need contract/policy — verify SetOfferInventory semantics |
| ارسال | PARTIAL | responsibility exists; per-offer service assignment is undecided (Stage-3) |
| شرایط فروش | IMPLEMENTABLE | return policy + qty limits + lifecycle via patch |
| تاریخچه | BLOCKED (contract) | no history read API — implementable only if an audit/event projection is authorized |

## Conclusion
Not a task-wide blocker. Independent implementable slices (identity, price, single-location inventory write, returns, qty limits, lifecycle, responsibility shipping) can proceed.
Genuinely undecided/authority-missing (isolated): per-offer discount definition, per-offer shipping service assignment (Stage-3), multi-warehouse adjust policy, offer-history projection.
