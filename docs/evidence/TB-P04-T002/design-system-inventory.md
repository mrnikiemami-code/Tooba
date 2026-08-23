# TB-P04-T002 — Design System inventory

Owned root: `src/frontend/design-system/`

| Area | Path | Notes |
| --- | --- | --- |
| Tokens | `app/globals.css`, `tokens/roles.ts` | Semantic CSS variables; reference layer isolated |
| Tailwind map | `app` sibling `tailwind.config.ts` | class darkMode; no Tailwind 4 |
| Theme | `theme/types.ts`, `theme/ThemeProvider.tsx` | light/dark + rtl/ltr on html |
| Core | `primitives/core.tsx` | Button through Field |
| Overlays | `primitives/overlays.tsx` | Dialog native; Drawer `start-0` |
| Commerce view | `primitives/commerce.tsx` | No pricing/inventory calls |
| Invariants | `invariants.ts` | money schema, drawer start, icon label |
| Showcase | `app/design-system/` | `robots: noindex`; not visual ACCEPT |
| Providers | `app/providers.tsx` | ThemeProvider only |

Not in this task: Data Grid, workspaces, storefront redesign, admin/seller/customer apps.
