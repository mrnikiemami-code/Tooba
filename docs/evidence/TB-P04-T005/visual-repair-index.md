# TB-P04-T005 visual repair index

Functional PASS is not Visual ACCEPT. Cursor PASS is not Architect ACCEPT.

Live evidence is under `docs/evidence/TB-P04-T005/visual-repair/`. Captured from Next `http://127.0.0.1:3012` rewriting `/v1/*` to Host `http://127.0.0.1:5088`. Tenant `store-alpha`. Admin routes did not substitute fixture JSON.

| file | route | viewport | theme | direction | live API | proves |
| --- | --- | --- | --- | --- | --- | --- |
| visual-repair/01-admin-products-list-desktop-rtl.png | `/admin/products` | desktop ~1440 | light | rtl | yes | Admin shell, Persian grid headers, Host list, no UUID row labels |
| visual-repair/02-overview-desktop-rtl.png | `/admin/products/01a030d1-4056-7000-baf1-99951569bc6b` | desktop | light | rtl | yes | Overview: Catalog identity, inventory health, no debug toolbar |
| visual-repair/03-variants.png | same workspace | desktop | light | rtl | yes | One variant, offer count 2 |
| visual-repair/04-commercial-two-sellers.png | Commercial | desktop | light | rtl | yes | Seller display names, two IRR prices, tax class |
| visual-repair/05-inventory-locations.png | Inventory | desktop | light | rtl | yes | Location names, offer-scoped stock |
| visual-repair/06-seo-content.png | SEO | desktop | light | rtl | yes | Semantic content vs page composition |
| visual-repair/07-publication.png | Publication | desktop | light | rtl | yes | Readiness checklist |
| visual-repair/08-workspace-mobile-rtl.png | same workspace | ~390 | light | rtl | yes | Narrow shell |
| visual-repair/09-workspace-ltr.png | same workspace | desktop | light | ltr | yes | LTR via Admin appearance control |
| visual-repair/10-workspace-dark.png | same workspace | desktop | dark | ltr | yes | Dark tokens |
| visual-repair/11-live-http-409.png | same workspace | desktop | dark | ltr | yes | Optimistic concurrency after a real Host PATCH (HTTP 409), not a debug Stale-save control |

Contact sheet: `visual-repair/architect-visual-contact-sheet.png`.

TB-P04-T006 is not issued.
