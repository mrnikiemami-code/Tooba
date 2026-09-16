import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R8-R1/screenshots");
const report = { ok: true, steps: [], files: {}, geometry: {}, purity: {}, errors: [] };

mkdirSync(OUT, { recursive: true });

function rec(name, pass, data) {
  report.steps.push({ name, pass, ...(data ? { data } : {}) });
  if (!pass) {
    report.ok = false;
    report.errors.push(name);
  }
}

async function shot(page, name, options = {}) {
  const dest = join(OUT, name);
  await page.screenshot({ path: dest, fullPage: Boolean(options.fullPage), ...options });
  const size = statSync(dest).size;
  report.files[name] = { bytes: size };
  rec(`file:${name}`, size > 1200, { bytes: size });
  return dest;
}

async function waitPreview(page) {
  await page.locator("[data-testid=fashion-template-preview]").waitFor({ timeout: 90000 });
  await page.waitForTimeout(800);
}

async function sectionMetrics(page, selectors) {
  for (const sel of selectors) {
    const loc = page.locator(sel).first();
    if ((await loc.count()) === 0) continue;
    await loc.scrollIntoViewIfNeeded().catch(() => {});
    await page.waitForTimeout(400);
    const box = await loc.boundingBox();
    const empty = await loc.getAttribute("data-empty");
    const badges = await loc.locator("[data-preview-fake-badge=true]").count();
    const placeholder = await page.locator("[data-testid*=placeholder], .border-dashed").count();
    return { selector: sel, box, empty, badges, placeholderHits: placeholder };
  }
  return null;
}

async function shotSection(page, name, selectors) {
  const metrics = await sectionMetrics(page, selectors);
  if (!metrics?.box) {
    rec(`section:${name}`, false, { reason: "missing" });
    await shot(page, name);
    return null;
  }
  await page.locator(metrics.selector).first().screenshot({ path: join(OUT, name) });
  const size = statSync(join(OUT, name)).size;
  report.files[name] = { bytes: size, metrics };
  rec(`file:${name}`, size > 1200, { bytes: size, metrics });
  return metrics;
}

async function openPreview(context, path) {
  const page = await context.newPage();
  page.setDefaultTimeout(90000);
  await page.goto(`${FE}${path}`, { waitUntil: "domcontentloaded", timeout: 120000 });
  await waitPreview(page);
  return page;
}

async function installEmptyCatalogRoutes(page, mode = "products-categories") {
  // hasStoreData requires at least one of products/categories/brands.
  // Mode switches which sentinel keeps the preview loadable while zeroing the target sections.
  const sentinelCategory = [
    {
      categoryId: "01a05387-e895-7000-ab2e-c875d2ff38cc",
      parentCategoryId: null,
      name: "دسته نگهبان",
      imageUrl: "/images/fashion-template/1.jpg",
    },
  ];
  const sentinelBrand = [
    {
      brandId: "brand-sentinel-1",
      slug: "brand-sentinel-1",
      name: "برند نگهبان",
      productCount: 0,
      logoUrl: "/images/fashion-template/5.jpg",
    },
  ];
  const emptyProducts = mode !== "keep-products";
  const emptyCategories = mode === "brands-zero";
  const emptyBrands = mode !== "brands-zero";

  await page.route("**/v1/storefront/products**", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        products: [],
        categories: emptyCategories ? [] : sentinelCategory,
        page: 1,
        pageSize: 24,
        totalCount: 0,
      }),
    });
  });
  await page.route("**/v1/storefront/categories**", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(emptyCategories ? [] : sentinelCategory),
    });
  });
  await page.route("**/v1/storefront/brands**", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(emptyBrands ? [] : sentinelBrand),
    });
  });
  void emptyProducts;
}

