# 09 — Seller product Shopeiva map (TB-P06-T021)

Task: visual/structural map of Shopeiva Vendor product management vs Tooba `/vendor-panel/products*`.  
Scope: routes, components, list/create/edit, media, price, stock.  
Constraint: **do NOT redesign advanced variants** — document only what exists today.

Shopeiva root: `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva`  
Tooba FE: `D:\Users\User\source\repos\SarvNewVer\src\frontend`

---

## 1. Shopeiva route inventory (exact)

| Route | Page file | Component | Role |
|---|---|---|---|
| `/vendor-panel/products` | `src/app/(vendor)/vendor-panel/products/page.jsx` | `ProductsList` | List / search / filter / paginate / delete modal |
| `/vendor-panel/products/new` | `src/app/(vendor)/vendor-panel/products/new/page.jsx` | `ProductForm` (create mode) | Full product create form |
| `/vendor-panel/products/[id]/edit` | `src/app/(vendor)/vendor-panel/products/[id]/edit/page.jsx` | `ProductForm` (edit mode via `params.id`) | Full product edit form |

Barrel: `src/components/vendor/panel/products/index.js` exports `ProductsList`, `ProductForm`.

Nav entry: `src/app/(vendor)/vendor-panel/layout.jsx` → `{ id: 'products', href: '/vendor-panel/products' }`.

### No separate media / price / stock routes

Shopeiva does **not** split media, price, or stock into dedicated URLs. All live as **sections inside one `ProductForm`** on create/edit:

| Concern | Where in Shopeiva | UI pattern |
|---|---|---|
| Price | `ProductForm` section «قیمت و موجودی» | `price` (required), `oldPrice` (optional) text inputs |
| Stock | same section | `stock` (required) text input |
| Media — main | section «عکس اصلی» | `<input type="file" accept="image/*">` → FileReader data URL into `image` |
| Media — gallery | section «گالری تصاویر» | path string input + add/remove thumbnails (`gallery: string[]`) |
| Discount / status / SKU | adjacent grid | `status` select, `discount`, `sku` |
| Colors (option-like) | section «رنگ‌ها» | hex string chips (`colors: string[]`) — **not** a variant matrix |
| Tags / SEO / shipping / flags | lower sections | `tags`, `metaTitle`, `metaDescription`, `weight`, `shipping.*`, `isAmazing`, `isNew`, freeShipping |

There are **no tabs** for product authoring. Layout is a single scrollable card form (`max-w-4xl`).

---

## 2. Shopeiva components (exact paths)

| Symbol | Path | Notes |
|---|---|---|
| `ProductsList` | `src/components/vendor/panel/products/productsList.jsx` | Client list UI |
| `ProductForm` | `src/components/vendor/panel/products/productForm.jsx` | Shared create/edit; detects edit via `useParams().id` |
| `DeleteConfirmModal` | inline in `productsList.jsx` | Delete confirm overlay |
| `CustomSelect` | `@/src/components/ui/customSelect` | Category / brand / status |
| `DashboardSkeleton` | `@/src/components/skeleton/dashboard` | Fake 300ms load on list/new pages |

### Data source (mock)

- Products: `public/jsons/products.json` (`products[]`)
- Categories: `public/jsons/menuCategories.json`
- Brands: `public/jsons/brands.json`

Submit / delete are **client stubs**: toast + `setTimeout` / `confirm` — no persistence API.

Sample product fields used by list+form: `id`, `name`, `categoryId`, `brandId`, `price`, `oldPrice`, `status`, `stock`, `discount`, `rating`, `visits`, `isAmazing`, `isNew`, `colors[]`, `image`, `gallery[]`, `sku`, `shortDescription`, `description`, `metaTitle`, `metaDescription`, `tags[]`, `weight`, `shipping.{freeShipping,shippingCost,deliveryTime}`.

---

## 3. Shopeiva list UI (`ProductsList`)

