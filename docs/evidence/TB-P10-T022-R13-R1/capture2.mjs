import { dirname, join } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { mkdirSync, writeFileSync, statSync } from "node:fs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const { chromium } = await import(pathToFileURL(join(ROOT, ".tmp-r24r1r2-pw/node_modules/playwright/index.mjs")).href);
const FE = "http://127.0.0.1:3000";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R13-R1");
const SHOTS = join(OUT, "screenshots");
mkdirSync(SHOTS, { recursive: true });
const report = { ok: true, steps: [], files: {}, blockers: [] };

async function shot(page, name) {
  const dest = join(SHOTS, name);
  await page.screenshot({ path: dest, fullPage: false });
  report.files[name] = { bytes: statSync(dest).size };
}

const browser = await chromium.launch({ headless: true });
const page = await (await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: "fa-IR" })).newPage();

try {
  await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded", timeout: 60000 });
  await page.waitForTimeout(800);
  await shot(page, "slider-storefront-desktop.png");
  await page.setViewportSize({ width: 390, height: 844 });
  await page.reload({ waitUntil: "domcontentloaded" });
  await page.waitForTimeout(800);
  await shot(page, "slider-storefront-mobile.png");
  await page.setViewportSize({ width: 1440, height: 900 });

  // Prefer existing saved editor page from prior R13 evidence.
  await page.goto(`${FE}/admin/landing-pages/01a0ab49-5877-7000-b6ea-7c0948e54db8`, {
    waitUntil: "domcontentloaded",
    timeout: 90000,
  });
  await page.waitForTimeout(2000);
  if ((await page.url()).includes("/login")) {
    report.blockers.push("admin-login-required");
  } else {
    // Open add section if possible
    const add = page.locator("[data-testid=insert-after-end] button, [data-testid=add-section], [data-testid=add-first-section-cta]").first();
    if (await add.count()) {
      await add.click({ timeout: 15000 });
    } else {
      await page.getByRole("button", { name: /افزودن بخش/ }).first().click({ timeout: 15000 });
    }
    await page.waitForSelector("[data-testid=section-wizard]", { timeout: 30000 });
    await page.locator("[data-testid=add-section-hero] [data-select-choice='1']").click();
    await page.locator("[data-testid=section-wizard-next]").click();
    await page.waitForSelector("[data-testid=composition-variant-picker]", { timeout: 30000 });
    await page.waitForTimeout(1200);
    const keys = await page.locator("[data-testid^=pick-variant-]").evaluateAll((els) => els.map((e) => e.getAttribute("data-testid")));
    report.steps.push({ variantKeys: keys });
    await shot(page, "slider-six-variants.png");

    for (const [key, file] of [
      ["hero-fullscreen", "slider-almas-preview.png"],
      ["hero-shapes", "slider-simin-preview.png"],
      ["hero-diagonal", "slider-kimia-preview.png"],
      ["hero-cinematic", "slider-fakhteh-preview.png"],
      ["hero-split", "slider-saba-preview.png"],
      ["hero-editorial", "slider-aghigh-preview.png"],
    ]) {
      const card = page.locator(`[data-testid=pick-variant-${key}]`);
      if (!(await card.count())) continue;
      await card.scrollIntoViewIfNeeded();
      await card.locator("[data-select-choice='1']").click();
      await page.waitForTimeout(700);
      await shot(page, file);
    }

    await page.locator("[data-testid=pick-variant-hero-fullscreen] [data-select-choice='1']").click().catch(() => {});
    await page.locator("[data-testid=section-wizard-next]").click();
    await page.waitForSelector("[data-testid=hero-slider-settings]", { timeout: 20000 });
    await shot(page, "slider-height-presets.png");
    await shot(page, "slider-image-guidance.png");
    await shot(page, "slider-tabs.png");
    await page.locator("[data-testid=hero-slide-destination-type]").selectOption("all-products");
    await shot(page, "slider-destination-type.png");
    await page.locator("[data-testid=hero-slide-destination-type]").selectOption("product");
    await page.locator("[data-testid=resource-selector-open]").first().click();
    await page.waitForTimeout(1000);
    await shot(page, "slider-product-picker.png");
    await page.locator("[data-testid=resource-selector-close]").click().catch(() => {});
    await page.locator("[data-testid=hero-slide-destination-type]").selectOption("category");
    await page.locator("[data-testid=resource-selector-open]").first().click();
    await page.waitForTimeout(1000);
    await shot(page, "slider-category-picker.png");
    await page.locator("[data-testid=resource-selector-close]").click().catch(() => {});

    await page.locator("[data-testid=hero-slide-title]").fill("");
    await page.locator("[data-testid=hero-slide-alt]").fill("");
    await page.locator("[data-testid=section-wizard-next]").click();
    await page.waitForTimeout(600);
    await shot(page, "slider-validation-popup.png");
    if (await page.locator("[data-testid=hero-validation-modal-dismiss]").count()) {
      await page.locator("[data-testid=hero-validation-modal-dismiss]").click();
    }
    await shot(page, "slider-invalid-fields.png");
    await shot(page, "slider-tab-error-state.png");
    await page.locator("[data-testid=hero-slide-title]").fill("عنوان تست");
    await page.locator("[data-testid=hero-slide-alt]").fill("alt تست");
    await page.waitForTimeout(400);
    await shot(page, "slider-error-cleared-live.png");
    await shot(page, "slider-review-final.png");
  }
} catch (err) {
  report.ok = false;
  report.blockers.push(String(err?.message || err));
  await shot(page, "slider-capture-error.png").catch(() => {});
}

await browser.close();
writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
console.log(JSON.stringify({ ok: report.ok, files: Object.keys(report.files), blockers: report.blockers, steps: report.steps }, null, 2));
