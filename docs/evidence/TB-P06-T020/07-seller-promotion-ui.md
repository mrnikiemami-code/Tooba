# 07 — Seller promotion UI (TB-P06-T020)

## Routes

| Route | Component |
|---|---|
| `/vendor-panel/coupons` | `coupons-list.tsx` (live list) |
| `/vendor-panel/coupons/new` | `coupon-form.tsx` |
| `/vendor-panel/coupons/[id]/edit` | `coupon-form.tsx` (draft/expired) |

Source visual map: Shopeiva `couponsList.jsx` / `couponForm.jsx` (`#E53935` accent, card grid, stats strip, search/filter, status badges).

## API client

`seller-api.ts`: `loadSellerPromotions`, create/update/activate/deactivate.

## Nav

`vendor-shell.tsx`: coupons `live: true`; removed from `VENDOR_DEFERRED_NAV_HREFS`.  
`panel-nav-integrity.test.ts` updated.

## Honesty

- No fake usage/maxUses progress bars (Shopeiva had mock counts)
- Activate/deactivate call Host; edit blocked while Active
