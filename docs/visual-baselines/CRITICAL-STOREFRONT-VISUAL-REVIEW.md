# Critical Storefront Visual Review

Use this checklist for **any** Task that touches Home, PDP, or shared storefront components (Product Card, Header, layout primitives, tabs, carousel, image wrapper, pricing/rating presentation).

## Absolute rules

- Home and PDP are critical Shopeiva-locked surfaces.
- Functional PASS ≠ Visual ACCEPT.
- No redesign, reinterpretation, or generic structural flattening.

## Verify before Worker PASS

Home unchanged unless explicitly in scope:

- [ ] Section order matches `HOME-FIDELITY-CONTRACT.md`
- [ ] No giant Catalog dump on Home
- [ ] Rails / brands / mid-banners preserved
- [ ] Mobile 390×844 still faithful

PDP unchanged unless explicitly in scope:

- [ ] Sticky tabs preserved
- [ ] Tabs remain structurally distinct
- [ ] No generic tab flattening
- [ ] Other sellers / related seams preserved where data supports
- [ ] Mobile tab strip usable

Shared / geometry:

- [ ] Shopeiva geometry preserved
- [ ] No generic replacement of card/header/tab patterns
- [ ] No arbitrary density change
- [ ] No section loss
- [ ] No fake promotions/ratings/prices

## Required automated checks

```bash
cd src/frontend
npm run test:critical-storefront
```

Capture refresh (when runtime available):

```bash
node scripts/capture-t019-critical-baselines.mjs
```

Contracts:

- `docs/visual-baselines/HOME-FIDELITY-CONTRACT.md`
- `docs/visual-baselines/PDP-FIDELITY-CONTRACT.md`
