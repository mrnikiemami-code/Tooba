# TB-P10-T008-R1 — Dark contrast repair

Canonical per-palette `primaryOnDarkRgb` on FE + Host registries. `appearanceCssVars` emits `--color-primary-on-dark`. CTA `--color-primary` / `--color-primary-foreground` unchanged.

`.dark { --color-brand-emphasis: var(--color-primary-on-dark); }` and `html.dark .text-primary` (+ hover/group-hover) use brand-emphasis. Focus outline on dark uses `--color-primary-on-dark`. No page-local wine-burgundy branch.

| Palette | primaryOnDarkRgb | Contrast vs `12 12 14` | CTA primary/on-primary |
| --- | --- | --- | --- |
| tooba-blue | `59 115 237` | 4.52 | 4.5+ |
| forest-green | `42 139 78` | 4.56 | 4.5+ |
| wine-burgundy | `189 91 118` | 4.58 | 4.5+ |
| slate-navy | `104 123 148` | 4.51 | 4.5+ |
| amber-gold | `187 98 31` | 4.54 | 4.5+ |
| teal-lagoon | `46 136 129` | 4.61 | 4.5+ |
| violet-royal | `144 88 240` | 4.50 | 4.5+ |

LOCK-SF-158 records the invariant.
