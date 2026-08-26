# 02 — Home Guard Proof

- Contract: `docs/visual-baselines/HOME-FIDELITY-CONTRACT.md`
- Structural assertions: `src/frontend/app/storefront/home-structure.guard.test.ts`
- Markers asserted: `storefront-home`, `home-hero`, `home-stories`, `home-categories`, `home-flash-sales`, `home-best-sellers`, `home-most-viewed`, `home-middle-banners`, `home-brands`, `home-new-products`
- Order asserted inside main Home composition
- Full Catalog dump patterns rejected (`home-all-categories`, `{categories.map(`)
- Command: `npm run test:home`
