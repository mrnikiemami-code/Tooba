# Final Product Create — TB-P07-T031

## Live
- `/fa/admin/products/new` → 200
- Code gate: **8** stages present in `product-create-screen.tsx`

## Stages (live wizard)
1. Primary Category (`category`)
2. Base information (`structure`)
3. Translations (`translations`) — Draft-first create after FA name
4. Attributes (`attributes`) — reuses `ProductAttributesPanel`
5. Variants (`variants`) — reuses `ProductVariantsPanel`
6. Media (`media`) — reuses `ProductMediaPanel` / real DAM
7. SEO (`seo`) — reuses `ProductSeoPanel`
8. Review & create (`review`) — readiness summary (category, brand, translations, attributes, variants, media, SEO, publish)

## Guardrails verified
- Draft-first (default Draft)
- No duplicate invent of Seller/Offer price-stock stages
- TipTap rich description (no CKEditor)
- fa/en translations; RTL/LTR by locale
- Mobile-usable sequential flow (create stays sequential by design vs VIEW/EDIT side media)
