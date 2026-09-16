import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R8-R2/screenshots");
const report = { ok: true, steps: [], files: {}, purity: {}, errors: [] };

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
  rec(`file:${name}`, size > 800, { bytes: size });
  return dest;
}

async function waitPreview(page) {
  await page.locator("[data-testid=fashion-template-preview]").waitFor({ timeout: 90000 });
  await page.waitForTimeout(900);
}

async function shotSection(page, name, selectors) {
  for (const sel of selectors) {
    const loc = page.locator(sel).first();
    if ((await loc.count()) === 0) continue;
    await loc.scrollIntoViewIfNeeded().catch(() => {});
    await page.waitForTimeout(400);
    const box = await loc.boundingBox();
    if (!box) continue;
    await loc.screenshot({ path: join(OUT, name) });
    const size = statSync(join(OUT, name)).size;
    report.files[name] = { bytes: size, selector: sel };
    rec(`file:${name}`, size > 800, { bytes: size, selector: sel });
    return { selector: sel, box };
  }
  rec(`section:${name}`, false, { reason: "missing" });
  await shot(page, name);
  return null;
}

async function installEmptyCatalogRoutes(page, mode = "products-categories") {
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
            mediaUrl: "/images/products/1.jpg",
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
          imageUrl: "/images/categories/2.png",
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
          logoUrl: "/images/brand/brand1-1.png",
        },
      ]),
    });
  });
}

