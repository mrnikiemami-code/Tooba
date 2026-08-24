# TB-P04-T007 Repair — Layout measurements

Live measurements from Tooba runtime (`http://localhost:3000`) after Host `/v1/storefront/*` bind. `nextjs-portal` removed before capture.

Desktop target CSS viewport: **1440×900** via `Emulation.setDeviceMetricsOverride` (`mobile: false`, `deviceScaleFactor: 1`).

| Route | `window.innerWidth` × `innerHeight` | `documentElement.clientWidth` | `main` width | inner shell width | content/viewport | unexplained blank | overflow-x |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Home `/` | 1440×900 | 1425 (scrollbar) | 1425 | 1425 | 1.00 of clientWidth | none | no (`scrollWidth` 1425) |
| Listing `/products` | 1440×900 | 1425 | 1425 | 1425 | 1.00 | none | no |
| PDP `/products/workspace-live-shirt` | 1440×900 | 1425 | 1425 | 1425 | 1.00 | none | no |

Shell: `max-w-[1800px] mx-auto px-4 sm:px-6`. Main visual canvas occupies the full CSS viewport minus scrollbar; it is not a 200–700px centered strip.

Mobile target CSS viewport: **390×844** (`mobile: true`).

| Route | CSS viewport | PNG IHDR | overflow-x |
| --- | --- | --- | --- |
| Home | 390×844 | 390×844 | no |
| Listing | 390×844 | 390×844 | no |
| PDP | 390×844 | 390×844 | no |

Desktop PNG IHDR (after recapture): Home/Listing/PDP/mega/footer/cards/other-sellers = 1440×900 except where noted in `screenshots.md`.