| Element | Behavior |
|---|---|
| Header | «مدیریت محصولات» + counts (total / active / inactive) |
| CTA | Link «محصول جدید» → `/vendor-panel/products/new` |
| Search | Fuse.js on `name`, `price`, `sku`, `shortDescription` |
| Filter dropdown | `all` / `active` / `inactive` / `draft` |
| Row layout | Card list (not DataGrid): thumbnail, name, badges `جدید` / `شگفت‌انگیز`, price + oldPrice + discount %, stock, status pill, SKU, rating, visits |
| Actions | Edit → `/vendor-panel/products/{id}/edit`; Delete → modal (toast only); Eye → storefront `/product/{id}/{slugified-name}` |
| Pagination | `react-paginate`, 8 per page |
| Responsive | Header wrap; search+filter stack on `sm`; row actions stay inline |

Accent in Shopeiva: `#E53935`.

---

## 4. Shopeiva create/edit form (`ProductForm`) — field map

Zod schema (`productSchema`) drives validation. Mode = create when no `params.id`; edit loads JSON by numeric id.

| Field group | Fields | Required |
|---|---|---|
| Identity | `name`, `categoryId`, `brandId` | yes |
| Commerce | `price`, `stock` | yes |
| Commerce optional | `oldPrice`, `discount` | no |
| Status / SKU | `status` (active/inactive/draft), `sku` | sku yes |
| Copy | `shortDescription`, `description` | yes |
| Options present today | `colors[]` (hex chips) | no |
| Taxonomy soft | `tags[]` | no |
| Media | `image` (file→data URL), `gallery[]` (path strings) | no |
| SEO | `metaTitle`, `metaDescription` | no |
| Logistics | `weight`, `shipping.shippingCost`, `shipping.deliveryTime`, `shipping.freeShipping` | no |
| Flags | `isAmazing`, `isNew` | no |

Buttons: «بازگشت», «افزودن محصول» / «ذخیره تغییرات»; edit-only «حذف محصول» (`window.confirm`).

### Variant / option boundary (Shopeiva — current only)

- Present: free-form **color hex list** only.
- Absent: variant matrix, axis builder, per-variant SKU/price/stock, size/material axes, bulk generation.
- **Do not redesign** beyond this current presentation for T021 fidelity work.

---

## 5. Tooba `/vendor-panel/products*` (current)

| Route | File | Status |
|---|---|---|
| `/vendor-panel/products` | `app/vendor-panel/products/page.tsx` | **LIVE** — Host `GET /v1/seller/offers` via `loadSellerOffers` |
| `/vendor-panel/products/[offerId]` | `app/vendor-panel/products/[offerId]/page.tsx` | **LIVE** — Host `GET` + `PATCH /v1/seller/offers/{offerId}` |
| `/vendor-panel/products/new` | — | **MISSING** (no route) |
| `/vendor-panel/products/[id]/edit` | — | **MISSING** (Tooba uses offerId detail, not product id/edit) |
| Separate media / price / stock routes | — | **MISSING** (same as Shopeiva: none) |

Nav: `vendor-shell.tsx` marks products `live: true`.

Client API: `app/vendor-panel/seller-api.ts`

- List: `loadSellerOffers` → Offer rows (`offerId`, `productTitle`, `sellerSku`, `status`, `amount`, `currency`, `availableUnits`, …)
- Detail: `loadSellerOfferDetail`
- Patch: `patchSellerOffer({ sellerSku?, status? })` only — **not** price/stock/media/title

### Tooba list (`VendorProductsPage`)

- Design-system **DataGrid** (not Shopeiva card list).
- Columns: product title+SKU link, seller SKU, Offer status, price, available units, lastUpdated, «ویرایش».
- Copy explicitly: «فهرست Offer؛ قیمت روی Product نیست».
- No create CTA, no delete, no storefront Eye link, no Fuse search chrome (grid filters instead).
- Auth-denied / Host-error states via `ErrorState`.

