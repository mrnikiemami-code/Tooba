# 13 — Panel i18n proof (TB-P06-T018)

## Scope

Touched panel surfaces in Wave 1 (Customer settings + nav, Seller settings + nav, Admin nav hide). No new translation architecture.

## Verification matrix

| Surface | Persian (fa) | English where catalog supports | RTL (fa) | LTR (en) |
|---|---|---|---|---|
| Customer panel shell / nav | yes | panel strings primarily FA; storefront locale cookie respected | shell `dir="rtl"` baseline | locale cookie preference does not invent parallel panel i18n system |
| Customer settings | yes | profile bridge uses existing profile i18n patterns | forms/icons align with shell | locale preference write uses existing cookie foundation |
| Seller panel shell / nav | yes | existing vendor shell FA | shell RTL baseline | unchanged |
| Seller settings operational | yes | operational labels FA | layout matches vendor shell | unchanged |
| Admin shell (settings hidden) | yes | existing admin FA | shell RTL baseline | unchanged |

## Rules followed

- No duplicate translation architecture.
- Reuse existing storefront locale cookie / T016 foundation for customer locale preference.
- Icons, buttons, nav, mobile drawer inherit existing panel shell behavior (no foreign LTR islands inside RTL shells).

## Non-claims

- Full English string catalog for all panel chrome is still a broader readiness gap (noted since T014).
- Wave 1 did not productize hreflang or locale-prefixed panel routes.
