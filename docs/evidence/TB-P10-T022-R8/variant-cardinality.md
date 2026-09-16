# Variant Cardinality

Registry `VariantDefinition` now carries code-owned `previewMinItems` / `previewTargetItems` (not Admin settings). Examples: banner.two-equal=2, banner.four-grid=4, reviews.card-carousel=3, product.card-carousel=4. Policy falls back to responsive `itemVisible` desktop tokens when metadata absent.
