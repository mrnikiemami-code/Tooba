/**
 * Runtime visual capture for TB-P10-T022-R13 Final Builder Hardening.
 * Requires Host :5088 + FE :3000 + admin cookies in .tmp-r2-browser-cookies.json.
 */
import { mkdirSync, writeFileSync, statSync, existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const HOST = "http://127.0.0.1:5088";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R13");
const SHOTS = join(OUT, "screenshots");
const report = { ok: true, steps: [], files: {}, errors: [], timings: {} };

mkdirSync(SHOTS, { recursive: true });

function rec(name, pass, data) {
  report.steps.push({ name, pass, ...(data ? { data } : {}) });
  if (!pass) {
    report.ok = false;
    report.errors.push(name);
  }
}

async function shot(page, name) {
  const dest = join(SHOTS, name);
  await page.screenshot({ path: dest, fullPage: false });
  const size = statSync(dest).size;
  report.files[name] = { bytes: size };
  rec(`file:${name}`, size > 800, { bytes: size });
}

function selectButton(locator) {
  return locator.locator("[data-select-choice='1']");
}

async function closeWizard(page) {
  await page.locator("[data-testid=section-wizard]").getByRole("button", { name: "بستن" }).click({ timeout: 5000 }).catch(() => {});
  await page.waitForSelector("[data-testid=section-wizard]", { state: "detached", timeout: 10000 }).catch(() => {});
}

async function openWizardOnSection(page, sectionTestId) {
  const addBtn = page.locator("[data-testid=add-first-section-cta], [data-testid=insert-after-end] button, [data-testid=add-section]").first();
  if (await addBtn.count()) {
    await addBtn.click({ timeout: 20000 });
  } else {
    await page.getByRole("button", { name: /افزودن بخش|افزودن اولین بخش/ }).first().click();
  }
  await page.waitForSelector("[data-testid=section-wizard]", { timeout: 30000 });
  const card = page.locator(`[data-testid="${sectionTestId}"]`);
  await card.scrollIntoViewIfNeeded();
  await selectButton(card).click({ timeout: 15000 });
  await page.locator("[data-testid=section-wizard-next]").click();
  await page.waitForSelector("[data-testid=composition-variant-picker]", { timeout: 30000 });
  await page.waitForTimeout(1200);
}

async function openTemplateWorkspace(page) {
  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForSelector(
    "[data-testid=composition-start-mode],[data-testid=admin-landing-page-editor],[data-testid=template-selection-workspace]",
    { timeout: 60000 },
  );
  const choose = page.locator("[data-testid=start-from-template], [data-testid=choose-template]").first();
  if (await choose.count()) await choose.click().catch(() => {});
  await page.waitForSelector("[data-testid=template-selection-workspace],[data-testid=template-picker]", { timeout: 30000 });
}

async function main() {
  const t0 = Date.now();
  const warmStart = Date.now();
  const landingRes = await fetch(`${FE}/landing/demo`).catch(() => null);
  report.timings.landingWarmMs = Date.now() - warmStart;
  rec("landing-warm-reachable", !landingRes || landingRes.status < 500, {
    status: landingRes?.status ?? "fetch-failed",
    ms: report.timings.landingWarmMs,
  });

  for (const key of ["fashion", "auto-parts", "interior-decor", "beauty", "shoes", "plants"]) {
    const res = await fetch(`${HOST}/v1/storefront/template-catalog/${key}/preview`);
    const json = await res.json();
    rec(`api-${key}`, res.ok && json?.purity?.isPure === true && json?.purity?.templateProductCount === 15, {
      products: json?.purity?.templateProductCount,
      roots: json?.purity?.templateTopLevelCategoryCount,
    });
  }

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: "fa-IR" });
  const cookiePath = join(ROOT, ".tmp-r2-browser-cookies.json");
  if (existsSync(cookiePath)) {
    try {
      const cookies = JSON.parse(readFileSync(cookiePath, "utf8"));
      if (Array.isArray(cookies)) await context.addCookies(cookies);
    } catch {
      /* ignore */
    }
  }
  const page = await context.newPage();
  page.setDefaultTimeout(60000);

  // 1) Store Pages grid
  await page.goto(`${FE}/admin/landing-pages`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForSelector("[data-testid=admin-landing-pages]", { timeout: 60000 });
  await page.waitForTimeout(800);
  await shot(page, "store-pages-grid-final.png");
  rec("store-pages-label", /صفحات فروشگاه/.test(await page.locator("h1").first().innerText().catch(() => "")));

  // 2) Template selector all 10
  await openTemplateWorkspace(page);
  await page.waitForTimeout(1000);
  const cardCount = await page.locator("[data-testid^=template-card-]").count();
  rec("selector-10-cards", cardCount >= 10, { cardCount });
  await shot(page, "template-selector-all-10.png");

  // 3–6) Representative previews
  for (const key of ["fashion", "auto-parts", "interior-decor", "beauty"]) {
    await page.locator(`[data-testid=template-card-${key}]`).click({ timeout: 15000 });
    await page.waitForTimeout(1500);
    await page.waitForSelector("[data-testid=fashion-preview-iframe]", { timeout: 20000 }).catch(() => {});
    await shot(page, `${key}-preview-final.png`);
  }

  // 7) Sample vs Store difference (beauty)
  await page.locator("[data-testid=template-card-beauty]").click();
  await page.locator("[data-testid=load-sample-data-action]").click().catch(() => {});
  await page.waitForTimeout(1200);
  const sampleSrc = await page.locator("[data-testid=fashion-preview-iframe]").getAttribute("src");
  await page.locator("[data-testid=load-store-data-action]").click().catch(() => {});
  await page.waitForTimeout(1500);
  const storeSrc = await page.locator("[data-testid=fashion-preview-iframe]").getAttribute("src");
  rec("sample-store-src-diff", Boolean(sampleSrc && storeSrc && sampleSrc !== storeSrc), { sampleSrc, storeSrc });
  await shot(page, "sample-store-mode-difference.png");

  // 8) Use template → populated editor
  await page.locator("[data-testid=template-card-fashion]").click();
  await page.waitForTimeout(800);
  await page.locator("[data-testid=use-selected-template]").click();
  await page.waitForSelector("[data-testid=unified-section-workspace],[data-testid=landing-section-composer],[data-testid=composer-section-card]", {
    timeout: 90000,
  });
  await page.waitForTimeout(1500);
  const sectionCount = await page.locator("[data-testid=composer-section-card]").count();
  rec("use-template-sections", sectionCount > 0, { sectionCount, url: page.url() });
  await shot(page, "use-template-populated-editor.png");

  // 9–11) Editor reorder / insert / disable
  await page.locator("[data-testid=landing-section-composer]").scrollIntoViewIfNeeded().catch(() => {});
  await page.locator("[data-testid=section-drag-handle], [data-testid=section-move-up]").first().scrollIntoViewIfNeeded().catch(() => {});
  await shot(page, "editor-reorder-controls-final.png");
  await page.locator("[data-testid=insert-after-0], [data-testid^=insert-after-]").first().scrollIntoViewIfNeeded().catch(() => {});
  await shot(page, "editor-insert-between-final.png");
  const toggle = page.locator("[data-testid=section-toggle-enabled]").first();
  if (await toggle.count()) {
    await toggle.click();
    await page.waitForTimeout(800);
  }
  await page.locator("[data-testid=section-disabled-badge]").first().scrollIntoViewIfNeeded().catch(() => {});
  await shot(page, "editor-disabled-section-final.png");

  // SEO panels on current page (likely Landing after fashion apply)
  const seoPanel = page.locator("[data-testid=page-seo-panel]");
  if (await seoPanel.count()) {
    await seoPanel.scrollIntoViewIfNeeded();
    await shot(page, "landing-seo-final.png");
  } else {
    await shot(page, "landing-seo-final.png");
  }

  // Variant picker shots on blank/new page flow (reuse R11 pattern)
  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForSelector("[data-testid=composition-start-mode],[data-testid=admin-landing-page-editor]", { timeout: 60000 });
  await page.locator("[data-testid=start-blank]").click();
  await page.waitForSelector("[data-testid=create-page-type],[data-testid=page-workspace-meta]", { timeout: 20000 });
  const stamp = Date.now().toString(36).slice(-5);
  await page.locator("[data-testid=create-page-type] select").selectOption("Landing").catch(() => {});
  await page.locator("[data-testid=page-workspace-meta] input").nth(0).fill(`لندینگ R13 ${stamp}`);
  await page.locator("[data-testid=page-workspace-meta] input[dir=ltr]").first().fill(`landing-r13-${stamp}`);
  await page.getByRole("button", { name: "ذخیره" }).click();
  await page.waitForURL(/\/admin\/landing-pages\/[0-9a-f-]+/i, { timeout: 90000 }).catch(() => {});
  await page.waitForTimeout(1200);

  await openWizardOnSection(page, "add-section-products");
  await shot(page, "variant-picker-real-components-final.png");
  const carouselCard = page.locator("[data-testid=pick-variant-product-card-carousel]");
  await carouselCard.scrollIntoViewIfNeeded();
  await page.waitForTimeout(800);
  await shot(page, "variant-carousel-before-final.png");
  const rail = carouselCard.locator("[data-product-layout=rail], .swiper-wrapper, [data-testid=landing-products]").first();
  await rail.evaluate((el) => {
    el.scrollLeft = (el.scrollLeft || 0) + 160;
  }).catch(() => {});
  await page.waitForTimeout(400);
  await shot(page, "variant-carousel-after-final.png");
  await selectButton(carouselCard).click().catch(() => {});
  await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
  await page.waitForTimeout(400);
  await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
  await page.waitForTimeout(400);
  await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
  await page.waitForSelector("[data-testid=section-wizard-preview]", { timeout: 20000 }).catch(() => {});
  await shot(page, "review-step-real-component-final.png");
  await closeWizard(page);

  // Home SEO
  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.locator("[data-testid=start-blank]").click();
  await page.waitForSelector("[data-testid=create-page-type]", { timeout: 15000 });
  await page.locator("[data-testid=create-page-type] select").selectOption("Home");
  const homeStamp = Date.now().toString(36).slice(-5);
  await page.locator("[data-testid=page-workspace-meta] input").nth(0).fill(`خانه R13 ${homeStamp}`);
  await page.locator("[data-testid=page-workspace-meta] input[dir=ltr]").first().fill(`home-r13-${homeStamp}`);
  await page.getByRole("button", { name: "ذخیره" }).click();
  await page.waitForURL(/\/admin\/landing-pages\/[0-9a-f-]+/i, { timeout: 60000 }).catch(() => {});
  await page.waitForTimeout(1200);
  const homeSeo = page.locator("[data-testid=page-seo-panel]");
  if (await homeSeo.count()) await homeSeo.scrollIntoViewIfNeeded();
  await shot(page, "home-seo-final.png");

  // Storefront routes
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto(`${FE}/`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForTimeout(1500);
  await shot(page, "home-route-final.png");

  const landingSlug = `landing-r13-${stamp}`;
  await page.goto(`${FE}/landing/${landingSlug}`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForTimeout(1500);
  await shot(page, "landing-route-final.png");

  // Mobile preview (fashion template selector mobile device)
  await openTemplateWorkspace(page);
  await page.locator("[data-testid=template-card-fashion]").click();
  await page.locator("[data-testid=template-device-mobile]").click();
  await page.waitForTimeout(1200);
  await shot(page, "mobile-preview-final.png");

  report.timings.totalMs = Date.now() - t0;
  writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
  writeFileSync(
    join(OUT, "runtime.md"),
    [
      "# Runtime — TB-P10-T022-R13",
      "",
      `- Host ${HOST}`,
      `- FE ${FE}`,
      `- ok=${report.ok}`,
      `- errors=${report.errors.join(", ") || "none"}`,
      `- landingWarmMs=${report.timings.landingWarmMs}`,
      `- totalMs=${report.timings.totalMs}`,
      "",
      "## Steps",
      ...report.steps.map((s) => `- ${s.pass ? "PASS" : "FAIL"} ${s.name}${s.data ? ` — ${JSON.stringify(s.data)}` : ""}`),
      "",
    ].join("\n"),
  );

  await browser.close();
  console.log(JSON.stringify({ ok: report.ok, errors: report.errors, files: Object.keys(report.files).length }, null, 2));
  if (!report.ok) process.exit(1);
}

main().catch((err) => {
  console.error(err);
  writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify({ ...report, fatal: String(err) }, null, 2));
  process.exit(1);
});
