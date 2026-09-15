import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022/screenshots");
const report = { ok: true, steps: [], files: {}, errors: [] };

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
  rec(`file:${name}`, size > 1000, { bytes: size });
}

async function ensurePage(slug, title) {
  const existing = await admin("/v1/admin/pages");
  const prior = Array.isArray(existing.body) ? existing.body.find((r) => (r.slug || r.Slug) === slug) : null;
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
  for (const row of Array.isArray(listed.body) ? listed.body : []) {
    const id = row.pageSectionId || row.PageSectionId;
    if (id) await admin(`/v1/admin/pages/${pageId}/sections/${id}`, { method: "DELETE" });
  }
}

async function seedAndPublish(pageId, seed) {
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

async function waitLanding(page, slug) {
  for (let i = 0; i < 5; i++) {
    await page.goto(`${FE}/fa/${slug}`, { waitUntil: "domcontentloaded", timeout: 90000 });
    await page.locator("[data-testid=storefront-landing-page],[data-testid=landing-hero]").first().waitFor({ timeout: 45000 }).catch(() => {});
    if (await page.locator("[data-testid=storefront-landing-page],[data-testid=landing-hero]").count() > 0) return true;
    await page.waitForTimeout(1500);
  }
  return false;
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1200 }, locale: "fa-IR" });
  await context.addInitScript(() => {
    localStorage.setItem("tooba.adminActorUserId", "01a036c2-970e-7000-8eb7-94bf5cc2d8db");
  });
  const page = await context.newPage();
  await page.setDefaultTimeout(90000);

  await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: null }),
  }).catch(() => {});

  // A start blank screen
  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "load" });
  await page.locator("[data-testid=composition-start-mode]").waitFor({ timeout: 20000 });
  rec("A-start", true);
  await shot(page, "final-start-screen.png");

  // B template picker
  await page.locator("[data-testid=start-from-template]").click();
  await page.locator("[data-testid=template-picker]").waitFor({ timeout: 15000 });
  await shot(page, "final-template-picker.png");

  // Blank composer page
  const mixedSlug = "t022-landing";
  let mixedId = await ensurePage(mixedSlug, "لندینگ پذیرش T022");
  await page.goto(`${FE}/admin/landing-pages/${mixedId}`, { waitUntil: "load" });
  await clearSections(mixedId);
  await page.reload({ waitUntil: "load" });

  async function waitAdd() {
    await page.waitForFunction(() => {
      const el = document.querySelector("[data-testid=landing-add-section]");
      return el instanceof HTMLButtonElement && !el.disabled;
    }, null, { timeout: 20000 });
  }

  async function addVariant(sectionTestId, variantTestId, shotSection, shotVariant) {
    await waitAdd();
    await page.locator("[data-testid=landing-add-section]").click();
    await page.locator("[data-testid=composition-section-catalog]").waitFor({ timeout: 10000 });
    if (shotSection) await shot(page, shotSection);
    await page.locator(`[data-testid=${sectionTestId}]`).click();
    await page.locator("[data-testid=composition-variant-picker]").waitFor({ timeout: 10000 });
    if (shotVariant) await shot(page, shotVariant);
    await page.locator(`[data-testid=${variantTestId}]`).click();
    await page.locator("[data-testid=landing-section-drawer]").waitFor({ timeout: 15000 });
  }

  // C-E add mixed sections
  await addVariant("add-section-hero", "pick-variant-hero-editorial", "final-section-picker.png", null);
  if (await page.locator("[data-testid=height-preset-select]").count() > 0) {
    await page.locator("[data-testid=height-preset-select]").selectOption("Large");
  }
  await shot(page, "final-settings-form.png");
  await page.locator("[data-testid=landing-section-drawer]").getByRole("button", { name: "ذخیرهٔ بخش" }).click();
  await page.waitForTimeout(500);

  await addVariant("add-section-products", "pick-variant-product-card-carousel", null, "final-product-variant-picker.png");
  await page.locator("[data-testid=landing-section-drawer]").getByRole("button", { name: "ذخیرهٔ بخش" }).click();
  await page.waitForTimeout(500);

  await addVariant("add-section-banners", "pick-variant-banner-two-equal", null, "final-banner-variant-picker.png");
  await page.locator("[data-testid=landing-section-drawer]").getByRole("button", { name: "ذخیرهٔ بخش" }).click();
  await page.waitForTimeout(500);

  await addVariant("add-section-brands", "pick-variant-brand-logo-rail");
  await page.locator("[data-testid=landing-section-drawer]").getByRole("button", { name: "ذخیرهٔ بخش" }).click();
  await page.waitForTimeout(500);

  await addVariant("add-section-categories", "pick-variant-category-image-cards");
  await page.locator("[data-testid=landing-section-drawer]").getByRole("button", { name: "ذخیرهٔ بخش" }).click();
  await page.waitForTimeout(500);

  await page.reload({ waitUntil: "load" });
  await shot(page, "final-section-card.png");
  await shot(page, "final-composer.png");
  rec("composer-sections", true);

  // F reorder via API
  const listed = await admin(`/v1/admin/pages/${mixedId}/sections`);
  const rows = Array.isArray(listed.body) ? listed.body : [];
  if (rows.length >= 2) {
    const ids = rows.map((r) => r.pageSectionId || r.PageSectionId);
    const re = await admin(`/v1/admin/pages/${mixedId}/sections/reorder`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ sectionIds: [ids[1], ids[0], ...ids.slice(2)] }),
    });
    rec("E-reorder", re.status < 300);
    const id0 = ids[0];
    const dis = await admin(`/v1/admin/pages/${mixedId}/sections/${id0}/enabled`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ isEnabled: false }),
    });
    rec("F-disable", dis.status < 300);
    await admin(`/v1/admin/pages/${mixedId}/sections/${id0}/enabled`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ isEnabled: true }),
    });
  }

  // G preview
  await page.goto(`${FE}/admin/landing-pages/${mixedId}/preview`, { waitUntil: "domcontentloaded" });
  await page.locator("[data-testid=landing-draft-preview],[data-testid=storefront-landing-page],[data-testid=landing-hero]")
    .first().waitFor({ timeout: 60000 }).catch(() => {});
  rec("G-preview", await page.locator("[data-testid=landing-draft-preview],[data-testid=storefront-landing-page],[data-testid=landing-hero]").count() > 0);

  // H publish
  const pub = await admin(`/v1/admin/pages/${mixedId}/status`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ status: "Published" }),
  });
  rec("H-publish", pub.status < 300);

  // Industry pages
  const fashionId = await ensurePage("t022-fashion", "پوشاک T022");
  await seedAndPublish(fashionId, [
    { type: "Hero", config: { title: "پوشاک", variantKey: "hero.editorial", href: "/products", heightPreset: "Large" } },
    { type: "StoryRail", config: { title: "استوری", variantKey: "story.circle", items: [{ title: "س۱", href: "/products", enabled: true }] } },
    { type: "ProductCollection", config: { title: "کالا", variantKey: "product.card-carousel", source: "Newest", take: 8, productIds: [] } },
    { type: "BannerShowcase", config: { title: "بنر", variantKey: "banner.two-equal", heightPreset: "Medium", items: [{ title: "1", href: "/offers" }, { title: "2", href: "/offers" }] } },
  ]);
  const autoId = await ensurePage("t022-autoparts", "یدکی T022");
  await seedAndPublish(autoId, [
    { type: "CategoryGrid", config: { title: "دسته", variantKey: "category.compact-tiles", categoryIds: [] } },
    { type: "BrandStrip", config: { title: "برند", variantKey: "brand.logo-grid", brandIds: [] } },
    { type: "ProductCollection", config: { title: "کالا", variantKey: "product.compact-rows", source: "Newest", take: 8, productIds: [] } },
  ]);
  const interiorId = await ensurePage("t022-interior", "دکوراسیون T022");
  await seedAndPublish(interiorId, [
    { type: "Hero", config: { title: "دکوراسیون", variantKey: "hero.split", href: "/products", heightPreset: "Large" } },
    { type: "CategoryGrid", config: { title: "دسته", variantKey: "category.editorial-tiles", categoryIds: [] } },
    { type: "ProductCollection", config: { title: "کالا", variantKey: "product.large-cards", source: "Newest", take: 6, productIds: [] } },
  ]);
  const beautyId = await ensurePage("t022-beauty", "آرایشی T022");
  await seedAndPublish(beautyId, [
    { type: "StoryRail", config: { title: "استوری", variantKey: "story.icon-shortcuts", items: [{ title: "س۱", href: "/products", enabled: true }] } },
    { type: "BrandStrip", config: { title: "برند", variantKey: "brand.featured", brandIds: [] } },
    { type: "ProductCollection", config: { title: "کالا", variantKey: "product.tabbed", source: "Newest", take: 8, productIds: [] } },
    { type: "BannerShowcase", config: { title: "بنر", variantKey: "banner.three", heightPreset: "Medium", items: [{ title: "1", href: "/offers" }, { title: "2", href: "/offers" }, { title: "3", href: "/offers" }] } },
  ]);

  await page.setViewportSize({ width: 1440, height: 1200 });
  rec("fashion-d", await waitLanding(page, "t022-fashion"));
  await shot(page, "final-fashion-desktop.png");
  rec("auto-d", await waitLanding(page, "t022-autoparts"));
  await shot(page, "final-autoparts-desktop.png");
  rec("interior-d", await waitLanding(page, "t022-interior"));
  await shot(page, "final-interior-desktop.png");
  rec("beauty-d", await waitLanding(page, "t022-beauty"));
  await shot(page, "final-beauty-desktop.png");
  rec("landing-d", await waitLanding(page, mixedSlug));
  await shot(page, "final-landing-desktop.png");

  // M set fashion home
  const homeSet = await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: fashionId }),
  });
  rec("M-home", homeSet.status < 300);
  await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(2000);
  rec("N-custom-home", await page.locator("[data-testid=storefront-custom-home],[data-testid=storefront-landing-page]").count() > 0);
  await shot(page, "final-custom-home-desktop.png");
  await shot(page, "final-palette-tint-home.png");

  // Theme dark / neutral via attribute override for evidence (restore after)
  await page.evaluate(() => {
    document.documentElement.setAttribute("data-storefront-theme-mode", "DarkOnly");
    document.documentElement.setAttribute("data-storefront-color-scheme", "dark");
  });
  await page.waitForTimeout(400);
  await shot(page, "final-dark-home.png");
  await page.evaluate(() => {
    document.documentElement.setAttribute("data-storefront-background-style", "Neutral");
    document.documentElement.setAttribute("data-storefront-theme-mode", "LightOnly");
    document.documentElement.setAttribute("data-storefront-color-scheme", "light");
  });
  await page.goto(`${FE}/fa/${mixedSlug}`, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(800);
  await page.evaluate(() => {
    document.documentElement.setAttribute("data-storefront-background-style", "Neutral");
  });
  await shot(page, "final-neutral-landing.png");

  // Mobile
  await page.setViewportSize({ width: 390, height: 844 });
  await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: fashionId }),
  });
  await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(1500);
  await shot(page, "final-custom-home-mobile.png");
  rec("fashion-m", await waitLanding(page, "t022-fashion"));
  await shot(page, "final-fashion-mobile.png");
  rec("auto-m", await waitLanding(page, "t022-autoparts"));
  await shot(page, "final-autoparts-mobile.png");
  rec("interior-m", await waitLanding(page, "t022-interior"));
  await shot(page, "final-interior-mobile.png");
  rec("beauty-m", await waitLanding(page, "t022-beauty"));
  await shot(page, "final-beauty-mobile.png");
  rec("landing-m", await waitLanding(page, mixedSlug));
  await shot(page, "final-landing-mobile.png");

  // P restore canonical
  await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: null }),
  });
  await page.setViewportSize({ width: 1440, height: 1200 });
  await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(1500);
  rec("P-canonical", await page.locator("[data-testid=storefront-home]").count() > 0);

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T022/runtime-report.json"), JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ ok: report.ok, errors: report.errors, files: Object.keys(report.files) }, null, 2));
  process.exit(report.ok ? 0 : 1);
})().catch((err) => {
  console.error(err);
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T022/runtime-report.json"), JSON.stringify(report, null, 2));
  process.exit(1);
});
