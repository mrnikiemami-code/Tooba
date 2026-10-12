# Scope decisions — TB-FE-STOREFRONT-COMPOSITION-CONTRACT-BATCH-001

Baseline: `docs/architecture/evidence/TB-FE-ADMIN-GRID-RICHTEXT-TYPE-BATCH-001/typecheck-after-tsc.txt`
= 10 diagnostics (1 × TS2740, 8 × TS2339, 1 × TS2741).
After: 0 diagnostics (`typecheck-after-tsc.txt`, empty; `tsc --noEmit` exit 0).

---

## SCOPE A — HERO SLIDE COMPLETE CONTRACT — **FIXED** (1 × TS2740)

**Target:** `src/frontend/app/storefront/storefront-landing-blocks.tsx` (`LandingHero`, legacy
compat path ~:721).

**Evidence (producer/consumer, no guessing):**

1. `HeroSlideConfig` (`lib/storefront-composition/hero-slider-config.ts:192-221`) declares six
   **required** panel fields: `panelMediaAssetId`, `panelImageUrl`, `panelColor`, `panelSize`,
   `panelOpacity`, `panelSide`.
2. The canonical producer for an unconfigured slide already exists:
   `emptyHeroSlide()` (`hero-slider-config.ts:253-273`) →
   `panelMediaAssetId: ""`, `panelImageUrl: ""`,
   `panelColor: HERO_DIAGONAL_PANEL_DEFAULT_COLOR` (`:29` = `"#0f172a"`),
   `panelSize: "xlarge"`, `panelOpacity: 100`, `panelSide: "left"`.
3. The **same file** already spells out that identical no-panel record literally in its
   `safeSlides` fallback (`storefront-landing-blocks.tsx:336-355`), value for value
   (`panelColor: "#0f172a"` there === `HERO_DIAGONAL_PANEL_DEFAULT_COLOR`).
4. The legacy compat path is genuinely reachable: `LandingHero` takes the legacy branch whenever
   `hasConfiguredSlides(config)` is false (`:69-71`, `:688`), and it is invoked from
   `renderSharedLandingSection` (`shared-composition-renderer.tsx:548`) with
   `layout = heroLayoutFromVariant(variantKey)`.
5. The renderer reads the panel fields **unconditionally** inside
   `if (variant === "split")` (`:423-436`) and `if (variant === "diagonal")` (`:526-530`):
   `slide.panelMediaAssetId.trim()` / `slide.panelImageUrl.trim()`.
   So before this change a legacy single-image hero on a split/diagonal layout threw
   `TypeError: Cannot read properties of undefined (reading 'trim')` — i.e. the composed
   landing page crashed. Note `safeSlides` only guards the *empty array* case, so this
   legacy-built slide was never covered by it.

**Decision:** semantically this inline object **is** the no-panel case, and the applicable
canonical values already exist and are already used in this very file. Therefore the six fields
are supplied from `emptyHeroSlide()` (a spread, not hand-copied literals), keeping the existing
`mediaAssetId`, `imageUrl`, `title`, `alt`, `description`, `ctaLabel`, `destinationType`,
`targetId`, `targetSlug`, `targetLabel`, `customUrl`, `href` overrides unchanged and in the same
order (spread first, overrides after).

**No invention:** no artwork, no product/category intent, no new placeholder, no default that is
not already shipped in the same file's fallback.

**Runtime/visual delta (disclosed):**
- Variants that do not read panel fields (`fullscreen` — the `LandingHero` default, `shapes`,
  `editorial`, `cinematic`; `DiamondSlideFace` reads only image/title/alt/description/CTA):
  **zero DOM delta**.
- `split` / `diagonal` + legacy single-image config: previously a runtime crash, now the same
  canonical no-panel panel that `safeSlides` already yields (dark `#0f172a` panel, `xlarge`,
  left, opacity 100, no panel image). This is a crash → canonical-default transition, flagged
  here for Architect visual acceptance; it is not a redesign and not new artwork.

---

## SCOPE B — SECTION DISPLAY CONFIG BANNER CONTRACT — **FIXED** (8 × TS2339)

**Target:** `src/frontend/lib/storefront-composition/shared-composition-renderer.tsx`
(`renderSharedHomeSection`, `product.sunny|money|cinematic|cinematic-plus|explorer` case).

**Evidence:**

1. `SectionDisplayConfig` (`app/composition/composition-api.ts:65-71`) is the **whitelist**
   contract: `title?`, `subtitle?`, `href?`, `itemCount?`, `sourceKind?`, `heightPreset?`.
2. Its only producer is `parseSectionDisplayConfig()` (`composition-api.ts:215-231`), which maps
   exactly `title`, `subtitle`, `href`, `itemCount`, `sourceKind` from `configurationJson` and
   **drops every other key**. Covered by an explicit contract test
   (`app/composition/composition-api.test.ts:78-83`, "reads safe keys only").
