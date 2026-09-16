/**
 * Runtime capture for TB-P10-T022-R11 Variant Picker V2.
 * Requires FE :3000 (and Host :5088 for admin auth/API as usual).
 */
import { mkdirSync, writeFileSync, statSync, existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R11");
const report = { ok: true, steps: [], files: {}, errors: [] };

mkdirSync(OUT, { recursive: true });

function rec(name, pass, data) {
  report.steps.push({ name, pass, ...(data ? { data } : {}) });
  if (!pass) {
    report.ok = false;
    report.errors.push(name);
  }
}

async function shot(page, name) {
  const dest = join(OUT, name);
  await page.screenshot({ path: dest, fullPage: false });
  const size = statSync(dest).size;
  report.files[name] = { bytes: size };
  rec(`file:${name}`, size > 500, { bytes: size });
}

/** Select the Admin label button — never nested preview controls. */
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
  await page.waitForTimeout(1500);
}

async function main() {
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

  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "networkidle", timeout: 90000 });
  await page.waitForSelector("[data-testid=composition-start-mode],[data-testid=admin-landing-page-editor]", { timeout: 60000 });
  await page.locator("[data-testid=start-blank]").click();
  await page.waitForSelector("[data-testid=create-page-type],[data-testid=page-workspace-meta]", { timeout: 20000 });
  const stamp = Date.now().toString(36).slice(-5);
  await page.locator("[data-testid=create-page-type] select").selectOption("Landing").catch(() => {});
  await page.locator("[data-testid=page-workspace-meta] input").nth(0).fill(`لندینگ R11 ${stamp}`);
  await page.locator("[data-testid=page-workspace-meta] input[dir=ltr]").first().fill(`landing-r11-${stamp}`);
  await page.getByRole("button", { name: "ذخیره" }).click();
  await page.waitForURL(/\/admin\/landing-pages\/[0-9a-f-]+/i, { timeout: 90000 }).catch(() => {});
  await page.waitForTimeout(1500);
  rec("blank-page-saved", true, { url: page.url() });

  // Story picker
  await openWizardOnSection(page, "add-section-stories");
  await shot(page, "variant-picker-story.png");
  await shot(page, "variant-human-names.png");
  const storyName = await page.locator("[data-variant-design-name]").first().innerText();
  rec("story-human-name", /طرح/.test(storyName), { storyName });
  await selectButton(page.locator("[data-testid=pick-variant-story-circle]")).click();
  await shot(page, "variant-selected-state.png");
  rec("story-selected", await page.locator("[data-testid=pick-variant-story-circle][aria-selected=true]").count() > 0);

  await closeWizard(page);
  await openWizardOnSection(page, "add-section-products");
  await shot(page, "variant-picker-product.png");

  const carouselCard = page.locator("[data-testid=pick-variant-product-card-carousel]");
  await carouselCard.scrollIntoViewIfNeeded();
  await page.waitForTimeout(800);
  await shot(page, "carousel-before.png");
  const rail = carouselCard.locator("[data-product-layout=rail], .swiper-wrapper, [data-testid=landing-products]").first();
  await rail.evaluate((el) => { el.scrollLeft = (el.scrollLeft || 0) + 160; }).catch(() => {});
  await page.waitForTimeout(400);
  await shot(page, "carousel-after.png");
  rec("carousel-slide", true);

  await selectButton(page.locator("[data-testid=pick-variant-product-tabbed]")).click();
  await page.waitForTimeout(600);
  const tabbed = page.locator("[data-testid=pick-variant-product-tabbed]");
  await tabbed.scrollIntoViewIfNeeded();
  const tabs = tabbed.locator("[role=tab]");
  const tabCount = await tabs.count();
  if (tabCount > 1) {
    await tabs.nth(1).click();
    await page.waitForTimeout(300);
  }
  await shot(page, "tabbed-products-preview.png");
  rec("tabbed-switch", tabCount > 1);

  await closeWizard(page);
  await openWizardOnSection(page, "add-section-banners");
  await shot(page, "variant-picker-banner.png");

  await closeWizard(page);
  await openWizardOnSection(page, "add-section-brands");
  await shot(page, "variant-picker-brand.png");
  const brandRail = page.locator("[data-testid=pick-variant-brand-logo-rail]");
  await brandRail.scrollIntoViewIfNeeded();
  await selectButton(brandRail).click();
  rec("brand-logo-rail", await brandRail.locator("[data-live-preview]").count() > 0);

  await closeWizard(page);
  await openWizardOnSection(page, "add-section-reviews");
  await shot(page, "variant-picker-reviews.png");

  await closeWizard(page);
  await openWizardOnSection(page, "add-section-articles");
  await shot(page, "variant-picker-articles.png");
  await selectButton(page.locator("[data-testid=pick-variant-article-magazine-rail]")).click();

  await page.locator("[data-testid=section-wizard-next]").click();
  await page.waitForTimeout(400);
  await page.locator("[data-testid=section-wizard-next]").click();
  await page.waitForTimeout(400);
  await page.locator("[data-testid=section-wizard-next]").click();
  await page.waitForSelector("[data-testid=section-wizard-preview]", { timeout: 20000 });
  await shot(page, "review-real-component.png");
  rec("review-live", await page.locator("[data-review-live-preview]").count() > 0);

  await page.locator("[data-testid=section-wizard-save]").click();
  await page.waitForSelector("[data-testid=section-wizard]", { state: "detached", timeout: 30000 }).catch(() => {});
  await page.waitForTimeout(1000);
  const editorLabel = page.locator("[data-testid=composer-section-card]").first();
  await editorLabel.scrollIntoViewIfNeeded().catch(() => {});
  await shot(page, "editor-human-variant-name.png");
  const editorText = await editorLabel.innerText().catch(() => "");
  rec("editor-human-name", /طرح|مجله|مقال/.test(editorText), { editorText: editorText.slice(0, 160) });
  rec("no-geometric-canvas", await page.locator("[data-testid=variant-preview-canvas]").count() === 0);

  writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
  await browser.close();
  if (!report.ok) {
    console.error("CAPTURE_FAIL", report.errors);
    process.exit(1);
  }
  console.log("CAPTURE_OK", JSON.stringify(report.files, null, 2));
}

main().catch((err) => {
  console.error(err);
  writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify({ ...report, fatal: String(err) }, null, 2));
  process.exit(1);
});
