# 04 — API smoke (Host)

Task: TB-P06-T026

Host: `http://127.0.0.1:5088` (Host header `localhost`, Dev actor `aaaaaaaa-aaaa-4aaa-8aaa-000000000009`)

## demo-preview

`GET /v1/admin/wallet/demo-preview` → 200

- balance seed: **350000** (admin credit 250k + partial gift 100k)
- unused code: `TOOBA-DEMO-GIFT-500K`
- note: checkout/refund deferred

## Customer wallet

| Step | Result |
|------|--------|
| GET `/v1/customer/wallet` before redeem | balance **350000** |
| POST redeem `TOOBA-DEMO-GIFT-500K` idem=`t026-redeem-smoke-1` | amount **500000**, walletBalance **850000**, idempotentReplay false |
| GET wallet after | **850000** |
| POST same idem replay | idempotentReplay **true**, balance still **850000** |
| POST expired `TOOBA-DEMO-GIFT-EXPIRED` | **400** `wallet.redeem.rejected` |
| GET ledger | total **3**, balance **850000** |

No direct DB mutation used.
