# TB-P10-T017-R2 — Media root cause

Classification: **A. Actual runtime rendering defect**

Measured on live PLP (`1440×1200`) before repair (`r2-probe.json`):

| Field | Value |
| --- | --- |
| `<img>` | present, `complete=true`, `naturalWidth=150` |
| src | Host `/v1/storefront/media/{seeded-id}` (Tooba placeholder, not missing `aaaaaaaa-…`) |
| well class | `relative aspect-[4/5] bg-background overflow-hidden` |
| computed `aspect-ratio` | `auto` |
| well `clientHeight` | **0** |
| img layout | `position:absolute; inset:0` so the well collapsed |

Cause: Tailwind `content` was only `./app/**` and `./design-system/**`. `aspect-[4/5]` lived in `lib/storefront-appearance/product-card-skin.ts` and was never emitted. Not a capture/viewport issue, not missing seed media, not a blank transparent asset.

Not B/C/D.