3. `storefront-home.tsx:81` feeds that parsed object into
   `renderSharedHomeSectionForLegacyType(..., config)` → `renderSharedHomeSection(variantKey,
   context, config)` (`shared-composition-renderer.tsx:452`). So on the Home path
   `config.bannerImageUrl`, `config.bannerMediaAssetId` and `config.bannerHref` **cannot exist**:
   the reads always evaluated to `undefined`.
4. The banner keys are nevertheless a **real** contract — but a *landing/variant-local* one:
   - producer: the admin section editor writes them
     (`app/admin/landing-pages/admin-landing-section-forms.tsx:350-422`);
   - consumer: the **landing** path reads them from the raw record
     (`SharedLandingRenderInput.config: Record<string, unknown>`, `:88`) in the duplicated
     sunny/explorer block at `:657-698`;
   - consumer behaviour: `ShowcaseProps.bannerImageUrl?/bannerHref?` are optional
     (`storefront-product-showcase-embla-rails.tsx:381-382`) and render the Explorer side banner
     only when `isBeauty && bannerImageUrl?.trim()` (`:901`, `:906`, `:915-921`).
5. Independent corroboration: the pre-existing root-cause audit already recorded that
   `parseSectionDisplayConfig()` makes `bannerImageUrl` unreachable for `/fa` Home sections
   (`docs/architecture/evidence/TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001/summary.md:82`).

**Decision:** on the Home path these are **stale legacy reads over the wrong contract**, not
missing fields. They are removed (with an explanatory comment), and the two props are no longer
passed there. Because they were always `undefined`, `bannerImageUrl={undefined}` ≡ prop omitted,
so **rendered output is byte-identical** (Explorer banner never rendered on the Home path before
or after; the `data-explorer-banner="0"` / `"relative"` branches are unchanged).

**Rejected alternatives (why):**
- Adding `bannerImageUrl`/`bannerMediaAssetId`/`bannerHref` to `SectionDisplayConfig` and to the
  parser would have *changed runtime behaviour* (banners would start appearing on `/fa` Home) —
  explicit scope violation ("do not add fields merely because renderer reads them", no hidden/
  new UI).
- `config as Record<string, unknown>` / `any` / `Record<string, any>` widening —
  forbidden by the task and by the repo type-safety contract.

**No visual loss:** nothing that rendered before stopped rendering; the landing path that does
honor these keys is untouched.

---

## SCOPE C — PREVIEW FAKE BANNER STRUCTURE — **FIXED** (1 × TS2741)

**Target:** `shared-composition-renderer.tsx`, `banner.mosaic|grid|three|four-grid|
one-large-two-small|one-large-four-small|eight-compact` case (`:790-819`).

**Evidence:**

1. `applyPreviewFill<T>({ realItems: readonly T[]; createFake: (index) => T })`
   (`lib/storefront-composition/preview-fill-policy.ts:82-88`) unifies the real element type `T`
   with the fake producer's return type.
2. `createFakeBanner()` returns `PreviewFakeBannerItem`
   (`preview-fake-data.ts:146-164`: `src`, `href`, `title`, `objectPosition`, `previewFake: true`,
   `previewSource`) — it has **no** `alt`.
3. The real `realItems` map always emitted an `alt` key, so TS inferred a bespoke element type
   with a **required** `alt: string | undefined`, which `PreviewFakeBannerItem` cannot satisfy →
   TS2741.
4. The actual consumer, `CompositionBannerGrid`, declares
   `items?: Array<{ src?; href?; title?; objectPosition?; previewFake? }>`
   (`storefront-home-blocks.tsx:551`) — it does **not** read any `alt` field and builds the image
   alt from the title (`:595`).

**Decision:** annotate `realItems` with the true shared consumer element contract
(`src?`, `href?`, `title?`, `alt?`, `objectPosition`, `previewFake?`) instead of relying on
literal inference. This is the task's preferred remedy ("sound shared consumer element typing"):
no global `alt`-optional weakening, no weakening of discriminators, `previewFake`/`previewSource`
provenance preserved, no accessibility text invented (`alt` stays exactly as authored on real
items and absent on fakes), **zero runtime delta**.

**Rejected alternatives (why):**
- `alt` from the fake's own title — would have changed rendered content (new `alt` attributes in
  Store preview) and the grid does not even read `alt`.
- Making `alt` optional on `PreviewFakeBannerItem` — would require editing
  `preview-fake-data.ts`, a path outside this task's allowlist; and the fake genuinely has no alt.
- `@ts-ignore` / cast / `any` — forbidden.
