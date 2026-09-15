import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = join(ROOT, "docs/evidence/TB-P10-T021/screenshots");
const report = { ok: true, steps: [], files: {}, errors: [], console: [] };

mkdirSync(OUT, { recursive: true });

function rec(name, pass, data) {
  report.steps.push({ name, pass, ...(data ? { data } : {}) });
  if (!pass) {
    report.ok = false;
    report.errors.push(name);
  }
}

async function api(path, init = {}) {
  const res = await fetch(`${HOST}${path}`, {
    ...init,
    headers: { Accept: "application/json", Host: "alpha.localhost", ...(init.headers ?? {}) },
  });
  const text = await res.text();
  let body;
  try { body = JSON.parse(text); } catch { body = text.slice(0, 240); }
  return { status: res.status, body };
}

async function admin(path, init = {}) {
  return api(path, {
    ...init,
    headers: { "X-Tooba-Dev-Actor-User-Id": ACTOR, ...(init.headers ?? {}) },
  });
}

async function shot(page, name) {
  const dest = join(OUT, name);
  await page.screenshot({ path: dest, fullPage: false });
  const size = statSync(dest).size;
  report.files[name] = { bytes: size };
  rec(`file:${name}`, size > 1200, { bytes: size });
}

async function ensurePage(slug, title) {
  const existing = await admin("/v1/admin/pages");
  const prior = Array.isArray(existing.body) ? existing.body.find((row) => row.slug === slug || row.Slug === slug) : null;
  if (prior) return prior.pageId || prior.PageId;
  const created = await admin("/v1/admin/pages", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ title, slug, locale: "fa" }),
  });
  return created.body?.pageId || created.body?.PageId || null;
}

async function clearSections(pageId) {
  const listed = await admin(`/v1/admin/pages/${pageId}/sections`);
  const rows = Array.isArray(listed.body) ? listed.body : [];
  for (const row of rows) {
    const id = row.pageSectionId || row.PageSectionId;
    if (id) await admin(`/v1/admin/pages/${pageId}/sections/${id}`, { method: "DELETE" });
  }
}

async function seedSections(pageId, seed) {
  await clearSections(pageId);
  for (const item of seed) {
    await admin(`/v1/admin/pages/${pageId}/sections`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ sectionType: item.type, configurationJson: JSON.stringify(item.config) }),
    });
  }
  await admin(`/v1/admin/pages/${pageId}/status`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ status: "Published" }),
  });
}

