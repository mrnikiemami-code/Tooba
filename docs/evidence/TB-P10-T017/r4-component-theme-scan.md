# Component theme scan — TB-P10-T017-R4

Scanned Storefront + Customer Panel + shared support/wallet customer surfaces.

| Class | Disposition |
| --- | --- |
| Structural `bg-white` / `bg-gray-50` / `bg-gray-100` | remapped on canvases to derived Card/Media/Interactive; major cards now `bg-surface` |
| `bg-[#…]` structural | replaced with `bg-primary` / `bg-primary/10` on customer ticket/wallet CTAs |
| Status `bg-red-50` / `bg-amber-50` / `bg-blue-50` | legitimate Status |
| Media wells `bg-background` | legitimate Media |
| Decorative circles / story overlays | legitimate Decorative |
| Inline hex backgrounds | none remaining on scanned surfaces |
| Unresolved real structural violations | 0 |

Guard: `component-compliance.guard.test.ts`.
