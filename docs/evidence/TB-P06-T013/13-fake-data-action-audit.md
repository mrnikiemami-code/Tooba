# 13 — Fake data / action audit (TB-P06-T013)

Task: `TB-P06-T013`

## Removed / fixed

| Item | Action |
|---|---|
| Home article heart/like toggle (local React state, no backend) | **Removed** from `HomeArticlesSection` article cards |

## Honestly deferred (not ported)

| Shopeiva behavior | Tooba stance |
|---|---|
| Blog likes | Not ported — no fake counter or toggle on `/blogs` or detail |
| Blog views | Not ported — no inflated view metrics |
| Demo static blog posts when API empty | Not injected — empty/loading/error shown honestly |

## Still present (out of Content scope)

- Product/wishlist heart controls on other home rails (separate commerce surfaces; not claimed as Content engagement).
- Dev seed articles in Content module are explicit development seed, not runtime fake UI fabrication.

## Audit flag

`FAKE_DATA_ACTION_AUDIT = AUDITED`
