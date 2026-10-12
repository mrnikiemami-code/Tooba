# TB-FE-STOREFRONT-COMPOSITION-CONTRACT-BATCH-001 — evidence summary

Task-ID: TB-FE-STOREFRONT-COMPOSITION-CONTRACT-BATCH-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main / Worker: tooba-worker-01
Base SHA (expected & actual): 898acdf23151902ee6d5bb3b9f0b643526ef4a6c

## Result per scope

| Scope | Issue | Verdict | Evidence |
|---|---|---|---|
| A | `HeroSlideConfig` complete contract — 1 × TS2740 @ `storefront-landing-blocks.tsx:722` | **FIXED** | `scope-decisions.md` §A |
| B | `SectionDisplayConfig` banner contract — 8 × TS2339 @ `shared-composition-renderer.tsx:244-256` | **FIXED** (dead legacy reads removed) | `scope-decisions.md` §B |
| C | Preview fake banner element typing — 1 × TS2741 @ `shared-composition-renderer.tsx:819` | **FIXED** | `scope-decisions.md` §C |

All three scopes were independently justified; none needed `BLOCKED_ARCHITECT_DECISION`.

## Compiler

- before: 10 diagnostics (`typecheck-before.txt`) — 1 TS2740, 8 TS2339, 1 TS2741.
- after: **0 diagnostics** (`typecheck-after-tsc.txt`, 0 bytes; `node_modules/.bin/tsc.cmd
  --noEmit` exit code 0).
- single final run after the last edit (no second run needed; no masked diagnostic surfaced).
- normalized (file, line, TS code) comparison: all 10 baseline keys removed, **0 new keys**.
- forbidden techniques used: none (no `any`, no cast/assertion, no `@ts-ignore`, no tsconfig or
  exclusion change, no guard disabled, no package change).

## Focused tests (exit code 0 on every command)

`focused-tests.txt` — 129 passing assertions/marks, 0 failures:

- `npm run test:composition-engine` (composition-registry / shared-composition-renderer /
  industry-templates / completeness-matrix guards) — 28 pass / 0 fail.
- `npm run test:composition` (`composition-api.test.ts`, incl. "reads safe keys only") — 5/0.
- `npm run test:home` (`storefront-home.test.ts`, `home-structure.guard.test.ts`) — 6/0.
- `npm run test:storefront` (incl. `storefront-landing.guard.test.ts`) — 76/0.
- `node --test app/admin/landing-pages/admin-hero-slider-r13r1.guard.test.ts` — 10/0
  (incl. "legacy slide seo* and href remain adaptable", "LOCK-SF-367…374 present").

## Critical storefront suite (mandatory, AGENTS.md rule 17)

`critical-storefront.txt` — `npm run test:critical-storefront`, 4 of 5 suites green
(home 6/6, pdp 6/6, listing 6/6, category-plp 1/1), then:

- `lib/storefront-appearance/component-compliance.guard.test.ts`
  → **1 FAIL, PRE-EXISTING and unrelated**:
  `AssertionError: app/storefront/storefront-landing-blocks.tsx:286 bg-white`
  (raw structural colour in the legacy hero pagination bullet `bg-white/70`).

**Proven pre-existing** (`critical-storefront-preexisting-check.txt`): with
`storefront-landing-blocks.tsx` restored byte-exactly to HEAD (working file backed up and
restored, `Get-FileHash` identical before/after), the same guard fails with the *same*
assertion at **:285** — the only difference is the +1 line offset from this task's added import.
The flagged line is untouched by this task (`git diff` proves it) and is not part of Scopes A/B/C.
Relevance to this task: **none**; it is a separate raw-colour finding outside this task's scope
and was not opportunistically "fixed".

## Visual / runtime delta

- Scope A: `fullscreen` (LandingHero default), `shapes`, `editorial`, `cinematic` → **zero DOM
  delta** (these branches never read panel fields; `DiamondSlideFace` reads only image/title/alt/
  description/CTA). `split` / `diagonal` + legacy single-image config → previously a runtime
  `TypeError` (crash), now the canonical no-panel default already shipped in this file's
  `safeSlides`. Flagged for Architect visual acceptance; no new artwork/placeholder.
- Scope B: **byte-identical render** — the removed reads always yielded `undefined`, and passing
  `bannerImageUrl={undefined}` is equivalent to omitting the optional prop. The landing path that
  genuinely honours banner keys (`shared-composition-renderer.tsx:657-698`) is untouched.
- Scope C: **byte-identical render** — type annotation only; fakes keep no `alt`, real items keep
  the authored `alt`, `previewFake`/`previewSource` provenance intact.
- Locked Shopeiva Home/PDP DOM/CSS, variant selection, positioning, fallback precedence, media,
  hrefs, locale and sample/store trust isolation: unchanged.

## Changed files

1. `src/frontend/app/storefront/storefront-landing-blocks.tsx` (+7 lines: `emptyHeroSlide` import,
   `...emptyHeroSlide()` spread + rationale comment).
2. `src/frontend/lib/storefront-composition/shared-composition-renderer.tsx` (dead banner reads
   removed on the Home path; `realItems` annotated with the real consumer contract).

No other production path was modified; `composition-api.ts` was **not** touched (evidence showed
the narrow fix belonged in the renderer/legacy call site).

## Rollback

`git revert <final-sha>` (single commit), or reverse the two hunks — the change is comment +
spread + type-annotation + deletion of provably dead reads, with no schema, backend, package or
tsconfig impact.