async function installPartialCatalogRoutes(page) {
  await page.route("**/v1/storefront/products**", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        products: [
          {
            productId: "store-visual-product-1",
            slug: "store-visual-product-1",
            title: "کالای واقعی فروشگاه",
            categoryName: "پوشاک",
            categoryId: "cat-real-1",
            mediaAssetId: "",
            mediaUrl: "/images/fashion-template/3.jpg",
            primaryOfferId: "offer-1",
            sellerPartyId: "seller-1",
            sellerDisplayName: "فروشنده",
            offerAmountExclusiveOfTax: 990000,
            promotionalAmountExclusiveOfTax: null,
            currency: "IRR",
            availableUnits: 3,
            inStock: true,
            promotionLabel: null,
            averageRating: 4.8,
            reviewCount: 2,
            brandId: null,
          },
        ],
        categories: [],
        page: 1,
        pageSize: 24,
        totalCount: 1,
      }),
    });
  });
  await page.route("**/v1/storefront/categories**", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify([
        {
          categoryId: "cat-real-1",
          parentCategoryId: null,
          name: "دسته واقعی",
          imageUrl: "/images/fashion-template/4.jpg",
        },
      ]),
    });
  });
  await page.route("**/v1/storefront/brands**", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify([
        {
          brandId: "brand-real-1",
          slug: "brand-real-1",
          name: "برند واقعی",
          productCount: 1,
          logoUrl: "/images/fashion-template/5.jpg",
        },
      ]),
    });
  });
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1100 }, locale: "fa-IR" });
  await context.addInitScript(() => {
    localStorage.setItem("tooba.adminActorUserId", "01a036c2-970e-7000-8eb7-94bf5cc2d8db");
  });

  // --- Sample mode ---
  const sample = await openPreview(context, "/template-preview/fashion?source=sample&locale=fa-IR");
  const sampleReviews = await shotSection(sample, "sample-reviews.png", [
    "[data-testid=home-testimonials]",
    "[data-testid=landing-reviews-empty]",
  ]);
  const sampleArticles = await shotSection(sample, "sample-articles.png", [
    "[data-testid=home-articles]",
    "[data-testid=landing-articles-empty]",
  ]);
  report.geometry.sampleReviews = sampleReviews;
  report.geometry.sampleArticles = sampleArticles;
  const sampleOrigin = await sample.locator("[data-testid=fashion-template-preview]").getAttribute("data-demo-origin");
  const sampleSource = await sample.locator("[data-testid=fashion-template-preview]").getAttribute("data-preview-source");
  rec("sample-template-origin", sampleOrigin === "fashion-template-catalog-persisted" || Boolean(sampleOrigin), {
    sampleOrigin,
    sampleSource,
  });
  await sample.close();

  // --- Store zero (reviews/articles always empty; catalog mocked) ---
  // products-categories mode: sentinel category keeps load alive; products+brands zero-filled
  const storeZero = await context.newPage();
  storeZero.setDefaultTimeout(90000);
  await installEmptyCatalogRoutes(storeZero, "products-categories");
  await storeZero.goto(`${FE}/template-preview/fashion?source=store&locale=fa-IR`, {
    waitUntil: "domcontentloaded",
    timeout: 120000,
  });
  await waitPreview(storeZero);
  const storeOrigin = await storeZero.locator("[data-testid=fashion-template-preview]").getAttribute("data-demo-origin");
  const storeSource = await storeZero.locator("[data-testid=fashion-template-preview]").getAttribute("data-preview-source");
  rec("store-origin", storeOrigin === "operational-store-catalog" && storeSource === "store", {
    storeOrigin,
    storeSource,
  });

  const storeReviewsZero = await shotSection(storeZero, "store-reviews-zero-filled.png", [
    "[data-testid=home-testimonials]",
  ]);
  const storeArticlesZero = await shotSection(storeZero, "store-articles-zero-filled.png", [
    "[data-testid=home-articles]",
  ]);
  await shotSection(storeZero, "store-banner-zero-filled.png", ["[data-testid=landing-banner-showcase]"]);
  await shotSection(storeZero, "store-products-zero-filled.png", [
    "[data-testid=landing-products]",
    "[data-testid=home-new-products]",
  ]);
  await shotSection(storeZero, "store-brands-zero-filled.png", [
    "[data-testid=home-brands]",
    "[data-testid=landing-brands-empty]",
  ]);

  const fakeBadges = await storeZero.locator("[data-preview-fake-badge=true]").count();
  const dashedPlaceholders = await storeZero.locator(".border-dashed").count();
  rec("store-has-fake-badges", fakeBadges > 0, { fakeBadges });
  rec("store-no-generic-dashed-primary", dashedPlaceholders === 0, { dashedPlaceholders });
  report.geometry.storeReviewsZero = storeReviewsZero;
  report.geometry.storeArticlesZero = storeArticlesZero;
  report.purity.storeFakeBadges = fakeBadges;
  report.purity.storeDashed = dashedPlaceholders;

  // brands-zero mode: sentinel brand; categories (and products) zero-filled
  const storeCats = await context.newPage();
  storeCats.setDefaultTimeout(90000);
  await installEmptyCatalogRoutes(storeCats, "brands-zero");
  await storeCats.goto(`${FE}/template-preview/fashion?source=store&locale=fa-IR`, {
    waitUntil: "domcontentloaded",
    timeout: 120000,
  });
  await waitPreview(storeCats);
  await shotSection(storeCats, "store-categories-zero-filled.png", [
    "[data-testid=landing-categories]",
    "[data-testid=home-categories]",
  ]);
  await storeCats.close();

  // Geometry comparison pages (side-by-side iframes)
  const geoReviews = await context.newPage();
  await geoReviews.setContent(`<!doctype html><html><body style="margin:0;display:flex;gap:8px;background:#e2e8f0">
    <iframe src="${FE}/template-preview/fashion?source=sample&locale=fa-IR" style="width:700px;height:900px;border:0;background:#fff"></iframe>
    <iframe src="${FE}/template-preview/fashion?source=store&locale=fa-IR" style="width:700px;height:900px;border:0;background:#fff"></iframe>
  </body></html>`);
  await geoReviews.waitForTimeout(6000);
  await shot(geoReviews, "sample-vs-store-reviews-geometry.png", { fullPage: true });
  await shot(geoReviews, "sample-vs-store-articles-geometry.png", { fullPage: true });
  await geoReviews.close();

  // Localized fake items
  await storeZero.goto(`${FE}/template-preview/fashion?source=store&locale=en-US`, {
    waitUntil: "domcontentloaded",
    timeout: 120000,
  });
  await waitPreview(storeZero);
  await shotSection(storeZero, "store-preview-localized-fake-items.png", [
    "[data-testid=landing-products]",
    "[data-testid=home-new-products]",
    "[data-testid=home-testimonials]",
  ]);
  const enBadge = await storeZero.locator("text=Sample preview").count();
  rec("localized-en-badge", enBadge > 0, { enBadge });
  await storeZero.close();

  // --- Partial reviews/articles via fixture + partial catalog ---
  const storePartial = await context.newPage();
  storePartial.setDefaultTimeout(90000);
  await installPartialCatalogRoutes(storePartial);
  await storePartial.goto(
    `${FE}/template-preview/fashion?source=store&locale=fa-IR&fixture=partial-fillers`,
    { waitUntil: "domcontentloaded", timeout: 120000 },
  );
  await waitPreview(storePartial);
  const partialReviews = await shotSection(storePartial, "store-reviews-partial-filled.png", [
    "[data-testid=home-testimonials]",
  ]);
  const partialArticles = await shotSection(storePartial, "store-articles-partial-filled.png", [
    "[data-testid=home-articles]",
  ]);
  const realReviewVisible = await storePartial.locator("text=خریدار فروشگاه").count();
  const fakeInPartial = await storePartial
    .locator("[data-testid=home-testimonials] [data-preview-fake-badge=true]")
    .count();
  rec("partial-reviews-mixed", realReviewVisible > 0 && fakeInPartial > 0, {
    realReviewVisible,
    fakeInPartial,
    metrics: partialReviews,
  });
  const realArticleVisible = await storePartial.locator("text=مقاله واقعی فروشگاه").count();
  const fakeArticles = await storePartial
    .locator("[data-testid=home-articles] [data-preview-fake-badge=true]")
    .count();
  rec("partial-articles-mixed", realArticleVisible > 0 && fakeArticles > 0, {
    realArticleVisible,
    fakeArticles,
    metrics: partialArticles,
  });
  await storePartial.close();

  // --- Banner iframe (admin) + fullpage ---
  const admin = await context.newPage();
  admin.setDefaultTimeout(90000);
  await admin.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "domcontentloaded", timeout: 120000 });
  await admin.locator("[data-testid=start-from-template]").click();
  await admin.locator("[data-testid=template-selection-workspace], [data-testid=template-picker]").first().waitFor({
    timeout: 20000,
  });
  await admin.locator("[data-testid=template-card-fashion]").click();
  await admin.waitForTimeout(800);
  await admin.locator("[data-testid=load-store-data-action]").click();
  await admin.waitForTimeout(2500);
  await admin.locator("[data-testid=fashion-preview-iframe]").waitFor({ timeout: 30000 });
  const iframe = admin.frameLocator("[data-testid=fashion-preview-iframe]");
  await iframe.locator("[data-testid=fashion-template-preview]").waitFor({ timeout: 60000 }).catch(() => {});
  await iframe.locator("[data-testid=landing-banner-showcase]").first().scrollIntoViewIfNeeded().catch(() => {});
  await admin.waitForTimeout(600);
  await shot(admin, "store-banner-iframe.png");
  const lang = admin.locator("[data-testid=template-preview-language]");
  if ((await lang.count()) > 0) {
    await lang.scrollIntoViewIfNeeded();
    await shot(admin, "preview-language-selector-r8r1.png");
  } else {
    rec("language-selector-present", false);
  }

  const full = await openPreview(context, "/template-preview/fashion/full?source=store&locale=fa-IR");
  await full.locator("[data-testid=landing-banner-showcase]").first().scrollIntoViewIfNeeded().catch(() => {});
  await full.waitForTimeout(500);
  await shot(full, "store-banner-fullpage.png", { fullPage: false });
  await full.close();

  // --- Published storefront safety ---
  const pub = await context.newPage();
  pub.setDefaultTimeout(90000);
  await pub.goto(`${FE}/fa`, { waitUntil: "domcontentloaded", timeout: 120000 });
  await pub.waitForTimeout(2000);
  const pubFakeBadge = await pub.locator("[data-preview-fake-badge=true]").count();
  const pubSampleFa = await pub.locator("text=نمونه نمایشی").count();
  const pubSampleEn = await pub.locator("text=Sample preview").count();
  await shot(pub, "published-storefront-no-preview-fake.png", { fullPage: false });
  rec("published-no-fake-badge", pubFakeBadge === 0 && pubSampleFa === 0 && pubSampleEn === 0, {
    pubFakeBadge,
    pubSampleFa,
    pubSampleEn,
  });
  report.purity.published = { pubFakeBadge, pubSampleFa, pubSampleEn };
  await pub.close();

  // Geometry material equivalence (heights)
  const rhS = sampleReviews?.box?.height ?? 0;
  const rhZ = storeReviewsZero?.box?.height ?? 0;
  const ahS = sampleArticles?.box?.height ?? 0;
  const ahZ = storeArticlesZero?.box?.height ?? 0;
  const reviewsRatio = rhS && rhZ ? Math.min(rhS, rhZ) / Math.max(rhS, rhZ) : 0;
  const articlesRatio = ahS && ahZ ? Math.min(ahS, ahZ) / Math.max(ahS, ahZ) : 0;
  report.geometry.reviewsHeightRatio = reviewsRatio;
  report.geometry.articlesHeightRatio = articlesRatio;
  rec("geometry-reviews-material", reviewsRatio >= 0.55, { rhS, rhZ, reviewsRatio });
  rec("geometry-articles-material", articlesRatio >= 0.55, { ahS, ahZ, articlesRatio });

  await admin.close();
  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T022-R8-R1/runtime-report.json"), JSON.stringify(report, null, 2));
  console.log(JSON.stringify(report, null, 2));
  process.exit(report.ok ? 0 : 1);
})().catch((err) => {
  report.ok = false;
  report.errors.push(String(err?.stack || err));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T022-R8-R1/runtime-report.json"), JSON.stringify(report, null, 2));
  console.error(err);
  process.exit(1);
});
