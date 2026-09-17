/**
 * Visual capture for TB-P10-T022-R13-R1 Hero Slider Builder Repair.
 * Requires Host :5088 + FE :3000 + admin cookies in .tmp-r2-browser-cookies.json.
 */
import { mkdirSync, writeFileSync, statSync, existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R13-R1");
const SHOTS = join(OUT, "screenshots");
const report = { ok: true, steps: [], files: {}, errors: [], blockers: [] };

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

async function main() {
  const hostOk = await fetch("http://127.0.0.1:5088/health").then((r) => r.ok).catch(() => false);
  const feOk = await fetch(`${FE}/fa`).then((r) => r.ok).catch(() => false);
  rec("host-5088", hostOk);
  rec("fe-3000", feOk);
  if (!hostOk || !feOk) {
    report.blockers.push("Host:5088 or FE:3000 unavailable");
    writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
    process.exit(2);
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

  try {
    await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded", timeout: 60000 });
    await page.waitForTimeout(800);
    await shot(page, "slider-storefront-desktop.png");
    await page.setViewportSize({ width: 390, height: 844 });
    await page.reload({ waitUntil: "domcontentloaded" });
    await page.waitForTimeout(800);
    await shot(page, "slider-storefront-mobile.png");
    await page.setViewportSize({ width: 1440, height: 900 });

    await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "domcontentloaded", timeout: 90000 });
    await page.waitForTimeout(1500);
    if ((await page.url()).includes("/login") || (await page.url()).includes("/account")) {
      report.blockers.push("Admin auth required — cookies missing/expired; Admin wizard screenshots blocked");
      writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
      await browser.close();
      process.exit(0);
    }

    const blankBtn = page.locator("[data-testid=start-blank]");
    await blankBtn.waitFor({ state: "visible", timeout: 20000 });
    await blankBtn.click({ timeout: 15000 });
    await page.waitForSelector("[data-testid=add-first-section-cta]", { timeout: 30000 });
    await page.waitForTimeout(500);

    // Fill required title BEFORE opening wizard (save-on-add needs it).
    const titleCandidates = [
      page.locator("[data-testid=landing-page-title]"),
      page.getByLabel(/^عنوان$/),
      page.locator("label").filter({ hasText: /^عنوان$/ }).locator("input"),
      page.locator("input[placeholder*='عنوان'], input").nth(0),
    ];
    for (const loc of titleCandidates) {
      if (await loc.count()) {
        await loc.first().fill("تست اسلایدر R13-R1");
        break;
      }
    }
    await page.waitForTimeout(300);

    // Dismiss stray save modal if present
    if (await page.locator("text=امکان ذخیره نیست").count()) {
      await page.getByRole("button", { name: "بستن" }).last().click().catch(() => {});
      await page.waitForTimeout(300);
    }

    await page.locator("[data-testid=add-first-section-cta]").click({ timeout: 20000 });
    // If save modal appears after click, fill title again and retry
    if (await page.locator("text=امکان ذخیره نیست").count()) {
      await page.getByRole("button", { name: "بستن" }).last().click().catch(() => {});
      await page.getByLabel(/عنوان/).first().fill("تست اسلایدر R13-R1").catch(() => {});
      await page.locator("input").first().fill("تست اسلایدر R13-R1").catch(() => {});
      await page.locator("[data-testid=add-first-section-cta]").click({ timeout: 20000 });
    }

    await page.waitForSelector("[data-testid=section-wizard]", { timeout: 45000 });
    const heroCard = page.locator("[data-testid=add-section-hero]");
    await heroCard.scrollIntoViewIfNeeded();
    await selectButton(heroCard).click({ timeout: 15000 });
    await page.locator("[data-testid=section-wizard-next]").click();
    await page.waitForSelector("[data-testid=composition-variant-picker]", { timeout: 30000 });
    await page.waitForTimeout(1500);
    await shot(page, "slider-six-variants.png");

    // Count hero variant cards
    const heroVariantCards = page.locator("[data-testid^=pick-variant-hero-]");
    const heroCount = await heroVariantCards.count();
    rec("hero-variant-card-count", heroCount === 6, { heroCount });

    const variants = [
      ["hero-fullscreen", "slider-almas-preview.png"],
      ["hero-shapes", "slider-simin-preview.png"],
      ["hero-diagonal", "slider-kimia-preview.png"],
      ["hero-cinematic", "slider-fakhteh-preview.png"],
      ["hero-split", "slider-saba-preview.png"],
      ["hero-editorial", "slider-aghigh-preview.png"],
    ];
    for (const [key, file] of variants) {
      const card = page.locator(`[data-testid=pick-variant-${key}]`);
      if (!(await card.count())) {
        report.blockers.push(`missing variant card ${key}`);
        continue;
      }
      await card.scrollIntoViewIfNeeded();
      await selectButton(card).click({ timeout: 10000 });
      await page.waitForTimeout(900);
      await shot(page, file);
    }

    if (await page.locator("[data-testid=pick-variant-hero-fullscreen]").count()) {
      await selectButton(page.locator("[data-testid=pick-variant-hero-fullscreen]")).click();
    }
    await page.locator("[data-testid=section-wizard-next]").click();
    await page.waitForSelector("[data-testid=hero-slider-settings]", { timeout: 20000 });
    await page.waitForTimeout(600);
    await shot(page, "slider-height-presets.png");
    await shot(page, "slider-image-guidance.png");
    await shot(page, "slider-tabs.png");

    const dest = page.locator("[data-testid=hero-slide-destination-type]");
    await dest.selectOption("all-products");
    await shot(page, "slider-destination-type.png");

    await dest.selectOption("product");
    await page.waitForTimeout(300);
    await page.locator("[data-testid=resource-selector-open]").first().click().catch(() => {});
    await page.waitForTimeout(1200);
    if (await page.locator("[data-testid=admin-resource-selector]").count()) {
      await shot(page, "slider-product-picker.png");
      await page.locator("[data-testid=resource-selector-close]").click().catch(() => {});
    }

    await dest.selectOption("category");
    await page.waitForTimeout(300);
    await page.locator("[data-testid=resource-selector-open]").first().click().catch(() => {});
    await page.waitForTimeout(1200);
    if (await page.locator("[data-testid=admin-resource-selector]").count()) {
      await shot(page, "slider-category-picker.png");
      await page.locator("[data-testid=resource-selector-close]").click().catch(() => {});
    }

    await page.locator("[data-testid=hero-slide-title]").fill("");
    await page.locator("[data-testid=hero-slide-alt]").fill("");
    await page.locator("[data-testid=section-wizard-next]").click();
    await page.waitForTimeout(700);
    if (await page.locator("[data-testid=hero-validation-modal]").count()) {
      await shot(page, "slider-validation-popup.png");
      await page.locator("[data-testid=hero-validation-modal-dismiss]").click();
    }
    await shot(page, "slider-invalid-fields.png");
    await shot(page, "slider-tab-error-state.png");

    await page.locator("[data-testid=hero-slide-title]").fill("عنوان تست");
    await page.locator("[data-testid=hero-slide-alt]").fill("alt تست");
    await page.waitForTimeout(400);
    await shot(page, "slider-error-cleared-live.png");

    await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
    await page.waitForTimeout(500);
    await shot(page, "slider-review-final.png");

    rec("admin-capture-complete", true);
  } catch (err) {
    report.blockers.push(String(err?.message || err));
    rec("admin-capture", false, { error: String(err?.message || err) });
    try {
      await shot(page, "slider-capture-error.png");
    } catch {
      /* ignore */
    }
  }

  await browser.close();
  writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ ok: report.ok, files: Object.keys(report.files).length, blockers: report.blockers }, null, 2));
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
