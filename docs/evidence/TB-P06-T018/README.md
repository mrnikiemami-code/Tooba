# Evidence — TB-P06-T018

**Commercial Panel Completion Wave 1 — Customer / Seller / Admin honesty + live settings (no fake modules)**

| Field | Value |
|---|---|
| Task-ID | `TB-P06-T018` |
| Bridge UUID | `de718665-85f2-464e-bfc1-4436f4c3e786` |
| Predecessor | `1ff6b7fe4cc18f900536a235f912fe4e1fb2d06a` |
| Worker / Channel | `tooba-worker-01` / `tooba-main` |
| May report readiness | `COMMERCIAL_PANEL_WAVE1_LIVE` (NOT `PRODUCT_FULLY_READY`) |

## Wave 1 selected scope (honest)

| Panel | Completed in Wave 1 | Intentionally deferred |
|---|---|---|
| Customer | Nav honesty (hide wallet/tickets/gift-cards/notifications); settings profile bridge + locale cookie; dashboard quick actions live-only | Wallet, tickets, notifications, gift-cards (no Host) |
| Seller | Nav honesty (hide customers/coupons/reviews/tickets/gift-cards); live operational settings from seller dashboard API; dashboard settings action live | Customers, coupons, reviews, tickets, gift-cards; business profile edit |
| Admin | Hide settings from primary nav (route remains honest unavailable) | Admin settings module |
| Foundations | — | Notifications foundation NOT selected; Support/Tickets foundation NOT selected |

## Files

| # | File | Topic |
|---|---|---|
| 01 | `01-runtime-before-panel-completion.md` | Runtime at claim |
| 02 | `02-panel-gap-input-matrix.md` | T014 readiness + T017 Story live |
| 03 | `03-shopeiva-panel-route-gap-map.md` | Shopeiva vs Tooba panel routes |
| 04 | `04-selected-commercial-gap-plan.md` | Wave 1 selected gaps |
| 05 | `05-customer-panel-completion.md` | Customer Wave 1 |
| 06 | `06-seller-panel-completion.md` | Seller Wave 1 |
| 07 | `07-admin-panel-completion.md` | Admin Wave 1 |
| 08 | `08-notification-foundation.md` | NOT selected + why |
| 09 | `09-support-ticket-foundation.md` | NOT selected + why |
| 10 | `10-settings-preferences-live.md` | Settings honesty / live prefs |
| 11 | `11-panel-navigation-integrity.md` | Primary nav live-only |
| 12 | `12-panel-fake-stub-audit.md` | Fake/stub audit after Wave 1 |
| 13 | `13-panel-i18n-proof.md` | Locale / RTL-LTR on touched routes |
| 14 | `14-new-ui-native-fit-map.md` | Source-derived native fit |
| 15 | `15-panel-browser-side-by-side.md` | Capture placeholders |
| 16 | `16-panel-authorization-proof.md` | No auth model change |
| 17 | `17-panel-boundary-proof.md` | Frontend-only wave |
| 18 | `18-panel-integration-tests.md` | Frontend nav integrity tests |
| 19 | `19-final-validation.md` | Validation command placeholders |
| 20 | `20-final-runtime.md` | Post-wave runtime probes |
| 21 | `21-commercial-readiness-after-panel-wave1.md` | Honest % after Wave 1 |

## Artifacts (pending / follow-up)

- Browser captures: `captures/` (to be added; see `15-panel-browser-side-by-side.md`)
- Proof script: to be added when captures are recorded
