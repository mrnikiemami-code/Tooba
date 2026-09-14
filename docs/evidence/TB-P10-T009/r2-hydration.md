# TB-P10-T009-R2 — Hydration

Isolated Admin Appearance and Home: 0 hydration warnings.

Full A–H capture recorded 1 generic Next.js "attributes didn't match" warning without product-card-skin / palette / appearance attribute names. `html` already has `suppressHydrationWarning`. LocaleProvider writes `lang`/`dir` after mount (T008). Skin SSR marker stays `data-storefront-product-card-skin` from layout and matched requested skin in capture steps.
