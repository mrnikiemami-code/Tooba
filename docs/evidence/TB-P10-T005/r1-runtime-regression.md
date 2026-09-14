# TB-P10-T005-R1 — Runtime regression

Host `:5088` + FE `:3000`.

| Surface | Status | palette | primary | notes |
| --- | --- | --- | --- | --- |
| `/fa` Home | 200 | tooba-blue | 37 99 235 | no `#E53935` |
| `/fa/products` PLP | 200 | tooba-blue | 37 99 235 | no `#E53935` |
| `/fa/products/demo-prod-av-audio-headphones-3` PDP | 200 | tooba-blue | 37 99 235 | no `#E53935` |
| `/fa/cart` | 200 | tooba-blue | 37 99 235 | no `#E53935` |
| `/fa/shipping` | 200 | tooba-blue | 37 99 235 | pre-existing `#E53935` chrome only |

R4 guards still green: identity session cache, one merge per login, cart UI/account menu, checkout conversion (`test:storefront` 67/67). Shipping page functions. No appearance-introduced console contract change. No visual redesign.
