# 06 — Gift Card redemption browser / API

Task: TB-P06-T026-R1

URL: `http://localhost:3000/customer-panel/gift-cards`

## Capture

- `captures/06-customer-gift-cards.png` — balance strip + redeem form + gift-credit history

## Proof (API + UI)

| Step | Result |
|------|--------|
| UI redeem form | present; no fake owned-card inventory |
| Valid redeem (spare) | amount 250000; balance 1100000 |
| Duplicate same Idempotency-Key | idempotentReplay **true**; balance unchanged |
| Expired code | 400 safe reject |
| Unused preview after repair seed | `TOOBA-DEMO-GIFT-R1` via demo-preview (code documented for Architect preview; not a production secret) |

Redeem codes used in smoke are Development-only demo constants.
