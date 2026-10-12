PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-FE-STOREFRONT-COMPOSITION-CONTRACT-BATCH-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_MULTI_ERROR_REPAIR_WITH_INDEPENDENT_STOP_GATES
Branch: main
Expected-Starting-HEAD: 898acdf23151902ee6d5bb3b9f0b643526ef4a6c
Actual-Starting-HEAD: 898acdf23151902ee6d5bb3b9f0b643526ef4a6c (== origin/main, tracked tree clean)

RECEIPT SUMMARY
----------------
Scopes A, B and C each investigated independently with producer/consumer evidence and each
independently RESOLVED. No scope was blocked; no scope was skipped; no fake PASS.

SCOPE A — HERO SLIDE COMPLETE CONTRACT (1 x TS2740) — FIXED
  Target: app/storefront/storefront-landing-blocks.tsx (LandingHero legacy compat path ~:721).
  Finding: the inline slide of the legacy single-image compat path omitted the six required
  HeroSlideConfig panel fields, even though the canonical no-panel record already exists
  (emptyHeroSlide(), hero-slider-config.ts:253-273) and is duplicated value-for-value by this
  very file's safeSlides fallback (:336-355). The renderer dereferences
  slide.panelMediaAssetId.trim()/panelImageUrl.trim() unconditionally inside the
  variant === "split" (:423-436) and variant === "diagonal" (:526-530) branches, so the legacy
  path crashed with TypeError on those layouts (safeSlides only guards the empty-array case).
  Fix: spread the canonical emptyHeroSlide() before the existing overrides; image, href,
  title/alt/description/ctaLabel and destination metadata are untouched and unchanged.
  No panel artwork, default value or product intent was invented.

SCOPE B — SECTION DISPLAY CONFIG BANNER CONTRACT (8 x TS2339) — FIXED
  Target: lib/storefront-composition/shared-composition-renderer.tsx, renderSharedHomeSection
  (product.sunny|money|cinematic|cinematic-plus|explorer).
  Finding: SectionDisplayConfig is the whitelist contract built only by
  parseSectionDisplayConfig() (composition-api.ts:65-71, 215-231), which maps exactly
  title/subtitle/href/itemCount/sourceKind and drops everything else (contract test
  composition-api.test.ts:78-83). storefront-home.tsx:81 feeds that parsed object straight into
  renderSharedHomeSection, so bannerImageUrl / bannerMediaAssetId / bannerHref could never exist
  there: the reads were always undefined. The banner keys are a real but landing/variant-local
  contract — authored by admin-landing-section-forms.tsx:350-422 and honoured on the landing path
  from the raw record (SharedLandingRenderInput.config: Record<string, unknown>, :88) in the
  duplicated explorer block at :657-698, where ShowcaseProps.bannerImageUrl?/bannerHref? are
  optional (storefront-product-showcase-embla-rails.tsx:381-382, rendered only when
  isBeauty && bannerImageUrl?.trim(): :901, :906, :915-921).
  Fix: removed the provably dead reads and the two prop passes on the Home path only, with the
  rationale documented in-code. Because the values were always undefined, prop={undefined} is
  equivalent to omitting it: rendered output is byte-identical. Adding the keys to
  SectionDisplayConfig/parser was rejected because it would change runtime behaviour (new banners
  on /fa Home), which the task forbids; any/Record<string, any> widening was rejected as
  forbidden. composition-api.ts was NOT modified — evidence showed the correct fix is the narrow
  one at the renderer/call site.