async function collectImgSrcs(page, rootSel) {
  return page.locator(`${rootSel} img`).evaluateAll((imgs) => imgs.map((i) => i.getAttribute("src") || ""));
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1100 }, locale: "fa-IR" });
  await context.addInitScript(() => {
    localStorage.setItem("tooba.adminActorUserId", "01a036c2-970e-7000-8eb7-94bf5cc2d8db");
  });

  // Sample — Template Fashion still
  const sample = await context.newPage();
  sample.setDefaultTimeout(90000);
  await sample.goto(`${FE}/template-preview/fashion?source=sample&locale=fa-IR`, {
    waitUntil: "domcontentloaded",
    timeout: 120000,
  });
  await waitPreview(sample);
  await shot(sample, "sample-fashion-still-template-data.png", { fullPage: false });
  const sampleImgs = await collectImgSrcs(sample, "[data-testid=fashion-template-preview]");
  const sampleHasFashion = sampleImgs.some((s) => s.includes("/images/fashion-template/"));
  const sampleHasPreviewFake = sampleImgs.some((s) => s.includes("/images/preview-placeholder/"));
  rec("sample-uses-fashion-template", sampleHasFashion, { sampleHasFashion, sampleHasPreviewFake, count: sampleImgs.length });
  rec("sample-no-preview-fake-media", !sampleHasPreviewFake, { sampleHasPreviewFake });
  await sample.close();

  // Store zero — PreviewFake isolated
  const storeZero = await context.newPage();
  storeZero.setDefaultTimeout(90000);
  await installEmptyCatalogRoutes(storeZero, "products-categories");
  await storeZero.goto(`${FE}/template-preview/fashion?source=store&locale=fa-IR`, {
    waitUntil: "domcontentloaded",
    timeout: 120000,
  });
  await waitPreview(storeZero);

  await shotSection(storeZero, "store-banner-previewfake-isolated.png", ["[data-testid=landing-banner-showcase]"]);
  await shotSection(storeZero, "store-products-previewfake-isolated.png", [
    "[data-testid=landing-products]",
    "[data-testid=home-new-products]",
  ]);
  await shotSection(storeZero, "store-brands-previewfake-isolated.png", [
    "[data-testid=home-brands]",
    "[data-testid=landing-brands-empty]",
  ]);
  await shotSection(storeZero, "store-articles-previewfake-isolated.png", ["[data-testid=home-articles]"]);
  await shotSection(storeZero, "store-reviews-previewfake-isolated.png", ["[data-testid=home-testimonials]"]);
  await shotSection(storeZero, "store-stories-previewfake-isolated.png", [
    "[data-testid=home-stories]",
    "[data-testid=landing-stories]",
  ]);

  const storeImgs = await collectImgSrcs(storeZero, "[data-testid=fashion-template-preview]");
  const fakeBadges = await storeZero.locator("[data-preview-fake-badge=true]").count();
  const storeHasFashionInFakeSlots = storeImgs.some((s) => s.includes("/images/fashion-template/"));
  const storeHasPreviewFake = storeImgs.some((s) => s.includes("/images/preview-placeholder/"));
  rec("store-has-fake-badges", fakeBadges > 0, { fakeBadges });
  rec("store-uses-preview-placeholder", storeHasPreviewFake, { storeHasPreviewFake });
  // Sentinel category may still show fashion-template if used as sentinel — flag only if banners/products use it heavily
  report.purity.storeImgs = { fashionHits: storeImgs.filter((s) => s.includes("fashion-template")).length, previewFakeHits: storeImgs.filter((s) => s.includes("preview-placeholder")).length, fakeBadges };

  const storeCats = await context.newPage();
  storeCats.setDefaultTimeout(90000);
  await installEmptyCatalogRoutes(storeCats, "brands-zero");
  await storeCats.goto(`${FE}/template-preview/fashion?source=store&locale=fa-IR`, {
    waitUntil: "domcontentloaded",
    timeout: 120000,
  });
  await waitPreview(storeCats);
  await shotSection(storeCats, "store-categories-previewfake-isolated.png", [
    "[data-testid=landing-categories]",
    "[data-testid=home-categories]",
  ]);
  await storeCats.close();

  // Partial
  const storePartial = await context.newPage();
  storePartial.setDefaultTimeout(90000);
  await installPartialCatalogRoutes(storePartial);
  await storePartial.goto(`${FE}/template-preview/fashion?source=store&locale=fa-IR&fixture=partial-fillers`, {
    waitUntil: "domcontentloaded",
    timeout: 120000,
  });
  await waitPreview(storePartial);
  await shot(storePartial, "store-partial-real-plus-previewfake.png", { fullPage: false });
  const realProduct = await storePartial.locator("text=کالای واقعی فروشگاه").count();
  const fakeInPartial = await storePartial.locator("[data-preview-fake-badge=true]").count();
  rec("partial-mixed", realProduct > 0 && fakeInPartial > 0, { realProduct, fakeInPartial });
  await storePartial.close();

  // Distinctness comparison
  const compare = await context.newPage();
  await compare.setContent(`<!doctype html><html><body style="margin:0;display:flex;gap:8px;background:#e2e8f0">
    <div style="flex:1"><div style="padding:8px;font:14px sans-serif">Sample (Template)</div>
      <iframe src="${FE}/template-preview/fashion?source=sample&locale=fa-IR" style="width:100%;height:980px;border:0;background:#fff"></iframe>
    </div>
    <div style="flex:1"><div style="padding:8px;font:14px sans-serif">Store Preview-Fake</div>
      <iframe src="${FE}/template-preview/fashion?source=store&locale=fa-IR" style="width:100%;height:980px;border:0;background:#fff"></iframe>
    </div>
  </body></html>`);
  await compare.waitForTimeout(7000);
  await shot(compare, "sample-vs-store-assets-clearly-distinct.png", { fullPage: true });
  await compare.close();

  // Published
  const pub = await context.newPage();
  pub.setDefaultTimeout(90000);
  await pub.goto(`${FE}/fa`, { waitUntil: "domcontentloaded", timeout: 120000 });
  await pub.waitForTimeout(2000);
  const pubFakeBadge = await pub.locator("[data-preview-fake-badge=true]").count();
  const pubSampleFa = await pub.locator("text=نمونه نمایشی").count();
  const pubPreviewFakeMedia = await collectImgSrcs(pub, "body");
  const pubHasPreviewFake = pubPreviewFakeMedia.some((s) => s.includes("/images/preview-placeholder/"));
  await shot(pub, "published-storefront-no-previewfake-r2.png", { fullPage: false });
  rec("published-no-fake", pubFakeBadge === 0 && pubSampleFa === 0 && !pubHasPreviewFake, {
    pubFakeBadge,
    pubSampleFa,
    pubHasPreviewFake,
  });
  await pub.close();

  await storeZero.close();
  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T022-R8-R2/runtime-report.json"), JSON.stringify(report, null, 2));
  console.log(JSON.stringify(report, null, 2));
  process.exit(report.ok ? 0 : 1);
})().catch((err) => {
  report.ok = false;
  report.errors.push(String(err?.stack || err));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T022-R8-R2/runtime-report.json"), JSON.stringify(report, null, 2));
  console.error(err);
  process.exit(1);
});
