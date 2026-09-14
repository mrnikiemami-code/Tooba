# Focused validation — TB-P10-T015

- Host StoreAppearance: 20/20 (output `.tmp-t015-test-out`, not committed)
- FE appearance / palette / theme: 20/20
- recovery staleness guard: 4/4
- `npm run test:critical-storefront`: 18/18
- `git diff --check`: clean (CRLF warnings only)
- Live PUT invalid BackgroundStyle: 400 `appearance.background.invalid`
- Live public `/v1/storefront/appearance` includes `backgroundStyle` + curated `tint` tokens ≠ primary
