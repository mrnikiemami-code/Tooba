# TB-P10-T005 — Runtime smoke

Host `:5088`, FE `:3000`.

| Surface | Result |
| --- | --- |
| GET `/v1/storefront/appearance` | 404 on current Host process (not recycled). FE uses identical `tooba-blue` fallback. |
| `/fa` Home | 200, `data-storefront-palette=tooba-blue`, accent present, no `#E53935` |
| `/fa/products` PLP | 200, palette=tooba-blue, no `#E53935` |
| `/fa/cart` | 200, palette=tooba-blue, no `#E53935` |
| `/fa/shipping` | 200, palette=tooba-blue; `#E53935` is **pre-existing** shipping chrome (deferred, not introduced) |
| theme flash | none — `:root` RGB equals fallback projection |
| T004 account/cart/checkout behavior | not redesigned |

USER_VISUAL_ACCEPTED remains NO.
