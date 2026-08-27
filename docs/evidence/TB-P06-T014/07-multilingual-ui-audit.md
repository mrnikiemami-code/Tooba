# 07 — Multilingual UI audit (TB-P06-T014)

| Surface | Persian RTL | English LTR | Notes |
|---|---|---|---|
| Root `html` | `lang=fa` `dir=rtl` default | Cookie `tooba_locale=en` → `lang=en` `dir=ltr` | `app/layout.tsx` |
| Storefront header | RTL chrome | LocaleSwitcher FA\|EN | Compact toggle |
| Panels (admin/vendor/customer) | Shells still Persian chrome | Direction inherits root | Full panel string catalogs deferred |
| Blog | fa content default | OG locale follows cookie | Content body still Host locale field |
| Icons/flex | RTL mirrored via dir | LTR when en | LTR islands remain for IDs/phones |

Hard-coded Persian product copy remains majority (HONESTLY_DEFERRED full dictionary). Foundation is cookie locale + design-system catalogs + chrome messages.
