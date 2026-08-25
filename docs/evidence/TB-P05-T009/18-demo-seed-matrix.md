# TB-P05-T009-REPAIR-01 — Development demo seed matrix

Source of the matrix: `src/backend/Host/Tooba.Host/Storefront/StorefrontDemoCatalogMatrix.cs`.
Writer: `src/backend/Host/Tooba.Host/Storefront/StorefrontDemoCatalogBootstrap.cs`, invoked from `Program.cs` only inside the existing `app.Environment.IsDevelopment()` branch.

## Totals

| Metric | Seeded | Required minimum |
| --- | --- | --- |
| Top-level categories | 8 | 8 |
| Child categories | 24 | 24 |
| Published demo products | 72 | 72 |
| Published brands | 8 | 8 |
| Offers | 96 | — |
| Active prices (Pricing module, keyed by OfferId) | 96 | — |
| Inventory positions (Inventory module, keyed by OfferId) | 96 | — |
| Brand-associated demo products | 25 | — |

Offer arithmetic: one active offer per demo product (72) plus a second-seller offer on the first product of every child category (24), so marketplace behaviour is visible on the same variant.

`StorefrontDemoSeedSummary.PublishedProducts` counts every published Catalog product, so on a development database that also ran `ProductWorkspaceDevelopmentBootstrap` it reads 73 — the 72 demo products plus the pre-existing `workspace-live-shirt`. The 72 figure is asserted separately against the `demo-` slug prefix.

## Category matrix

Price present through Pricing module and Inventory present through Inventory module is `yes` for every row: each product's offer receives an active `IRR` price in an open validity window and an inventory position with positive on-hand. Catalog `Product` carries neither.

| Top-level category | Child category | Published products | Representative product names | Brand coverage | Offers | Price via Pricing | Inventory via Inventory |
| --- | --- | --- | --- | --- | --- | --- | --- |
| محصولات دیجیتال | گوشی موبایل | 3 | گوشی موبایل سامسونگ Galaxy A55؛ گوشی موبایل شیائومی Redmi Note 13؛ گوشی موبایل اپل iPhone 13 | Samsung, Xiaomi, Apple (3/3) | 4 | yes | yes |
| محصولات دیجیتال | لپ‌تاپ | 3 | لپ‌تاپ لنوو IdeaPad Slim 3؛ لپ‌تاپ ایسوس VivoBook 15؛ لپ‌تاپ اپل MacBook Air M2 | Lenovo, ASUS, Apple (3/3) | 4 | yes | yes |
| محصولات دیجیتال | هدفون و صوتی | 3 | هدفون بی‌سیم جی‌بی‌ال Tune 720BT؛ اسپیکر بلوتوثی شیائومی Mi Portable؛ هندزفری سیمی فیلیپس TAE1105 | JBL, Xiaomi, Philips (3/3) | 4 | yes | yes |
| لوازم خانگی | نوشیدنی‌ساز | 3 | چای‌ساز فیلیپس HD7301؛ قهوه‌ساز بوش TKA3A031؛ آب‌مرکبات‌گیری شیائومی Mi Juicer | Philips, Bosch, Xiaomi (3/3) | 4 | yes | yes |
| لوازم خانگی | پخت‌وپز | 3 | سرخ‌کن بدون روغن فیلیپس Airfryer؛ مایکروویو سامسونگ MG23؛ غذاساز بوش MCM3501 | Philips, Samsung, Bosch (3/3) | 4 | yes | yes |
| لوازم خانگی | نظافت | 3 | جاروبرقی بوش GL30؛ جاروشارژی شیائومی G10؛ بخارشوی فیلیپس STE3170 | Bosch, Xiaomi, Philips (3/3) | 4 | yes | yes |
| مد و پوشاک | پوشاک مردانه | 3 | پیراهن مردانه لینن آستین بلند؛ شلوار جین مردانه راسته؛ سویشرت مردانه کلاه‌دار | none (0/3) | 4 | yes | yes |
| مد و پوشاک | پوشاک زنانه | 3 | مانتو زنانه کتان جلوبسته؛ شومیز زنانه آستین بلند؛ شلوار زنانه پارچه‌ای دم‌پا | none (0/3) | 4 | yes | yes |
| مد و پوشاک | کفش | 3 | کفش ورزشی مردانه رانینگ؛ کتانی زنانه روزمره؛ نیم‌بوت چرم مردانه | none (0/3) | 4 | yes | yes |
| زیبایی و سلامت | مراقبت پوست | 3 | کرم آبرسان صورت؛ سرم ویتامین C؛ کرم ضدآفتاب SPF50 | none (0/3) | 4 | yes | yes |
| زیبایی و سلامت | آرایشی | 3 | رژ لب مات؛ ریمل حجم‌دهنده؛ کرم پودر پوشش‌دهنده | none (0/3) | 4 | yes | yes |
| زیبایی و سلامت | بهداشت شخصی | 3 | ریش‌تراش فیلیپس S3231؛ مسواک برقی فیلیپس Sonicare؛ سشوار حرفه‌ای یون‌ساز | Philips (2/3) | 4 | yes | yes |
| خانه و آشپزخانه | ظروف پخت‌وپز | 3 | سرویس قابلمه گرانیتی ۹ پارچه؛ تابه نچسب ۲۸ سانتی‌متری؛ زودپز استیل ۶ لیتری | none (0/3) | 4 | yes | yes |
| خانه و آشپزخانه | سرو و پذیرایی | 3 | سرویس غذاخوری چینی ۲۸ پارچه؛ ست لیوان شش‌عددی شیشه‌ای؛ سینی سرو چوبی دست‌ساز | none (0/3) | 4 | yes | yes |
| خانه و آشپزخانه | دکوراسیون | 3 | تابلو دیواری مدرن؛ گلدان سرامیکی دست‌ساز؛ آینه دیواری قدی | none (0/3) | 4 | yes | yes |
| خودرو و موتور | لوازم خودرو | 3 | تیغه برف‌پاک‌کن بوش Aerotwin؛ شارژر فندکی سریع شیائومی؛ کفپوش سه‌بعدی خودرو | Bosch, Xiaomi (2/3) | 4 | yes | yes |
| خودرو و موتور | قطعات مصرفی | 3 | فیلتر روغن بوش؛ لنت ترمز جلو بوش؛ شمع موتور بوش | Bosch (3/3) | 4 | yes | yes |
| خودرو و موتور | لوازم موتورسیکلت | 3 | کلاه کاسکت فول‌فیس؛ دستکش موتورسواری زمستانی؛ چادر محافظ موتورسیکلت | none (0/3) | 4 | yes | yes |
| ورزش و سفر | ورزش خانگی | 3 | تردمیل خانگی تاشو؛ دوچرخه ثابت اسپینینگ؛ ست دمبل ۲۰ کیلوگرمی | none (0/3) | 4 | yes | yes |
| ورزش و سفر | کمپینگ | 3 | چادر مسافرتی چهار نفره؛ کیسه خواب کوهنوردی؛ اجاق گاز سفری | none (0/3) | 4 | yes | yes |
| ورزش و سفر | کیف و چمدان | 3 | چمدان چرخ‌دار سایز بزرگ؛ کوله‌پشتی لپ‌تاپ ۱۵ اینچی؛ کیف دوشی چرم طبیعی | none (0/3) | 4 | yes | yes |
| کتاب، هنر و سرگرمی | کتاب | 3 | کتاب رمان ایرانی معاصر؛ کتاب تاریخ ایران باستان؛ کتاب مهارت‌های مدیریت زمان | none (0/3) | 4 | yes | yes |
| کتاب، هنر و سرگرمی | لوازم تحریر | 3 | دفتر یادداشت سیمی ۱۰۰ برگ؛ ست خودکار و روان‌نویس؛ پک ماژیک رنگی ۱۲ رنگ | none (0/3) | 4 | yes | yes |
| کتاب، هنر و سرگرمی | بازی و سرگرمی | 3 | دسته بازی بی‌سیم؛ پازل ۱۰۰۰ تکه؛ بازی فکری رومیزی خانوادگی | none (0/3) | 4 | yes | yes |

