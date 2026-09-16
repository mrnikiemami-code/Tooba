# Preview Fill Architecture

Central module: `src/frontend/lib/storefront-composition/preview-fill-policy.ts`

- `applyPreviewFill` — real + only missing fake slots
- `resolveVariantPreviewCardinality` — variant `previewMinItems` / `previewTargetItems`
- `isStorePreviewFillEnabled` — `preview && previewSource === 'store'` only

Wired in `shared-composition-renderer.tsx` for all data-driven landing sections. Fake factories in `preview-fake-data.ts` + locale maps in `preview-fake-locale.ts`.
