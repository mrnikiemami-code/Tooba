# TB-P04-T005 visual repair round 2 index

Functional PASS is not Visual ACCEPT. Cursor PASS is not Architect ACCEPT.

Live evidence is under `docs/evidence/TB-P04-T005/visual-repair-2/`. Captured from Next `http://127.0.0.1:3012` rewriting `/v1/*` to Host `http://127.0.0.1:5088`. Tenant `store-alpha`. Admin routes did not substitute fixture JSON.

Product under review: `01a030d1-4056-7000-baf1-99951569bc6b` (`Live Workspace Shirt`).

ImageMagick was not available on this machine, so Architect review uses the eleven named shots rather than a generated contact-sheet PNG.

| file | route | viewport | theme | direction | live API | proves |
| --- | --- | --- | --- | --- | --- | --- |
| visual-repair-2/01-admin-products-list-desktop-rtl.png | `/admin/products` | desktop | light | rtl | yes | Wider shell, operational DataGrid columns (category, offer amount range, sellable units, locations, updated, open) |
| visual-repair-2/02-overview-desktop-rtl.png | workspace | desktop | light | rtl | yes | Denser metric strip, Catalog vs inventory health, no debug chrome |
| visual-repair-2/03-variants.png | Variants | desktop | light | rtl | yes | Variant row with offer count |
| visual-repair-2/04-commercial-two-sellers.png | Sales & price | desktop | light | rtl | yes | Two seller offers, SKU, channel, status, tax-exclusive amount |
| visual-repair-2/05-inventory-locations.png | Inventory | desktop | light | rtl | yes | Health strip + Tehran/Isfahan/seller locations |
| visual-repair-2/06-seo-content.png | SEO | desktop | light | rtl | yes | Readiness, slug seam, composition note |
| visual-repair-2/07-publication.png | Publication | desktop | light | rtl | yes | Grouped content/sales/inventory-SEO checks |
| visual-repair-2/08-workspace-mobile-rtl.png | workspace | ~narrow | dark | rtl | yes | Section switcher / stacked ops chrome |
| visual-repair-2/09-workspace-ltr.png | workspace | desktop | mixed | ltr | yes | Appearance LTR control |
| visual-repair-2/10-workspace-dark.png | workspace | desktop | dark | rtl | yes | Dark tokens on workspace |
| visual-repair-2/11-live-http-409.png | workspace | desktop | light | rtl | yes | Concurrent Host PATCH then UI Save → operator 409 banner (reload / review / discard) |

TB-P04-T006 is not issued.
