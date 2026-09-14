# Background discovery — TB-P10-T015

Light semantic tokens in `globals.css`: `--color-background` 250 250 249, `--color-surface` / `--color-surface-elevated` 255 255 255.

Dark: `--color-background` 12 12 14, `--color-surface` 28 28 32, `--color-surface-elevated` 44 44 50.

Root `html, body` used `--color-background`. Storefront canvas was hardcoded `bg-[#f3f5f8]` on `StorefrontShell` (243 245 248), which is why palettes did not change the page wash.

Palettes previously held brand tokens only (primary / strong / on-primary / focus / primaryOnDark). Tint cannot reuse primary-alpha.

Tintable: StorefrontShell page canvas (`--color-page-background`, default Neutral `#f3f5f8`).

Must stay elevated/neutral: product cards, inputs, header/menu chrome, mini-cart, modals, status colors, `--color-surface` / `--color-surface-elevated`.
