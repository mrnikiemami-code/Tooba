# Browser Proof — TB-P10-T022-R20

## Runtime
- Host: http://127.0.0.1:5088
- Frontend: http://127.0.0.1:3000
- Probe: `docs/evidence/TB-P10-T022-R20/probe.mjs` EXIT 0 (`probe-report.json`)
- Capture: `docs/evidence/TB-P10-T022-R20/capture.mjs` → `screenshots/`

## Flow coverage (API + UI)
| Step | Proof |
|------|-------|
| A login Admin | capture A-login + actor localStorage |
| B campaign list | `/admin/campaigns` + shot 01 |
| C–E create Amazing + FA/EN | Host POST create + translations; shot 02 |
| F–H add ≥5 offers + reorder | probe F–I |
| I–K promo prices + persist | probe J–K |
| L–N publish → فعال | probe M–O |
| O–Q Builder Preview resolves | probe R (ProductCollection PromotionCampaign CampaignId=null) |
| R–S Storefront SSR | probe S + shot 07 |
| T–U Cart campaign price | probe T–U (`/v1/storefront/cart`) |
| V–X archive → not active | probe V–X (cache bust via page status) |
| W console / X loops | capture consoleErrors empty; no polling added |

## Screenshots
- `screenshots/01-campaign-list.png`
- `screenshots/02-create-basic.png`
- `screenshots/03-workspace-info.png`
- `screenshots/04-offer-membership.png`
- `screenshots/05-pricing-ui.png`
- `screenshots/06-status.png`
- `screenshots/07-storefront.png`
