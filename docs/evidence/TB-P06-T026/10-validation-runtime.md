# 08 — Validation + final runtime

Task: TB-P06-T026

## Backend

| Check | Result |
|-------|--------|
| Host.Tests | **Passed 286** / Failed 0 / Skipped 0 |
| Host build | 0 warnings / 0 errors |
| git diff --check | clean (CRLF warnings only) |

## Frontend

| Check | Result |
|-------|--------|
| `npm run test:wallet` | 4 pass |
| `npm run test:customer` | 29 pass |
| `tsc --noEmit` | exit 0 |

## Runtime (kept alive)

| Service | URL | Status |
|---------|-----|--------|
| Host | `:5088` health/live + ready | 200 |
| FE | `:3000` `/customer-panel/wallet` | 200 |
| Shopeiva | `:3001` | 200 |

## Claims (honest)

- `WALLET_LEDGER_LIVE`, `GIFTCARD_LIVE`, `WALLET_USER_PREVIEW_READY`
- **Not** claimed: `USER_VISUAL_ACCEPTED`, `PRODUCT_FULLY_READY`, `PRODUCTION_GO_LIVE_READY`
- Checkout spend + refund-to-wallet: **DEFERRED** (see 05)
