# 13 — Customer visual regression (TB-P06-T029)

Compare closest Shopeiva User Panel patterns. **No redesign.**

## Surfaces

| Route | URL | Note |
| --- | --- | --- |
| Dashboard | http://localhost:3000/customer-panel | Capture `captures/customer-dashboard.png`; fake «wallet unavailable» copy **repaired** (`02-dead-fake-ux-sweep.md`) |
| Orders | `/customer-panel/orders` (+ seeded detail) | LIVE |
| Notifications | `/customer-panel/notifications` | LIVE |
| Tickets | `/customer-panel/tickets` | LIVE |
| Wallet | `/customer-panel/wallet` | LIVE (T028-R1 ACCEPT) |
| Gift cards | `/customer-panel/gift-cards` | LIVE |
| Settings / profile | `/customer-panel/settings`, `/profile` | LIVE (T027) |

## Findings

| Item | Result |
| --- | --- |
| Unauthorized foreign panel chrome | None requiring repair this gate |
| Dead/fake wallet messaging | Fixed → honest «فعال» + quick actions |
| Shopeiva lock | Preserved shell/cards/forms language |

## Verdict

Customer panel visually coherent with prior ACCEPTED wallet/settings work; dashboard honesty repair is the only T029 UX source change in this sweep.
