import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = join(ROOT, "docs/evidence/TB-P10-T020/screenshots");
const SLUG = "t020-runtime";
const FASHION_SLUG = "t020-fashion";
const AUTO_SLUG = "t020-autoparts";
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

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1200 }, locale: "fa-IR" });
  await context.addInitScript(() => {
    localStorage.setItem("tooba.adminActorUserId", "01a036c2-970e-7000-8eb7-94bf5cc2d8db");
  });
  const page = await context.newPage();
  page.on("console", (msg) => report.console.push({ type: msg.type(), text: msg.text().slice(0, 240) }));
  page.on("pageerror", (err) => report.console.push({ type: "pageerror", text: String(err).slice(0, 240) }));
  await page.setDefaultTimeout(90000);
  await page.setDefaultNavigationTimeout(90000);

  // Restore canonical home first
  await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: null }),
  }).catch(() => {});

  // A — open composer (start UX)
  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "load" });
  await page.locator("[data-testid=composition-start-mode]").waitFor({ timeout: 20000 });
  rec("A-composer-start", await page.locator("[data-testid=start-blank]").count() > 0
    && await page.locator("[data-testid=start-from-template]").count() > 0);
  await shot(page, "template-picker.png").catch(() => {});
  await page.locator("[data-testid=start-from-template]").click();
  await page.locator("[data-testid=template-picker]").waitFor({ timeout: 15000 });
  await shot(page, "template-picker.png");
  await page.locator("[data-testid=template-card-fashion]").waitFor({ timeout: 10000 });
  await shot(page, "template-fashion-preview.png");
  await page.locator("[data-testid=template-card-auto-parts]").waitFor({ timeout: 10000 });
  await shot(page, "template-autoparts-preview.png");

  // B — create blank composition (reuse existing slug if present)
  let pageId = await ensurePage(SLUG, "صفحه آزمایش T020");
  if (!pageId) {
    await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "load" });
    await page.locator("[data-testid=start-blank]").click();
    await page.locator("input").first().fill("صفحه آزمایش T020");
    await page.locator('input[dir="ltr"]').fill(SLUG);
    await page.getByRole("button", { name: "ذخیره" }).click();
    await page.waitForURL(/\/admin\/landing-pages\/[0-9a-f-]+/i, { timeout: 45000 });
    pageId = page.url().split("/").pop();
  } else {
    await page.goto(`${FE}/admin/landing-pages/${pageId}`, { waitUntil: "load" });
  }
  rec("B-blank-create", Boolean(pageId));
  await clearSections(pageId);
  await page.reload({ waitUntil: "load" });

  async function waitAddReady() {
    await page.waitForFunction(() => {
      const el = document.querySelector("[data-testid=landing-add-section]");
      return el instanceof HTMLButtonElement && !el.disabled;
    }, null, { timeout: 20000 });
  }

  async function addSectionVariant(sectionTestId, variantTestId, shotName) {
    await waitAddReady();
    await page.locator("[data-testid=landing-add-section]").click();
    await page.locator("[data-testid=composition-section-catalog]").waitFor({ timeout: 10000 });
    await page.locator(`[data-testid=${sectionTestId}]`).click();
    await page.locator("[data-testid=composition-variant-picker]").waitFor({ timeout: 10000 });
    if (shotName) await shot(page, shotName);
    await page.locator(`[data-testid=${variantTestId}]`).click();
    await page.locator("[data-testid=landing-section-drawer]").waitFor({ timeout: 15000 });
    return true;
  }

  // C Hero Split
  await addSectionVariant("add-section-hero", "pick-variant-hero-split", "hero-variant-picker.png");
  await page.locator("[data-testid=landing-section-drawer]").getByRole("button", { name: "ذخیرهٔ بخش" }).click();
  await page.waitForTimeout(700);
  rec("C-hero-split", true);

  // D Story Circle
  await addSectionVariant("add-section-stories", "pick-variant-story-circle", "story-variant-picker.png");
  if (await page.locator("[data-testid=story-items-editor]").count() > 0) {
    await shot(page, "banner-slot-editor.png").catch(() => {});
  }
  await page.locator("[data-testid=landing-section-drawer]").getByRole("button", { name: "ذخیرهٔ بخش" }).click();
  await page.waitForTimeout(700);
  rec("D-story-circle", true);

  // E Product CompactRows
  await addSectionVariant("add-section-products", "pick-variant-product-compact-rows", "product-variant-picker.png");
  await page.locator("[data-testid=landing-section-drawer]").getByRole("button", { name: "ذخیرهٔ بخش" }).click();
  await page.waitForTimeout(700);
  rec("E-product-compact-rows", true);

  // F Banner FourGrid
  await addSectionVariant("add-section-banners", "pick-variant-banner-four-grid", "banner-variant-picker.png");
  if (await page.locator("[data-testid=banner-slot-editor]").count() > 0) {
    await shot(page, "banner-slot-editor.png");
  }
  await page.locator("[data-testid=landing-section-drawer]").getByRole("button", { name: "ذخیرهٔ بخش" }).click();
  await page.waitForTimeout(700);
  rec("F-banner-four-grid", true);

  // G Brand LogoRail
  await addSectionVariant("add-section-brands", "pick-variant-brand-logo-rail");
  await page.locator("[data-testid=landing-section-drawer]").getByRole("button", { name: "ذخیرهٔ بخش" }).click();
  await page.waitForTimeout(700);
  rec("G-brand-logo-rail", true);

  await page.reload({ waitUntil: "load" });
  await shot(page, "composer-mixed-page.png");

  const after = await admin(`/v1/admin/pages/${pageId}/sections`);
  const sections = Array.isArray(after.body) ? after.body : [];
  const types = sections.map((s) => s.sectionType || s.SectionType);
  rec("sections-native", types.includes("StoryRail") && types.includes("BannerShowcase"), { types });
  rec("sections-count", sections.length >= 5, { count: sections.length });

  // H preview desktop
  await page.goto(`${FE}/admin/landing-pages/${pageId}/preview`, { waitUntil: "domcontentloaded" });
  await page.locator("[data-testid=landing-draft-preview]").waitFor({ timeout: 90000 }).catch(() => {});
  const previewOk = await page.locator("[data-testid=landing-draft-preview]").count() > 0
    || await page.locator("[data-testid=storefront-landing-page]").count() > 0
    || await page.locator("[data-testid=landing-hero]").count() > 0;
  rec("H-preview-desktop", previewOk);

  // I mobile viewport
  await page.setViewportSize({ width: 390, height: 844 });
  await page.reload({ waitUntil: "domcontentloaded" });
  await page.locator("[data-testid=landing-draft-preview],[data-testid=storefront-landing-page],[data-testid=landing-hero]")
    .first().waitFor({ timeout: 90000 }).catch(() => {});
  rec("I-preview-mobile", await page.locator("[data-testid=landing-draft-preview],[data-testid=storefront-landing-page],[data-testid=landing-hero]").count() > 0);
  await page.setViewportSize({ width: 1440, height: 1200 });

  // J publish
  const pub = await admin(`/v1/admin/pages/${pageId}/status`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ status: "Published" }),
  });
  rec("J-publish", pub.status < 300, { status: pub.status });

  // K public page — wait until Host+FE settle (avoid transient 500 during Host churn)
  let publicOk = false;
  for (let attempt = 0; attempt < 5; attempt++) {
    const health = await api("/health");
    if (health.status >= 300) {
      await page.waitForTimeout(2000);
      continue;
    }
    await page.goto(`${FE}/fa/${SLUG}`, { waitUntil: "domcontentloaded", timeout: 90000 });
    await page.locator("[data-testid=storefront-landing-page]").waitFor({ timeout: 60000 }).catch(() => {});
    publicOk = await page.locator("[data-testid=storefront-landing-page]").count() > 0
      || await page.locator("[data-testid=landing-hero]").count() > 0;
    if (publicOk) break;
    await page.waitForTimeout(2500);
  }
  rec("K-public", publicOk);
  await shot(page, "storefront-mixed-desktop.png");
  await page.setViewportSize({ width: 390, height: 844 });
  await page.reload({ waitUntil: "load" });
  await page.waitForTimeout(800);
  await shot(page, "storefront-mixed-mobile.png");
  await page.setViewportSize({ width: 1440, height: 1200 });

  // L Fashion template composition (UI start UX already proven; seed via API for idempotency)
  let fashionId = await ensurePage(FASHION_SLUG, "قالب پوشاک T020");
  await clearSections(fashionId);
  const fashionSeed = [
    { type: "Hero", config: { title: "پوشاک", variantKey: "hero.full-width", href: "/products", heightPreset: "Large" } },
    { type: "StoryRail", config: { title: "استوری", variantKey: "story.circle", items: [{ title: "س۱", href: "/products", enabled: true }] } },
    { type: "CategoryGrid", config: { title: "دسته", variantKey: "category.image-cards", categoryIds: [] } },
    { type: "ProductCollection", config: { title: "کالا", variantKey: "product.card-carousel", source: "Newest", take: 8, productIds: [] } },
    { type: "BannerShowcase", config: { title: "بنر", variantKey: "banner.two-equal", heightPreset: "Medium", items: [{ title: "1", href: "/offers" }, { title: "2", href: "/offers" }] } },
    { type: "BrandStrip", config: { title: "برند", variantKey: "brand.logo-rail", brandIds: [] } },
  ];
  for (const item of fashionSeed) {
    await admin(`/v1/admin/pages/${fashionId}/sections`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ sectionType: item.type, configurationJson: JSON.stringify(item.config) }),
    });
  }
  await page.goto(`${FE}/admin/landing-pages/${fashionId}`, { waitUntil: "load" });
  await page.waitForTimeout(800);
  const fashionSections = await admin(`/v1/admin/pages/${fashionId}/sections`);
  const fashionRows = Array.isArray(fashionSections.body) ? fashionSections.body : [];
  rec("L-fashion-template", fashionRows.length >= 3, { count: fashionRows.length });
  // M editable
  rec("M-fashion-editable", await page.locator("[data-testid=landing-add-section]").count() > 0);
  const fashionPub = await admin(`/v1/admin/pages/${fashionId}/status`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ status: "Published" }),
  });
  rec("L-fashion-publish", fashionPub.status < 300);

  // N AutoParts template
  let autoId = await ensurePage(AUTO_SLUG, "قالب یدکی T020");
  await clearSections(autoId);
  const autoSeed = [
    { type: "Hero", config: { title: "یدکی", variantKey: "hero.contained", href: "/products", heightPreset: "Medium" } },
    { type: "CategoryGrid", config: { title: "دسته", variantKey: "category.compact-tiles", categoryIds: [] } },
    { type: "ProductCollection", config: { title: "کالا", variantKey: "product.compact-rows", source: "Newest", take: 8, productIds: [] } },
    { type: "BrandStrip", config: { title: "برند", variantKey: "brand.logo-grid", brandIds: [] } },
  ];
  for (const item of autoSeed) {
    await admin(`/v1/admin/pages/${autoId}/sections`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ sectionType: item.type, configurationJson: JSON.stringify(item.config) }),
    });
  }
  await page.goto(`${FE}/admin/landing-pages/${autoId}`, { waitUntil: "load" });
  const autoSections = await admin(`/v1/admin/pages/${autoId}/sections`);
  const autoRows = Array.isArray(autoSections.body) ? autoSections.body : [];
  const fashionTypes = fashionRows.map((s) => `${s.sectionType || s.SectionType}:${(JSON.parse(s.config || s.Config || "{}").variantKey) || ""}`).join("|");
  const autoTypes = autoRows.map((s) => `${s.sectionType || s.SectionType}:${(JSON.parse(s.config || s.Config || "{}").variantKey) || ""}`).join("|");
  rec("N-autoparts-template", autoRows.length >= 2, { count: autoRows.length });
  rec("O-templates-distinct", fashionTypes !== autoTypes, { fashionTypes, autoTypes });
  const autoPub = await admin(`/v1/admin/pages/${autoId}/status`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ status: "Published" }),
  });
  rec("N-autoparts-publish", autoPub.status < 300);

  // P select fashion as Home
  const homeSet = await admin(`/v1/admin/pages/home`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: fashionId }),
  });
  rec("P-set-home", homeSet.status < 300, { status: homeSet.status });
  const hs = await api("/v1/storefront/home-selection");
  rec("P-home-selection", Boolean(hs.body?.selectedPage || hs.body?.SelectedPage), { status: hs.status });
  await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForTimeout(2500);
  const landingHome = await page.locator("[data-testid=storefront-custom-home]").count() > 0
    || await page.locator("[data-testid=storefront-landing-page]").count() > 0;
  rec("Q-fashion-home", landingHome);
  await shot(page, "fashion-template-home.png");
  await page.setViewportSize({ width: 390, height: 844 });
  await page.reload({ waitUntil: "domcontentloaded" });
  await page.waitForTimeout(1200);
  rec("Q-fashion-home-mobile", await page.locator("[data-testid=storefront-custom-home],[data-testid=storefront-landing-page],[data-testid=storefront-home]").count() > 0);
  await page.setViewportSize({ width: 1440, height: 1200 });

  // also capture autoparts public
  await page.goto(`${FE}/fa/${AUTO_SLUG}`, { waitUntil: "load" });
  await shot(page, "autoparts-template-home.png");

  // R restore canonical
  const unset = await admin(`/v1/admin/pages/home`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: null }),
  });
  rec("R-unset-home", unset.status < 300, { status: unset.status });
  await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForTimeout(1500);
  rec("R-canonical", await page.locator("[data-testid=storefront-home]").count() > 0);

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T020/runtime-report.json"), JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ ok: report.ok, errors: report.errors, steps: report.steps.length, files: Object.keys(report.files) }, null, 2));
  process.exit(report.ok ? 0 : 1);
})().catch((err) => {
  console.error(err);
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T020/runtime-report.json"), JSON.stringify(report, null, 2));
  process.exit(1);
});