async function waitPublic(page, slug) {
  let ok = false;
  for (let i = 0; i < 5; i++) {
    await page.goto(`${FE}/fa/${slug}`, { waitUntil: "domcontentloaded", timeout: 90000 });
    await page.locator("[data-testid=storefront-landing-page],[data-testid=landing-hero],[data-testid=storefront-custom-home]")
      .first().waitFor({ timeout: 45000 }).catch(() => {});
    ok = await page.locator("[data-testid=storefront-landing-page],[data-testid=landing-hero],[data-testid=storefront-custom-home],[data-testid=storefront-home]").count() > 0;
    if (ok) break;
    await page.waitForTimeout(2000);
  }
  return ok;
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1200 }, locale: "fa-IR" });
  await context.addInitScript(() => {
    localStorage.setItem("tooba.adminActorUserId", "01a036c2-970e-7000-8eb7-94bf5cc2d8db");
  });
  const page = await context.newPage();
  page.on("console", (msg) => report.console.push({ type: msg.type(), text: msg.text().slice(0, 200) }));
  page.on("pageerror", (err) => report.console.push({ type: "pageerror", text: String(err).slice(0, 200) }));
  await page.setDefaultTimeout(90000);

  await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: null }),
  }).catch(() => {});

  // Admin template picker polish
  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "load" });
  await page.locator("[data-testid=start-from-template]").click();
  await page.locator("[data-testid=template-picker]").waitFor({ timeout: 20000 });
  rec("A-template-picker", await page.locator("[data-testid=template-picker]").count() > 0);
  await shot(page, "template-picker-polished.png");

  // Blank mixed page for composer + variant pickers
  const mixedSlug = "t021-mixed";
  let mixedId = await ensurePage(mixedSlug, "صفحه ترکیبی T021");
  await page.goto(`${FE}/admin/landing-pages/${mixedId}`, { waitUntil: "load" });
  await clearSections(mixedId);
  await page.reload({ waitUntil: "load" });

  async function waitAddReady() {
    await page.waitForFunction(() => {
      const el = document.querySelector("[data-testid=landing-add-section]");
      return el instanceof HTMLButtonElement && !el.disabled;
    }, null, { timeout: 20000 });
  }

  async function openVariantPicker(sectionTestId) {
    await waitAddReady();
    await page.locator("[data-testid=landing-add-section]").click();
    await page.locator("[data-testid=composition-section-catalog]").waitFor({ timeout: 10000 });
    await page.locator(`[data-testid=${sectionTestId}]`).click();
    await page.locator("[data-testid=composition-variant-picker]").waitFor({ timeout: 10000 });
  }

  await openVariantPicker("add-section-products");
  await shot(page, "variant-picker-product-wave2.png");
  await page.locator("[data-testid=pick-variant-product-tabbed]").click();
  await page.locator("[data-testid=landing-section-drawer]").waitFor({ timeout: 15000 });
  await shot(page, "section-settings-expanded.png");
  await shot(page, "empty-state-product-source.png");
  await page.locator("[data-testid=landing-section-drawer]").getByRole("button", { name: "ذخیرهٔ بخش" }).click();
  await page.waitForTimeout(600);

  await openVariantPicker("add-section-banners");
  await shot(page, "variant-picker-banner-wave2.png");
  await page.locator("[data-testid=pick-variant-banner-three]").click();
  await page.locator("[data-testid=landing-section-drawer]").getByRole("button", { name: "ذخیرهٔ بخش" }).click();
  await page.waitForTimeout(600);

  await openVariantPicker("add-section-hero");
  await page.locator("[data-testid=pick-variant-hero-editorial]").click();
  await page.locator("[data-testid=landing-section-drawer]").getByRole("button", { name: "ذخیرهٔ بخش" }).click();
  await page.waitForTimeout(600);

  await page.reload({ waitUntil: "load" });
  await shot(page, "section-card-collapsed.png");
  rec("B-composer-cards", await page.locator("[data-testid=landing-section-composer]").count() > 0);

  // Seed industry template pages via API (fidelity compositions)
  const fashionId = await ensurePage("t021-fashion", "پوشاک T021");
  await seedSections(fashionId, [
    { type: "Hero", config: { title: "پوشاک", variantKey: "hero.editorial", href: "/products", heightPreset: "Large" } },
    { type: "StoryRail", config: { title: "استوری", variantKey: "story.circle", items: [{ title: "س۱", href: "/products", enabled: true }] } },
    { type: "CategoryGrid", config: { title: "دسته", variantKey: "category.image-cards", categoryIds: [] } },
    { type: "ProductCollection", config: { title: "کالا", variantKey: "product.card-carousel", source: "Newest", take: 8, productIds: [] } },
    { type: "BannerShowcase", config: { title: "بنر", variantKey: "banner.two-equal", heightPreset: "Medium", items: [{ title: "1", href: "/offers" }, { title: "2", href: "/offers" }] } },
  ]);

  const autoId = await ensurePage("t021-autoparts", "یدکی T021");
  await seedSections(autoId, [
    { type: "CategoryGrid", config: { title: "دسته", variantKey: "category.compact-tiles", categoryIds: [] } },
    { type: "BrandStrip", config: { title: "برند", variantKey: "brand.logo-grid", brandIds: [] } },
    { type: "ProductCollection", config: { title: "کالا", variantKey: "product.compact-rows", source: "Newest", take: 8, productIds: [] } },
  ]);

  const interiorId = await ensurePage("t021-interior", "دکوراسیون T021");
  await seedSections(interiorId, [
    { type: "Hero", config: { title: "دکوراسیون", variantKey: "hero.split", href: "/products", heightPreset: "Large" } },
    { type: "CategoryGrid", config: { title: "دسته", variantKey: "category.editorial-tiles", categoryIds: [] } },
    { type: "ProductCollection", config: { title: "کالا", variantKey: "product.large-cards", source: "Newest", take: 6, productIds: [] } },
    { type: "ArticleList", config: { title: "مطالب", variantKey: "article.featured-plus-list", source: "Latest", take: 6 } },
  ]);

  const beautyId = await ensurePage("t021-beauty", "آرایشی T021");
  await seedSections(beautyId, [
    { type: "StoryRail", config: { title: "استوری", variantKey: "story.icon-shortcuts", items: [{ title: "س۱", href: "/products", enabled: true }] } },
    { type: "BrandStrip", config: { title: "برند", variantKey: "brand.featured", brandIds: [] } },
    { type: "ProductCollection", config: { title: "کالا", variantKey: "product.tabbed", source: "Newest", take: 8, productIds: [] } },
    { type: "BannerShowcase", config: { title: "بنر", variantKey: "banner.three", heightPreset: "Medium", items: [{ title: "1", href: "/offers" }, { title: "2", href: "/offers" }, { title: "3", href: "/offers" }] } },
  ]);

  await seedSections(mixedId, [
    { type: "Hero", config: { title: "ترکیبی", variantKey: "hero.editorial", href: "/products", heightPreset: "Large" } },
    { type: "StoryRail", config: { title: "استوری", variantKey: "story.icon-shortcuts", items: [{ title: "آ", href: "/products", enabled: true }] } },
    { type: "ProductCollection", config: { title: "کالا", variantKey: "product.minimal-list", source: "Newest", take: 8, productIds: [] } },
    { type: "ProductCollection", config: { title: "رتبه", variantKey: "ranked.ticker", source: "Newest", take: 8, productIds: [] } },
    { type: "BannerShowcase", config: { title: "بنر", variantKey: "banner.four-grid", heightPreset: "Medium", items: [{ title: "1", href: "/offers" }, { title: "2", href: "/offers" }, { title: "3", href: "/offers" }, { title: "4", href: "/offers" }] } },
  ]);

  // Desktop storefront shots
  await page.setViewportSize({ width: 1440, height: 1200 });
  rec("fashion-desktop", await waitPublic(page, "t021-fashion"));
  await shot(page, "fashion-desktop.png");
  rec("autoparts-desktop", await waitPublic(page, "t021-autoparts"));
  await shot(page, "autoparts-desktop.png");
  rec("interior-desktop", await waitPublic(page, "t021-interior"));
  await shot(page, "interior-decor-desktop.png");
  rec("beauty-desktop", await waitPublic(page, "t021-beauty"));
  await shot(page, "beauty-desktop.png");
  rec("mixed-desktop", await waitPublic(page, mixedSlug));
  await shot(page, "mixed-landing-desktop.png");

  // Mobile
  await page.setViewportSize({ width: 390, height: 844 });
  rec("fashion-mobile", await waitPublic(page, "t021-fashion"));
  await shot(page, "fashion-mobile.png");
  rec("autoparts-mobile", await waitPublic(page, "t021-autoparts"));
  await shot(page, "autoparts-mobile.png");
  rec("interior-mobile", await waitPublic(page, "t021-interior"));
  await shot(page, "interior-decor-mobile.png");
  rec("beauty-mobile", await waitPublic(page, "t021-beauty"));
  await shot(page, "beauty-mobile.png");
  rec("mixed-mobile", await waitPublic(page, mixedSlug));
  await shot(page, "mixed-landing-mobile.png");

  // Set fashion as custom home briefly then restore
  await page.setViewportSize({ width: 1440, height: 1200 });
  const homeSet = await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: fashionId }),
  });
  rec("set-home", homeSet.status < 300);
  await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(2000);
  rec("custom-home", await page.locator("[data-testid=storefront-custom-home],[data-testid=storefront-landing-page]").count() > 0);
  await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: null }),
  });
  await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(1500);
  rec("canonical-home", await page.locator("[data-testid=storefront-home]").count() > 0);

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T021/runtime-report.json"), JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ ok: report.ok, errors: report.errors, files: Object.keys(report.files) }, null, 2));
  process.exit(report.ok ? 0 : 1);
})().catch((err) => {
  console.error(err);
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T021/runtime-report.json"), JSON.stringify(report, null, 2));
  process.exit(1);
});
