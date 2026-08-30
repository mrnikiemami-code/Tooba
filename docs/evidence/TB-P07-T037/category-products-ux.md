# Category Products UX

- Primary row: badge «دسته اصلی» + open product only — **no** trash/remove (even disabled)
- Display row: badge «نمایش در این دسته» + «حذف از این دسته»
- L1/L2: blocked banner (subtree helper) — **no** add CTA
- L3: «افزودن محصول برای نمایش در این دسته»
- Source tests in `category-products-panel.test.ts` enforce `visible: primary !== categoryId`
