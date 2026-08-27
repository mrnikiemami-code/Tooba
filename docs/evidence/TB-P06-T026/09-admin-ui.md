# 09 — Admin Gift Card / Wallet UI

Task: TB-P06-T026

## Routes

| Route | Behavior |
|-------|----------|
| `/admin/gift-cards` | List + search/status filter + issue form (DisplayCode once) |
| `/admin/gift-cards/[id]` | Detail + redemption history + revoke |
| `/admin/wallets` | Inspect by CustomerActorUserId + ledger + audited adjustment |

## Host paths (FE wired; Host may land shortly)

- `GET/POST /v1/admin/gift-cards`
- `GET /v1/admin/gift-cards/{id}`
- `POST /v1/admin/gift-cards/{id}/revoke`
- `GET /v1/admin/wallets/{customerActorUserId}`
- `GET /v1/admin/wallets/{customerActorUserId}/ledger`
- `POST /v1/admin/wallets/{customerActorUserId}/adjustments`
- `GET /v1/admin/wallet/demo-preview`

## Nav + ACC

- Admin nav: کارت هدیه (`giftcard.view`), کیف پول مشتریان (`wallet.view`)
- No arbitrary balance overwrite UI — adjustment posts immutable ledger entry only

## Modules

- `src/frontend/app/wallet/wallet-api.ts`
- `src/frontend/app/wallet/wallet-ui.tsx` (`AdminGiftCardsScreen`, `AdminGiftCardDetailScreen`, `AdminWalletInspectScreen`)
