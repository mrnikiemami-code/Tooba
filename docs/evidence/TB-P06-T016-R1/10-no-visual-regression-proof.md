# 10 — No visual regression proof (TB-P06-T016-R1)

## Contract

TB-P06-T016 / T016-R1 are **routing + SEO + acceptance evidence**. Forbidden without explicit Architect scope:

- CSS / Tailwind geometry edits for “locale look”
- JS behavior / carousel / animation / transition changes
- Hover / focus / active restyles
- Spacing / typography / responsive redesign
- Shopeiva structure redesign

Functional PASS ≠ Visual ACCEPT. Critical storefront remains Shopeiva-locked.

## Diff nature (R1)

| Change | Visual impact |
|---|---|
| Evidence + acceptance probe script | None |
| `LocaleProvider` sync of `documentElement.lang` / `dir` from URL | Direction/lang only (already SSR-correct); no chrome redesign |
| Cleared stale `.next` + FE restart (zod vendor-chunk 500) | Runtime recovery only |

No Shopeiva template CSS edits for this Repair.

## Capture set (side-by-side review material)

| Capture | Surface |
|---|---|
| `captures/01-fa-home-desktop.png` | Tooba Persian Home |
| `captures/02-en-home-desktop.png` | Tooba English Home |
| `captures/03-fa-pdp-desktop.png` | Tooba Persian PDP (`demo-game-3`) |
| `captures/04-en-pdp-desktop.png` | Tooba English PDP |
| `captures/05-fa-blogs-desktop.png` | Tooba Persian Blog listing |
| `captures/06-en-blogs-desktop.png` | Tooba English Blog listing |
| `captures/07-fa-article-desktop.png` | Tooba Persian Article |
| `captures/08-shopeiva-home-desktop.png` | **Original Shopeiva Home** reference |
| `captures/09-fa-home-mobile.png` | Tooba Persian Home mobile |
| `captures/10-en-home-mobile.png` | Tooba English Home mobile |

## Critical surfaces

| Surface | Expected |
|---|---|
| Home | Same section composition / chrome; only URL prefix + lang/dir |
| PDP | Same gallery / purchase chrome; locale URL only |
| Blog / Article | Same listing/detail chrome |
| Shopeiva `:3001` | Unchanged reference baseline (200) |

## Guardrails

- `VISUAL_CONTRACT = SHOPEIVA_LOCKED`
- `NEW_UI_RULE = SOURCE_DERIVED_NATIVE_FIT`
- Critical-storefront tests remain the automated visual/functional guard where applicable; this Task does not claim Architect Visual ACCEPT.

## Verdict

No intentional visual regression introduced. Captures 01–10 provide audit material. Routing/SEO-only scope held.
