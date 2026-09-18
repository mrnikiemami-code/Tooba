# Page Route Binding — TB-P10-T022-R13-R2-R2

Source: Admin page created and published during capture (`runtime-report.json`).

| Field | Value |
|---|---|
| page id | `01a0b5a6-6d33-7000-876e-78575bbe3f04` |
| page type | Landing |
| page title | R13-R2-R2 Exact Route 1789753976219 |
| slug | `r13-r2-r2-exact-route-1789753976219-9b3u1` |
| publication status | Published |
| exact public path | `/landing/r13-r2-r2-exact-route-1789753976219-9b3u1` |
| exact public URL | `http://127.0.0.1:3000/landing/r13-r2-r2-exact-route-1789753976219-9b3u1` |
| admin URL | `http://127.0.0.1:3000/admin/landing-pages/01a0b5a6-6d33-7000-876e-78575bbe3f04` |

## Binding rules applied

- Landing → public `/landing/{slug}` (not Home `/`)
- No evidence/demo page substitute
- No generic home probe when edited page is Landing
- Same exact route used for Sunny and Cinematic flows

## Publish semantics

- SEO title + SEO description filled (publish readiness)
- Product Showcase source = Newest (complete)
- Admin **انتشار** → status Published; public SSR cache invalidated via existing `invalidateFromPage` path
