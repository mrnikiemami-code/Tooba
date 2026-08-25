# Mega Menu fidelity checklist

Comparison source:
`D:/Users/User/source/repos/SarvNewVerRequirment/reference/shopeiva/src/components/common/Header/Header.jsx`.

| Area | Score | Proof / deviation |
| --- | --- | --- |
| Header alignment | MATCH | trigger remains in the original secondary navigation position |
| Panel dimensions | MATCH | full-width band, max-1800 inner container, 460px maximum content height |
| Column layout | MATCH | 12-column `3 / 6 / 3` source proportions |
| Category hierarchy | MINOR TECHNICAL DEVIATION | live Catalog currently has two visible levels; source shell supports deeper descendants without inventing them |
| Typography | MATCH | source text scales/weights restored |
| Spacing | MATCH | source `py-5`, `gap-6`, rail and pane padding restored |
| Separators | MATCH | panel top border and middle pane cross-border restored |
| Promo region | MATCH | source two-block region restored with honest offers CTA and six live brands |
| Hover/open behavior | MATCH | 150ms leave protection, stable switching, click support, scroll-close |
| RTL behavior | MATCH | rail at right, content center, promo left; no clipping |
| Mobile behavior | MATCH | right drawer, main/root accordions, two-column child links, close/backdrop |

## Approved differences

- Shopeiva red is replaced by Tooba blue.
- Category and brand links use real Tooba routes and opaque backend IDs/slugs.
- Static Shopeiva demo strings and the unsupported “تا ۵۰٪ تخفیف” claim are not
  copied.
- Dark-mode variants were not added because the accepted Tooba storefront shell
  is light-only.
- Leaf rows are absent where Catalog has no third-level descendant; the layout
  is intentionally not padded with fake hierarchy.

## Result

```text
UNRESOLVED MAJOR VISUAL DIFFERENCES: NONE
PRODUCT CARDS IN MENU: NONE
PRICES / STOCK / SELLER OFFERS / RATINGS IN MENU: NONE
```
