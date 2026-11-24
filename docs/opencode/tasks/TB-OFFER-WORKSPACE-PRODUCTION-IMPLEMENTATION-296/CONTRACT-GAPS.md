# CONTRACT GAPS — TB-296 (source-verified; no fabricated capability)

Evidence gathered from live backend endpoint sources:
- Offer: `POST /offers/query`, `POST /offers` (create), `GET/PATCH /offers/{id}`, `GET /offers` (list), `PUT /offers/{id}/fulfillment` (admin).
- Promotion: campaign-centric only — `/` list/create/update, `/{id}/publish|archive`, `/{id}/members` add/remove/order, `/{id}/members/{sellerOfferId}/price`. NO per-offer discount-definition API (percent/fixed/coupon/priority/min-qty/min-amount/customer conditions).
- Inventory: **no inventory endpoints module** found (`*Endpoints*.cs` returned none) → no multi-warehouse adjust+reason HTTP contract.
- Shipping: service authoring/carrier endpoints exist; per-offer **specific service assignment** is not an endpoint (only Offer `PUT /offers/{id}/fulfillment` sets responsibility + shipping type id). Stage-3 service assignment rule is not decided.
- Offer history: **no offer history endpoint/projection** found (only unrelated order/fulfillment history).

## Impact per tab
| Tab | Contract status | Can implement now without fabrication? |
|---|---|---|
| مشخصات آفر | create/get/patch + product/seller pickers exist | YES (identity locks, pickers, SKU, channel) |
| قیمت‌گذاری | price write DTO exists (SellerOfferPriceWriteRequest) but no listed per-currency rows CRUD route found; verify before build | PARTIAL — must verify price-write route; multi-currency rows may be a gap |
| تخفیفات | Promotion is campaign-based; no per-offer discount-definition API | NO — would require new Promotion contract (decision) |
| موجودی (چندانباره + اصلاح + دلیل) | no Inventory endpoints module found | NO — backend contract missing |
| ارسال (انتخاب سرویس مجاز) | responsibility via fulfillment assign; per-offer service assignment not present | PARTIAL — responsibility only; service assignment needs Stage-3 decision |
| شرایط فروش (مقدار/واحد/مرجوعی/چرخه) | patch supports return policy + qty limits; unit/step from product; lifecycle via status | YES (return default/custom/non-returnable, min/max, status) |
| تاریخچه | no offer history API | NO — backend contract missing |

## Conclusion
The shared 7-tab production workspace cannot be completed truthfully end-to-end because three required capabilities lack backend contracts
(Promotion per-offer discounts, Inventory multi-warehouse adjust, Offer history) and shipping service assignment is an undecided Stage-3 rule.
Implementing them now would require inventing backend behavior (forbidden) or new architecture decisions (architect-owned).

Independent, implementable-now sections (identity/pickers/SKU/channel; returns; quantity limits; lifecycle; price if write-route verified; responsibility-only shipping)
can proceed without the blocked domains.
