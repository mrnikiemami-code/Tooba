# TB-P04-T005 visual review index

Functional PASS is not Visual ACCEPT. Cursor PASS is not Architect ACCEPT.

Live evidence is under `docs/evidence/TB-P04-T005/live/`. Captured from Next `http://127.0.0.1:3012` rewriting `/v1/*` to Host `http://127.0.0.1:5088`. Tenant `store-alpha` (Hosts include `localhost` / `127.0.0.1`). Seed rows were written through module directories on Host startup, then read back over HTTP. Admin routes did not substitute fixture JSON.

| file | route | viewport | theme | direction | tenant/context | live API | business state | proves |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| live/live-01-list-desktop-rtl.png | `/admin/products` | desktop ~1440 | light | rtl | store-alpha | yes | Published product, 1 variant, 2 offers; banner Host ترکیب‌شده | list consumes composed Host HTTP, not fixture |
| live/live-02-overview-desktop-rtl.png | `/admin/products/01a030d1-4056-7000-baf1-99951569bc6b` | desktop | light | rtl | store-alpha | yes | Catalog Published; Price/Stock not on Product; Sellers 2 | identity overview; UI boundary != module boundary |
| live/live-03-variants.png | same workspace | desktop | light | rtl | store-alpha | yes | one variant fingerprint, offers 2 | one Variant != one Seller |
| live/live-04-commercial-two-sellers.png | same workspace, Commercial | desktop | light | rtl | store-alpha | yes | LIVE-A 1850000 IRR and LIVE-B 1790000 IRR, tax class standard | Offer != Price; two sellers |
| live/live-05-inventory-locations.png | same workspace, Inventory | desktop | light | rtl | store-alpha | yes | WH-THR reserved 3 / WH-ISF / WH-KSH on two offers | Inventory offer-scoped, multi-location |
| live/live-06-seo-content.png | same workspace, SEO & Content | desktop | light | rtl | store-alpha | yes | slug workspace-live-shirt; Semantic Content != Page Composition | SEO seam read-only |
| live/live-07-workspace-mobile.png | same workspace, forceNarrow | mobile ~390 | light | rtl | store-alpha | yes | compact shell; Host source | mobile workspace |
| live/live-08-workspace-ltr.png | same workspace | desktop | light | ltr | store-alpha | yes | LTR chrome with live Host source | LTR |
| live/live-09-workspace-dark.png | same workspace | desktop | dark | ltr | store-alpha | yes | dark theme; Host source | dark |
| live/live-10-conflict-or-readonly.png | same workspace | desktop | dark | ltr | store-alpha | yes | banner `workspace.catalog.stale` after Stale save against Host | live optimistic concurrency (HTTP 409), not a static demo |

Also verified: `?scope=view` disables Save/Publish (`منبع: Host · scope= view`). Native unsaved dialog remains DEFERRED_NON_BLOCKING.

Cursor PASS != Architect visual ACCEPT. TB-P04-T006 is not issued.