SCOPE C — PREVIEW FAKE BANNER STRUCTURE (1 x TS2741) — FIXED
  Target: shared-composition-renderer.tsx banner.mosaic|grid|three|four-grid|one-large-two-small|
  one-large-four-small|eight-compact.
  Finding: applyPreviewFill<T> unifies the real element type with the createFake return type
  (preview-fill-policy.ts:82-88); PreviewFakeBannerItem (preview-fake-data.ts:146-164) has no alt,
  while the realItems map always emitted alt, so literal inference produced a bespoke element type
  with a required alt. The actual consumer CompositionBannerGrid (storefront-home-blocks.tsx:551)
  reads only src/href/title/objectPosition/previewFake and derives img alt from title (:595).
  Fix: annotated realItems with the true shared consumer element contract (alt optional). No
  global alt-optional weakening, no discriminator weakening, no invented accessibility content,
  provenance (previewFake/previewSource) preserved, zero runtime delta.
  Rejected: deriving alt from the fake's own title (would have changed rendered content and the
  grid ignores alt); editing preview-fake-data.ts (outside the task allowlist).

COMPILER
  before: 10 diagnostics (1 TS2740, 8 TS2339, 1 TS2741), exact keys recorded.
  after:  0 diagnostics; node_modules/.bin/tsc.cmd --noEmit exit code 0 (single final run).
  normalized (file, line, TS code): 10 baseline keys removed, 0 new keys.
  Forbidden techniques: none used.

FOCUSED TESTS — all exit code 0, 129 passing marks, 0 failures
  test:composition-engine (28), test:composition (5, incl. "reads safe keys only"),
  test:home (6), test:storefront (76, incl. storefront-landing guard),
  admin-hero-slider-r13r1.guard (10, incl. legacy slide adaptability + LOCK-SF-367..374).

CRITICAL STOREFRONT SUITE (AGENTS.md rule 17)
  home 6/6, pdp 6/6, listing 6/6, category-plp 1/1, surface-coverage 19/20.
  1 FAILURE, PRE-EXISTING AND UNRELATED:
    lib/storefront-appearance/component-compliance.guard.test.ts
    AssertionError: app/storefront/storefront-landing-blocks.tsx:286 bg-white
    (raw structural colour in the legacy hero pagination bullet bg-white/70).
  Proven pre-existing: with that file restored byte-exactly to HEAD (hash verified identical
  before/after), the same guard fails on the same assertion at :285 — the only difference being
  the +1 line offset from this task's added import. The flagged line is untouched by this task.
  Not opportunistically fixed: outside task scope.
  Evidence: evidence/.../critical-storefront.txt, critical-storefront-preexisting-check.txt.

VISUAL / RUNTIME DELTA
  A: fullscreen (LandingHero default) / shapes / editorial / cinematic -> zero DOM delta.
     split | diagonal + legacy single-image config -> previously a runtime crash, now the
     canonical no-panel default already shipped in this file's safeSlides. Flagged for Architect
     visual acceptance; no redesign, no new artwork, no placeholder.
  B: byte-identical render (removed reads always returned undefined; optional prop omitted ==
     undefined). Landing path that honours banner keys untouched.
  C: byte-identical render (type annotation only).
  Locked Shopeiva Home/PDP DOM/CSS, variant selection, positioning, fallback precedence, media,
  hrefs, locale and sample/store trust isolation: unchanged. No functional visual re-baseline is
  required; the only new-vs-crash surface is Scope A's split/diagonal legacy path.

CHANGED FILES
  src/frontend/app/storefront/storefront-landing-blocks.tsx
  src/frontend/lib/storefront-composition/shared-composition-renderer.tsx
  docs/ai/tasks/TB-FE-STOREFRONT-COMPOSITION-CONTRACT-BATCH-001.task.md
  docs/architecture/evidence/TB-FE-STOREFRONT-COMPOSITION-CONTRACT-BATCH-001/*

ROLLBACK
  git revert <final-sha> (single commit) — no schema, backend, package or tsconfig impact.

Evidence directory:
  docs/architecture/evidence/TB-FE-STOREFRONT-COMPOSITION-CONTRACT-BATCH-001/
    typecheck-before.txt, typecheck-after-tsc.txt (0 bytes), focused-tests.txt,
    critical-storefront.txt, critical-storefront-preexisting-check.txt,
    scope-decisions.md, summary.md
