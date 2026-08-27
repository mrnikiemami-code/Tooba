# 07 — FE Shopeiva lock notes

Task: TB-P06-T026

## Sources

- `reference/shopeiva/.../dashboard/wallet/wallet.jsx`
- `reference/shopeiva/.../userGiftCards/*`

## Tooba binding

| Surface | Path |
|---------|------|
| Customer wallet | `app/customer-panel/wallet` + `app/wallet/wallet-ui.tsx` |
| Customer gift redeem | `app/customer-panel/gift-cards` |
| Shared API | `app/wallet/wallet-api.ts` |
| Admin gift cards | `app/admin/gift-cards` |
| Admin wallets | `app/admin/wallets` |

Accent `#E53935` on wallet/gift hero. Fake deposit/withdraw/cards not rendered as live actions.
Nav: customer wallet + gift-cards live; admin giftcard.view / wallet.view projected.
