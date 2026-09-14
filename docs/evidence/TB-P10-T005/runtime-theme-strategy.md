# TB-P10-T005 — Runtime theme strategy

| Step | Detail |
| --- | --- |
| source | `catalog.store_appearance_settings` singleton; missing row = default `tooba-blue` / Light |
| projection | `StoreAppearanceProjector.GetEffectiveAsync` |
| cache key | `store-appearance:{tenant:id\|marketplace:connection}` |
| invalidation | `Invalidate(context)` on future write; 5-minute absolute expiry |
| SSR | `app/layout.tsx` fetches Host `/v1/storefront/appearance` and sets html `style` + `data-storefront-palette` |
| hydration | same default CSS in `:root`; failed fetch uses identical `tooba-blue` → no FOUC |
| not used | per-card query, client-only theme first paint, per-store CSS bundle, Tailwind class names from DB |

Dark `ThemeMode` is stored but not applied to storefront chrome (surfaces are light-locked). Documented gap; applying `html.dark` now would break visuals.
