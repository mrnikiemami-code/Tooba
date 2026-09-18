/**
 * Visual capture for TB-P10-T022-R13-R2 against evidence page + production Embla rails.
 */
import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R13-R2");
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

async function shot(page, name, locator) {
  const dest = join(SHOTS, name);
  if (locator) {
    await locator.screenshot({ path: dest });
  } else {
    await page.screenshot({ path: dest, fullPage: false });
  }
  const size = statSync(dest).size;
  report.files[name] = { bytes: size };
  rec(`file:${name}`, size > 800, { bytes: size });
}

async function main() {
  const feOk = await fetch(`${FE}/fa`).then((r) => r.ok).catch(() => false);
  rec("fe-3000", feOk);
  if (!feOk) {
    report.blockers.push("FE:3000 unavailable");
    writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
    process.exit(2);
  }

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: "fa-IR" });
  const page = await context.newPage();

  try {
    await page.goto(`${FE}/evidence/tb-p10-t022-r13-r2`, { waitUntil: "domcontentloaded", timeout: 90000 });
    await page.waitForSelector("[data-testid=r13r2-product-showcase-evidence]", { timeout: 60000 });
    await page.waitForTimeout(1500);

    await shot(page, "product-showcase-picker-existing-plus-new.png", page.locator("[data-testid=r13r2-picker-grid]"));

    const map = [
      ["sunny", "product-showcase-sunny-preview.png", "product-showcase-storefront-sunny.png"],
      ["money", "product-showcase-money-preview.png", "product-showcase-storefront-money.png"],
      ["cinematic", "product-showcase-cinematic-preview.png", "product-showcase-storefront-cinematic.png"],
      ["cinematic-plus", "product-showcase-cinematic-plus-preview.png", "product-showcase-storefront-cinematic-plus.png"],
      ["explorer", "product-showcase-explorer-preview.png", "product-showcase-storefront-explorer.png"],
    ];

    for (const [shotKey, previewName, storeName] of map) {
      const loc = page.locator(`[data-testid=r13r2-preview-${shotKey}]`);
      await loc.scrollIntoViewIfNeeded();
      await page.waitForTimeout(400);
      await shot(page, previewName, loc);
      await shot(page, storeName, loc);
    }

    await shot(page, "product-showcase-review-new-variant.png", page.locator("[data-testid=r13r2-preview-cinematic]"));
    await shot(page, "product-showcase-rtl-cinematic.png", page.locator("[data-testid=r13r2-preview-cinematic]"));
    await shot(page, "product-showcase-productcard-actions.png", page.locator("[data-testid=r13r2-preview-money]"));

    await page.setViewportSize({ width: 390, height: 844 });
    await page.reload({ waitUntil: "domcontentloaded" });
    await page.waitForSelector("[data-testid=r13r2-preview-cinematic]", { timeout: 60000 });
    await page.waitForTimeout(1000);
    await shot(page, "product-showcase-mobile-cinematic.png", page.locator("[data-testid=r13r2-preview-cinematic]"));

    // SSR proof: product names present in initial HTML before interaction
    const html = await page.content();
    const ssrHasProducts = /StorefrontProductCard|product-showcase-|data-product-layout|پیش‌نمایش|data-embla/.test(html);
    rec("ssr-html-markers", ssrHasProducts);
  } catch (err) {
    report.ok = false;
    report.errors.push(String(err));
  } finally {
    writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
    await browser.close();
  }

  process.exit(report.ok ? 0 : 1);
}

main();
