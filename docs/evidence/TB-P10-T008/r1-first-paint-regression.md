# TB-P10-T008-R1 — First-paint regression

Unchanged ThemeMode contract. Rechecked via runtime A–I:

| Mode | SSR markers |
| --- | --- |
| LightOnly | `data-storefront-color-scheme=light`, no `html.dark` |
| DarkOnly | `scheme=dark` + `class=dark` + blocking bootstrap |
| System | SSR fallback light; bootstrap applies `prefers-color-scheme` |
| UserChoice persisted dark | cookie `tooba-storefront-color-scheme=dark` → dark class |
| UserChoice persisted light | cookie light / missing → light |

One `ThemeProvider`. `--color-primary-on-dark` present on root style. No hydration theme mismatch introduced. No FOUC beyond existing Next overlay.
