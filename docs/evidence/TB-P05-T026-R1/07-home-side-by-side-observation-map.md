# 07 — Home side-by-side observation map (TB-P05-T026-R1)

**Home is FROZEN in this Repair Task.** No speculative redesign. User explicitly reports Tooba Home is still visually wrong; Worker does **not** claim global MATCH.

Evidence: `03-original-shopeiva-home-live.png` vs `04-current-tooba-home-live.png` (1440×900 live captures).

| Section | Original Shopeiva structure | Current Tooba structure | Observed difference | Severity | User confirmation required |
|---|---|---|---|---|---|
| Header | Sticky Shopeiva header + mega menu | Storefront shell (T016 continuity) | Accent `#E53935` → `#2563EB` | MINOR | NO |
| Hero slider | Swiper rounded slider, image-only | Live slider bound; geometry largely restored (T018) | User reports remaining visual wrongness — exact delta TBD | **MATERIAL (user-reported)** | **YES** |
| Stories | Horizontal circle rail ~100px | Category/story rail present | Density/spacing may differ | MINOR–MATERIAL | **YES** |
| Categories | Horizontal cards rail ≤20 | HomeCategories rail (not giant grid) | Card sizing/spacing vs original | MATERIAL (historical T018 notes) | **YES** |
| Flash sales | Horizontal discount rail + chrome | Special offers rail | Countdown/chrome differences | MATERIAL | **YES** |
| Best sellers | Multi-column grouped rails | Live catalog-backed sections | Column/rail rhythm | MATERIAL | **YES** |
| Most viewed | Grouped rail by views proxy | Live reviewCount ordering | Ordering proxy differs from Shopeiva views | MINOR | YES |
| Middle banners | 4 linked 21/7 banners | 4 banners present | Hover/link treatment | MINOR | YES |
| Brands | Square tiles → /brands | Square tiles route | Mostly aligned | MINOR | NO |
| New products | Horizontal rail 220px | Horizontal rail | Rail width/spacing | MINOR | YES |
| Testimonials | Inline cards (template) | Omitted (no Content module) | Missing by honest deferral | MATERIAL (deferred) | YES |
| Blog | Inline cards (template) | Omitted (no Content module) | Missing by honest deferral | MATERIAL (deferred) | YES |
| Footer | DynamicFooter 4-col trust | Storefront footer | Largely preserved | MINOR | NO |

## Status

```text
HOME_VISUAL_ACCEPTANCE = AWAITING_USER_VISUAL_FEEDBACK
global MATCH claimed: NO
visual changes made in R1: NONE
```

User should open side-by-side URLs in `02-side-by-side-preview-routes.md` and tell Architect what is visually wrong.