Products in fashion, beauty cosmetics, homeware, sports, luggage, books and games are seeded with a null brand rather than being attached to an unrelated electronics brand. Inventing a brand association would be a fabricated claim, so the honest gap is preserved.

## Brands

| Brand | Slug | Status | Associated demo products |
| --- | --- | --- | --- |
| شیائومی (Xiaomi) | `xiaomi` | Published | 5 |
| سامسونگ (Samsung) | `samsung` | Published | 2 |
| اپل (Apple) | `apple` | Published | 2 |
| لنوو (Lenovo) | `lenovo` | Published | 1 |
| ایسوس (ASUS) | `asus` | Published | 1 |
| بوش (Bosch) | `bosch` | Published | 7 |
| فیلیپس (Philips) | `philips` | Published | 6 |
| جی‌بی‌ال (JBL) | `jbl` | Published | 1 |

Brand media uses opaque placeholder media references. No logo file, proprietary asset, or marketing claim is fabricated.

## Commerce separation

| Rule | How the seed satisfies it |
| --- | --- |
| `Product != Offer` | Catalog writes only descriptive product/variant data through `ICatalogDirectory`; seller identity lives on the Offer |
| `Offer != Price` | Amounts are written through `IPriceDirectory.CreatePriceAsync` keyed by `OfferId` |
| `Product != Inventory` | On-hand is written through `IInventoryDirectory.OpenPositionAsync` / `AdjustAsync` keyed by `OfferId` |
| No `Product.Price` / `Product.Stock` | Neither field exists on the Catalog aggregate and the seed never introduces one |
| No cross-module SQL join | Every write goes through the owning module's directory contract; the only DbContext the seed touches is `CatalogDbContext`, and only to count and check the idempotency sentinel inside its own schema |

## Seed safety

| Property | Implementation |
| --- | --- |
| Environment scope | Reachable only from the existing `IsDevelopment()` branch in `Program.cs`, on tenant `store-alpha` with correlation `storefront-demo-seed` |
| Deterministic | Slugs are `demo-{token}-{n}`, SKUs are `DEMO-{TOKEN}-{n}-{A\|B}`, amounts derive from a fixed per-child base (`base + index * base/10`; second seller `base - base/20`), and `ValidFrom` is the constant `2026-01-01T00:00:00Z` |
| Repeatable / idempotent | Sentinel slug `demo-mobile-1`; when present the seed writes nothing and reports `AlreadySeeded = true` |
| Production impact | None; production bootstrap semantics are untouched and no existing seeded row is modified |

## Publication seam added

`CatalogCategory` and `CatalogBrand` are created as `Draft`, while `StorefrontComposer.ListCategoriesAsync` and `ListBrandsAsync` return only `Published` rows. Without a publish path the mega menu and brand surfaces were structurally guaranteed to stay empty. `PublishCategoryAsync` and `PublishBrandAsync` were added to `ICatalogDirectory` and implemented in `CatalogDirectory`, mirroring the existing `PublishProductAsync` semantics. Category publication is classification-only and brand publication is editorial only; neither confers purchasability, seller ownership, or a marketing claim.
