# TB-P07-T035 — Live Browser Audit Report

**Captured:** 2026-08-30 (local LIVE)  
**Runtimes:** Admin FE `http://127.0.0.1:3000` · Host `http://127.0.0.1:5088` · Shopeiva `http://127.0.0.1:3001`  
**Method:** Playwright Chromium (headless) against live URLs; Cursor browser MCP unavailable (no tab).  
**Reference:** `D:\Users\User\source\repos\SarvNewVerRequirment\reference\Image\ChatGPT Image Aug 29, 2026, 06_33_46 PM.png`  
**Scope:** Screenshots + this report only (no application source changes).

---

## Visual gate recommendation

**FAIL** — commercial Product VIEW/EDIT still has material polish gaps vs the locked reference, plus incomplete/technical UX leaks on seeded Draft products. Functional list/paging/create entry and Popular Brands brand-only checks are largely OK, but they do **not** clear the visual gate.

---

## Material mismatches (Product VIEW/EDIT vs reference)

1. **Hero / identity image** — LIVE shows a gray circular placeholder; reference shows a real product thumbnail beside title/badges.
2. **Media gallery** — LIVE General/Media surfaces show gray main image + solid color thumbnails (not photographic product media). Reference shows a real main image + photo gallery strip. Seeded claim “۵ رسانه / آماده” does not visually render as commercial media.
3. **General workspace composition** — Reference: inline title / slug / short summary / rich-text description with adjacent media sidebar. LIVE: “هویت غیرزبانی” metadata + locale preview cards; title/description not in the reference-like General form layout; feels thinner/sparser than the locked commercial composition.
4. **Summary readiness cards** — Card set/copy differs from reference (e.g. no dedicated “وضعیت محصول” tile as in reference; LIVE emphasizes Attributes + empty Variants). LIVE Variants card = **بدون تنوع** on sampled products vs reference’s populated variants story.
5. **Warning / Issues chrome** — LIVE shows **۴ هشدار**, **غیرقابل‌خرید**, readiness “۶ از ۶” plus **نیاز به بررسی**, and a floating red **“N Issues”** badge. Reference does not present this cluttered warning stack.
6. **Raw HTML in VIEW** — Full description preview shows literal tags (`<p>`, `<ul>`, `<li>`, `<strong>…`) instead of rendered rich text.
7. **Technical / non-human strings** — Publish-readiness area exposes codes such as **`no-active-offer`**; history entries show raw ISO timestamps (e.g. `2026-08-30T…+00:00`).
8. **Product list media polish** — Grid “رسانه” column uses color blocks / placeholders rather than real primary thumbnails, weakening commercial density at 283 scale.

**Note:** Some structural differences (locale-based title on Translations, 2 active languages vs reference’s 3) may be intentional domain policy; they still contribute to visual distance from the locked reference when combined with empty media placeholders and Issues clutter.

---

## Checklist results

| Area | Result | Notes |
|------|--------|-------|
| `/fa/admin/products` @ 283 | **PASS (functional)** | Server paging visible: `نمایش ۱ تا ۲۰ از ۲۸۳ محصول`; page size controls present. API `totalCount=283`. |
| No Price / Stock columns | **PASS** | Headers: عملیات، رسانه، محصول، وضعیت، دسته، برند، تنوع، به‌روزرسانی. |
| Commercial polish (list) | **FAIL** | Placeholder media cells; many `تنوع=0`; otherwise layout is clean. |
| ≥3 Product VIEW (Draft) | **PASS (opened)** | 3 Draft VIEW samples opened (power-bank family). |
| ≥1 Product EDIT | **PASS (opened)** | EDIT sticky Save / پایان ویرایش / انصراف present. |
| `/fa/admin/catalog/categories` tree + workspace | **PASS (reachable)** | Correct route is **`/fa/admin/catalog/categories`** (not `/fa/admin/categories`). Tree + General workspace tabs (ترجمه‌ها / ویژگی‌ها / فیلترها / مگامنو / محصولات) reachable. |
| Product create entry | **PASS** | `/fa/admin/products/new` 8-step wizard; not published. |
| Shopeiva `/` Popular Brands | **PASS (asked rules)** | `برندهای محبوب`: real brand names only; **no** blank / **No Brand** / **بدون برند**; **no** Draft product titles leaking in that section. |
| Incomplete UX scan | **FAIL items found** | Raw HTML; technical `no-active-offer`; ISO timestamps; Issues badge. No `Coming soon` / `به‌زودی` / classic GUID / `Bad Request` on sampled pages. |

---

## Screenshots saved

Directory: `docs/evidence/TB-P07-T035/screenshots/`

| File | Subject |
|------|---------|
| `01-products-grid.png` / `01b-products-grid-viewport.png` | Product list |
| `01c-products-grid-paging.png` | Paging footer (283) |
| `02-product-view-1.png` / `02b-…` / `02c-…` / `02d-…` | VIEW #1 (+ media tab, description/HTML) |
| `03-product-view-2.png` / `03b-…` | VIEW #2 |
| `04-product-view-3.png` / `04b-…` | VIEW #3 |
| `05-product-edit-1.png` / `05b-…` / `05c-…` | EDIT #1 (+ media tab) |
| `06-categories-tree.png` / `06b-…` | Category tree |
| `07-category-workspace.png` / `07c-category-workspace-viewport.png` | Category workspace (موبایل و تبلت) |
| `07b-category-workspace-tab.png` | Category workspace (earlier pass / tab attempt) |
| `08-product-create.png` / `08b-…` / `08c-…` | Create entry |
| `09-shopeiva-home.png` / `09b-…` / `09c-…` / `09d-…` | Storefront home + Popular Brands |

Supporting capture notes (non-report): `audit-notes.json`, `audit-notes-supplement.json`, `audit-notes-category.json`.

---

## Bottom line

- **Visual gate:** **FAIL**  
- **Material mismatches:** listed above (not none)  
- **Screenshot paths:** `D:\Users\User\source\repos\SarvNewVer\docs\evidence\TB-P07-T035\screenshots\`
