# TB-P10-T009-R2 — Admin preview contract

`AdminProductCardSkinPreview` calls `resolveProductCardSkin` + `resolveProductCardSkinChrome` from `product-card-skin.ts`. No second registry. No preview-only CSS classes for skins.

Tiles and the large preview share that component. Fake content is labels only (image well, title, price, CTA).
