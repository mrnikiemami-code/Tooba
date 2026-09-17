# recovery-start — TB-P10-T022-R13-R1

## Preflight

| Item | Value |
|------|-------|
| branch | `main` |
| HEAD | `b462889c6c8bd7907ae1468f66a3acc4b0f15723` |
| origin/main | `b462889c6c8bd7907ae1468f66a3acc4b0f15723` |
| HEAD == origin/main | YES |
| Prior pin (task) | `3d694233047708434b020750be6729500a8d1a3a` |
| Divergence | Clean descendant: Home-reset + Admin audit docs + R13 HeroSlider multi-slide commit (`b462889c`) preserved |
| Unrelated `.tmp-*` | Present; left untouched |
| Stashes | Not mutated |

## Current HeroSlider / registry snapshot (start)

- Six registry variants: `hero.fullscreen|shapes|diagonal|cinematic|split|editorial`
- Persian design names via `variant-design-names.ts` (الماس…عقیق)
- Single `HeroSlider` + Swiper in `storefront-landing-blocks.tsx`
- Gaps at start: raw `displayHeightPx`, fake slide SEO fields, free-text href, bottom-only validation

## Wizard steps

Hero: type → variant → settings → preview (no source step).

## Decision

`RECOVERY_OK` — continue; no unexplained conflicting source divergence.
