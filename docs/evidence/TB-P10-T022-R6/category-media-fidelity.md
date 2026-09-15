# Category Media Fidelity — TB-P10-T022-R6

## Problem

LandingCategoryGrid used `/images/categories/{n}.png` placeholders (electronics/toys), ignoring Category.ImageMediaAssetId.

## Repair

1. TemplateCategory.ImageMediaAssetId already seeded to Fashion Template media Guids.
2. FashionTemplatePreviewQuery returns `imageMediaAssetId` + resolved `/images/fashion-template/{n}.jpg`.
3. StorefrontCategoryItem optional image fields; LandingCategoryGrid prefers `imageUrl` / `imageMediaAssetId` via storefrontMediaUrl before placeholder fallback.
4. Fashion preview roots therefore show Fashion-template local imagery, not generic category PNGs.

Evidence attribute: `data-category-media="template"` when Template media present.
