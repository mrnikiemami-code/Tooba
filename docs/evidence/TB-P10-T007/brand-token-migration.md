# TB-P10-T007 — Brand token migration

Migrated Storefront/customer/payment/blog brand hexes to `bg-primary` / `text-primary` / `from-primary` / `rgb(var(--color-primary))`.

Shipping `#E53935` brand chrome → primary. Validation stays `text-red-600` / `bg-red-50`.

`STOREFRONT_ACCENT` is now `rgb(var(--color-primary))`. Soft alpha concatenations replaced with `/ 0.1` `/ 0.2`.

amber-gold tokens darkened for WCAG AA (`180 83 9` / `146 64 14`) on both FE and Host registries.

Canonical loader remains `loadStorefrontAppearance` in layout only.
