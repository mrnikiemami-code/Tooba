# 01 — Runtime before panel completion (TB-P06-T018)

## Claim context

| Field | Value |
|---|---|
| Task | TB-P06-T018 Commercial Panel Completion Wave 1 |
| Bridge UUID | `de718665-85f2-464e-bfc1-4436f4c3e786` |
| Predecessor | `1ff6b7fe4cc18f900536a235f912fe4e1fb2d06a` |
| Branch | `main` |
| HEAD vs origin/main | `HEAD == origin/main` at predecessor |
| Worker / Channel | `tooba-worker-01` / `tooba-main` |
| Pipeline | BRIDGE-WAKE-V1 |

## Runtime probes at claim

| Probe | Port / URL | Result |
|---|---|---|
| Host health | `http://127.0.0.1:5088` (`/health/live`, `/health/ready`) | ok |
| Tooba Frontend | `http://127.0.0.1:3000` | up |
| Original Shopeiva | `http://127.0.0.1:3001` | up |
| Bridge | `http://127.0.0.1:17321` | ok |
| Customer Dashboard | `/customer-panel` | reachable for Wave 1 baseline |
| Seller Dashboard | `/vendor-panel` | reachable for Wave 1 baseline |
| Admin Dashboard | `/admin` | reachable for Wave 1 baseline |
| Storefront locale | `/fa` | reachable (T016 foundation) |

## Baseline notes carried in

- TB-P06-T017 Stories = ACCEPTED; Story surface live (~85%).
- TB-P06-T014 commercial readiness: Customer ~75%, Seller ~70%, Admin ~80%, Storefront ~85%, Blog ~90%.
- Presentation risk: visible primary-nav items and dashboard tiles still pointing at unavailable capabilities (wallet/tickets/etc.) or stub settings.
- No Host module work planned for Wave 1 foundations (notifications / tickets deferred).
