# recovery-start TB-P10-T022-R13

## Git preflight

- branch: main
- HEAD: 454ef53221017e107aee7f8f9c71af3ce533b07a
- origin/main: 454ef53221017e107aee7f8f9c71af3ce533b07a
- expected previous HEAD: 454ef53221017e107aee7f8f9c71af3ce533b07a
- match: True
- claimed UUID: 3709481d-78de-4e87-ab31-fb0264ace57c
- tracked divergence: none (clean vs origin/main; only pre-existing untracked `.tmp-*` / historical evidence noise — not task scope)
- RECOVERY_CONFLICT: no

## StoreTemplate keys (10)

`fashion`, `auto-parts`, `building-materials`, `tools-hardware`, `tile-ceramic`, `interior-decor`, `home-appliances`, `shoes`, `plants`, `beauty`

FE: `listIndustryTemplates()` / `INDUSTRY_TEMPLATE_SEEDS`  
BE: Fashion + Industry Batch A/B/C seeds → `StoreTemplates`

## Store Pages IA

- Admin module label: «صفحات فروشگاه» (`/admin/landing-pages`)
- Home + Landing share one composer (`admin-landing-page-composer.tsx`)
- Home route `/`; Landing `/landing/{slug}`
- Exactly one active Home; set/restore Home APIs retained from R9
- Canonical AppDataGrid profile (`data-grid-profile=orders-canonical`)

## Home / Landing page model

- Page types Home | Landing
- Unified section workspace: edit / enable-disable / delete / move up-down / drag-drop / insert before-between-after
- Template Apply materializes full ordered composition into Page Draft
- Disabled sections remain in Admin, excluded from published Storefront

## SEO model

- Admin panel `data-testid=page-seo-panel` (title, description, canonical, robots Index/NoIndex + Follow/NoFollow, OG title/description/image, hreflang/locale)
- Storefront: `storefront-page-seo.ts` → metadata + typed JSON-LD; sitemap exclusion for noindex
- No raw JSON SEO editor; no mandatory per-section SEO wizard step (R9)

## Template selector

- All 10 cards with Persian name, description, section list, industry photo (not geometric thumb)
- Live iframe Desktop/Tablet/Mobile + language + Sample/Store source + «استفاده از این قالب»
- Preview routes `/template-preview/{key}` (+ `/full`) via shared engines

## Preview mode / source isolation

- Sample → Template Catalog only
- Store Preview → operational Store first; missing slots → PreviewFake only (`/images/preview-placeholder/`)
- Published → operational Store only (no PreviewFake, no Template fallback)
- Guards: R8 / R8-R2 / Batch A/B/C Host purity tests

## Page Editor / Variant Picker

- Unified composer workspace (R10)
- Variant Picker V2 (R11): Persian «طرح …» names, production components via PreviewFake, no geometric canvas
- R13 removes unused `VariantPreviewCanvas` export remnant

## Drag/drop / order persistence

- Arrow + drag share canonical order model; server-persisted (R10)

## Template seed counts (runtime Host probe at start)

All 10 packs: products=15, roots=8, brands=6, purity=true

## Performance / caching

- R9-R1 Landing warm SSR low-single-digit / sub-second locally; Store+locale+page cache isolation
- Variant picker IntersectionObserver lazy mount (R11)
- Selector: single selected iframe (not 10 eager expensive mounts)

## Unrelated user changes

- Preserved; not modified by this task
- Do NOT invent TB-P10-T023; do NOT implement cloning; Builder USER_VISUAL_ACCEPTED remains NO
