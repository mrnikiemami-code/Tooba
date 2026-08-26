# 01 — Shared Component Risk Map

| Component | Surfaces affected | Risk if drifted | Required regression |
| --- | --- | --- | --- |
| Product Card (`storefront-product-card.tsx`) | Home rails, PDP related, listings | Card-family replacement / density change | `test:critical-storefront` + Home/PDP visual checklist |
| Header (`storefront-header.tsx`) | All storefront | Sticky header / Mega Menu integration break | visual review checklist + Mega Menu smoke |
| Layout/container primitives | Home/PDP section rhythm | Arbitrary padding/width redesign | Home/PDP contracts |
| Typography tokens | Home/PDP headings | Hierarchy flattening | visual checklist |
| Buttons | PDP purchase / tabs | CTA geometry drift | PDP guard + capture |
| Tabs (`storefront-pdp.tsx` sticky strip) | PDP | Generic TabContent flatten | `test:pdp-guard` |
| Carousel / hero slider | Home hero | Hero loss / redesign | `test:home` + Home capture |
| Image wrapper / media URL helper | Gallery, cards | Broken media / crop redesign | Home/PDP captures |
| Pricing/rating presentation | Cards + PDP | Fake Product.Price / fake ratings | PDP guard + domain checks |

No shared-component refactor in TB-P05-T019 except non-visual `data-testid` hooks.
