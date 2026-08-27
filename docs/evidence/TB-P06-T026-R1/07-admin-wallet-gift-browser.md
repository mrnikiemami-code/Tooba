# 07 — Admin wallet / gift browser

Task: TB-P06-T026-R1

## Captures

| File | Page |
|------|------|
| `captures/07-admin-gift-cards.png` | `/admin/gift-cards` list + issue |
| `captures/07b-admin-gift-detail.png` | seeded partial card detail + redemption history + revoke |
| `captures/07c-admin-wallet-inspect.png` | `/admin/wallets` ledger-derived balance + audited adjustment |

## Observed

- Seeded cards: Redeemed / PartiallyRedeemed / Expired / Revoked / Active (R1 preview)
- Detail `01900000-0000-7000-9000-000000000022`: remaining 100000; redemption history present
- Wallet inspect actor `aaaaaaaa-aaaa-4aaa-8aaa-000000000009`: balance 1,100,000; immutable adjust form (no direct set balance)
- Nav projected: `giftcard.view`, `wallet.view`
