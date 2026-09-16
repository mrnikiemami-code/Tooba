# Admin Theme Isolation — TB-P11-T001

| Surface | Isolation | Evidence |
| --- | --- | --- |
| Admin chrome | `data-panel-theme="admin"`; fixed admin primary | `app/admin/layout.tsx`, `globals.css` `[data-panel-theme="admin"]` |
| Seller chrome | `data-panel-theme="seller"` | `app/vendor-panel/layout.tsx` + `seller-theme-isolation.guard.test.ts` |
| Root html | Storefront appearance **not** applied as root style | `admin-theme-isolation.guard.test.ts` |
| Allowed storefront tokens | Only inside themed canvas / appearance **preview** | `storefront-themed-canvas.tsx`; `admin-appearance-settings.tsx` preview boundary |
| Landing preview | Preview may set storefront scope for SSR/preview | must stay preview-scoped |

**Verdict:** Guards present. Residual risk: accidental `--color-*` storefront vars outside preview. **LOCK-SF-364** encodes chrome isolation for future Admin work.
