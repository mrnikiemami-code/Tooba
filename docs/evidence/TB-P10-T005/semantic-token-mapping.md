# TB-P10-T005 — Semantic token mapping

| Role | CSS variable | Default (tooba-blue / light) | Store-overridable? |
| --- | --- | --- | --- |
| primary | `--color-primary` | 37 99 235 (`#2563EB`) | yes (palette) |
| primary-hover / strong | `--color-primary-strong` | 29 78 216 (`#1d4ed8`) | yes (palette) |
| on-primary | `--color-primary-foreground` | 255 255 255 | yes (palette) |
| focus | `--color-focus` | 37 99 235 | yes (palette) |
| surface | `--color-surface` | 255 255 255 | no in T005 |
| surface-elevated | `--color-surface-elevated` | 255 255 255 | no in T005 |
| background | `--color-background` | 250 250 249 | no in T005 |
| text | `--color-foreground` | 24 24 27 | no in T005 |
| text-muted | `--color-muted` | 113 113 122 | no in T005 |
| border | `--color-border` | 228 228 231 | no in T005 |
| danger | `--color-danger` | 185 28 28 | **never** |
| success | `--color-success` | 21 128 61 | **never** |
| warning | `--color-warning` | 180 83 9 | **never** |

Migrated to tokens this task: header, product card, mini-cart, cart checkout CTA, `storefront.css` already used `--color-primary`.

Deferred (visual lock): Home rails, PDP, shipping/payment remaining hex, dirty local account-menu UX file.
