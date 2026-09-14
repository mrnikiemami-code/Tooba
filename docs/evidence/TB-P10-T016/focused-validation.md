# Focused validation — TB-P10-T016

- Host StoreAppearance: 20/20 (output `.tmp-t016-test-out`, not committed)
- FE appearance / palette / surface-role / theme: 23/23
- recovery staleness guard: 4/4
- `npm run test:critical-storefront`: 18/18
- `git diff --check`: clean (CRLF warnings only)
- Live PUT invalid BackgroundStyle: recorded in runtime capture (`appearance.background.invalid`)
- Live public `/v1/storefront/appearance` includes four-role `tint` tokens ≠ primary
