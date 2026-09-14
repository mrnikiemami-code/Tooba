# TB-P10-T005 — Appearance discovery

| Area | Current source | Hard-coded? | Tokenized? | Store-configurable? | Future migration path |
| --- | --- | --- | --- | --- | --- |
| ThemeProvider | `design-system/theme/ThemeProvider.tsx` | light default | class `dark` on html | no | keep owned provider; do not add next-themes |
| Tailwind darkMode | `tailwind.config.ts` `class` | n/a | yes | no | storefront surfaces still light-first |
| CSS tokens | `globals.css` `--color-*` | default RGB | yes | via SSR vars after T005 | Host appearance projection |
| Brand paint on SF | many `#2563EB` | yes on Home/PDP/checkout | header/card/mini-cart/cart CTA now `primary` | palette key | replace remaining hex only when visually equivalent |
| Status colors | `--color-danger/success/warning` | DS defaults | yes | no (must stay independent) | never from PaletteKey |
| ThemeReference | Tenant context string `theme-alpha` | config | no UI | unused | leave unused; not appearance owner |
| Store settings | Catalog singleton tables | policy/hold/abuse | n/a | yes per DB/store | **extend same family** with `store_appearance_settings` |
| DS token roles | `tokens/roles.ts` | n/a | yes | no | added `primary-strong` |
| Product card | `StorefrontProductCardView` | one layout | brand classes after T005 | no skin | future skin prop only |
| Shopeiva locks | Home/PDP/card geometry | locked | n/a | n/a | defer Home/PDP hex |
| Appearance editor | customer settings omitted theme tab | n/a | n/a | no | do not add placeholder Admin |
| Abandoned | tenant theme editor out of scope (arch 52) | n/a | n/a | n/a | custom theme contract only |

No second settings subsystem. No duplicate ThemeProvider.