### Tooba detail (`VendorProductDetailPage`)

| Block | Editable? | Notes |
|---|---|---|
| Catalog context (title, brand, channel, catalogReadOnly) | read-only | Seller does not own Catalog Product here |
| `sellerSku` | **editable** | PATCH |
| Offer `status` (Active / Suspended) | **editable** | PATCH; no Draft in select |
| Offer price (`amount`) | **read-only display** | UI text: price/stock read-only in this slice |
| Stock (`availableUnits`; detail also has `onHand`/`reserved`) | **read-only display** | same |
| Media / gallery / description / colors / SEO / shipping | **absent** | stub vs Shopeiva form |
| Create product/offer from Vendor UI | **absent** | stub gap |

Architecture note (locked): Tooba models **Offer ≠ Product**. List/detail are Offer-centric; Shopeiva mock is product-centric with price/stock on the product JSON.

---

## 6. Live vs stub comparison matrix

| Capability | Shopeiva | Tooba today | Verdict |
|---|---|---|---|
| Products nav entry | Yes | Yes (`live: true`) | Live shell |
| List route | `/vendor-panel/products` | `/vendor-panel/products` | Live (Offer DataGrid + Host) |
| List data | Static JSON | Host offers | Live Host |
| Create route `/products/new` | Mock form | Missing | **Stub / gap** |
| Edit route product-id | `/products/[id]/edit` mock | `/products/[offerId]` Offer seam | Live Offer edit (different model) |
| Price edit in Vendor UI | Editable in form (mock save) | Display only | **Stub for write** (read live) |
| Stock edit in Vendor UI | Editable in form (mock save) | Display only | **Stub for write** (read live) |
| Media main + gallery | Present in form (mock) | Absent | **Stub / gap** |
| Title / description / category / brand edit | Present (mock) | Catalog read-only | Intentional Offer seam; create/content still gap |
| SKU edit | Yes (mock) | Yes (`sellerSku` PATCH) | Live |
| Status edit | active/inactive/draft (mock) | Active/Suspended PATCH | Live (status set differs) |
| Delete product | Modal toast stub | Absent | Stub both sides for real delete |
| Colors / soft options | Hex chips | Absent | Present only in Shopeiva mock; **no advanced variant UI either side** |
| Advanced variant matrix | Not present | Not present | Out of scope — do not invent |

---

## 7. Visual/fidelity anchors (for later UI work — no redesign here)

When closing seller product fidelity without inventing variants, prefer Shopeiva sources:

1. List chrome: header + «محصول جدید» CTA + search + status filter + card rows + badges + delete modal — `productsList.jsx`.
2. Form chrome: single card, red accent sections order (identity → price/stock → status/SKU → copy → colors → tags → media → SEO → shipping → flags → actions) — `productForm.jsx`.
3. Media: file picker for main image + path-based gallery chips — keep pattern; bind real storage later.
4. Price/stock: adjacent required inputs on create/edit — map to **Offer** (or Host-owned inventory) writes, never Product.Price/Stock conflation.
5. Colors: keep as simple chip list if shown; **do not** expand into matrix/bulk variant UI.

---

## 8. Gap summary for T021

**Live today**

- `/vendor-panel/products` Offer list from Host  
- `/vendor-panel/products/[offerId]` Offer detail with live PATCH for `sellerSku` + `status`  
- Price and stock **shown** from Host Offer projection  

**Stub / missing vs Shopeiva product-management surface**

- Create flow (`/products/new` + full form)  
- Writable price / stock in Vendor UI  
- Media main + gallery authoring  
- Product content fields (title, descriptions, category/brand picks, tags, SEO, shipping flags) on seller create/edit  
- List visual (cards, badges, delete modal, storefront preview) — structural fidelity debt  
- Soft color chips (optional; not variants)  

**Explicit non-goal**

- Advanced variant matrix / axes / bulk generation — not in Shopeiva Vendor source and must not be redesigned here.
