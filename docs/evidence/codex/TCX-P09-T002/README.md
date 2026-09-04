# TCX-P09-T002 — isolated development database and storefront publication

## Result

PASS. The primary development database was identified from the running primary host's live PostgreSQL sessions as `tooba_alpha`, backed up read-only, restored into `tooba_codex_dev`, and used only by the isolated Codex runtime.

Three copied, publish-ready Catalog products were made storefront-visible through the existing application contracts. Representative examples:

- Product: `ایرباد نسخه 3`
- Product ID: `01a0538a-22f7-7000-b0c4-d3970c92e972`
- Slug: `demo-prod-digital-gadgets-wearables-earbuds-3`
- Variant ID: `01a0538a-2449-7000-87e1-0a123d5fb7a8`
- Offer ID: `01a06adf-9541-7000-ad4d-8bf5cfa17b3f`
- Price: `14,990,000 IRR`
- Initial available units: `25` (24 after the one-unit cart verification)
- Second PDP: `demo-prod-digital-gadgets-wearables-earbuds-4` (`8,990,000 IRR`, 18 units)
- Variant PDP: `demo-prod-digital-gadgets-wearables-fitness-bands-3` (`11,990,000 IRR`, 12 units)

No direct SQL mutation was used for publication or commerce setup. The admin publication and variant lifecycle endpoints, followed by the seller offer, pricing, and inventory endpoints, performed all writes.

## Database clone

- Definitive primary product database: `tooba_alpha`
- Identification: the primary backend process on port 5088 had live sessions to the host PostgreSQL service on port 5432; `pg_stat_activity` named `tooba_alpha` for its tenant product data and `tooba_messaging` for messaging. Repository tenant configuration independently maps `tenant-alpha` to `tooba_alpha`.
- Source and target were confirmed different before restore.
- Read-only backup artifact (outside Git): `D:\Users\User\source\repos_tooba-codex-backups\tooba_alpha-20260904-084346.dump`
- Backup format: native PostgreSQL custom format, schema and data, no owner/privilege replay.
- Backup size: 1,254,038 bytes.
- Target: `tooba_codex_dev` on the same host PostgreSQL service.
- Only target sessions were eligible for termination; there were zero at recreation time.
- Restore completed with `--exit-on-error`.

Post-restore representative counts:

- Catalog products: 284
- Catalog variants: 472
- Catalog media references: 1,415
- Catalog product-category assignments: 340
- Offer rows: 106
- Pricing rows: 106
- Inventory positions: 108

The restored migration history matched the Codex branch. Host startup reported that the database was already current; no downgrade, schema reconciliation, or feature migration was needed.

## Publication decision and actions

The clone initially exposed zero storefront products. Catalog contained 283 Draft products and one Archived product, while the copied Offer rows referenced obsolete variant IDs that did not exist in the current Catalog set. This was verified by reading each owning module separately and comparing IDs outside PostgreSQL; no cross-schema SQL join or write was used.

The selected copied product passed every built-in publication readiness check: category, translations, attributes, variant, media, and SEO. The supported APIs then performed these actions:

1. Publish the existing product through `/v1/admin/products/{productId}/publish`.
2. Publish one existing variant through the product workspace variant endpoint.
3. Create an Active marketplace offer through `/v1/seller/offers` under the existing cloned seller context.
4. Write the IRR/IR price through the seller pricing endpoint.
5. Set on-hand inventory through the seller inventory endpoint.

The public storefront API subsequently returned exactly three sellable products with copied titles, categories, media references, seller context, real module-owned prices, and inventory. A copied simple product with no variant was also published, but correctly remained non-sellable because the current commerce model requires a variant-backed Offer; no invalid Offer was fabricated. A real guest cart was created through the storefront API and accepted one unit of the first offer at the expected subtotal, reducing its available count from 25 to 24 under the existing cart semantics.

## Runtime and routes

- Backend: `http://127.0.0.1:5188` (health returned HTTP 200)
- Frontend: `http://127.0.0.1:3100`
- Home: `http://127.0.0.1:3100/` — HTTP 200 and contains the published product
- Listing: `http://127.0.0.1:3100/products` — HTTP 200 and contains the published product
- PDP: `http://127.0.0.1:3100/products/demo-prod-digital-gadgets-wearables-earbuds-3` — HTTP 200 and contains the product title
- Variant PDP: `http://127.0.0.1:3100/products/demo-prod-digital-gadgets-wearables-fitness-bands-3` — HTTP 200 and contains the product title
- Cart: `http://127.0.0.1:3100/cart` — HTTP 200; the backend cart flow accepted the published offer

The isolated backend is process `556`; the frontend launcher is process `25276`. Primary ports 5088, 3000, and 3001 and their processes were not stopped or modified.

## Source-control boundary

- Worktree: `D:\Users\User\source\repos\SarvNewVer-Codex`
- Branch: `codex/p09`
- Pre-task HEAD: `55243ef5bad8abbabb0855f64a20ee8fdd229b93`
- Observed `origin/main`: `a0f615a4360cc0407e06c056778a8057bd5bdb3e`
- Divergence before evidence commit: Codex branch had one unique evidence commit and was two commits behind `origin/main` (`1 2`). The task's no-merge/no-rebase rule was followed.
- Feature-code changes: none
- P08 Content: untouched

No secrets or guest-cart credentials are recorded in this evidence.
