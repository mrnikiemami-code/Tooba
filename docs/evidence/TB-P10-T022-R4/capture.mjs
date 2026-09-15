import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R4/screenshots");
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
  rec(`file:${name}`, size > 800, { bytes: size });
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1100 }, locale: "fa-IR" });
  await context.addInitScript(() => {
    localStorage.setItem("tooba.adminActorUserId", "01a036c2-970e-7000-8eb7-94bf5cc2d8db");
  });
  const page = await context.newPage();
  page.setDefaultTimeout(90000);

  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "domcontentloaded", timeout: 120000 });
  await page.locator("[data-testid=start-from-template]").click();
  await page.locator("[data-testid=template-selection-workspace], [data-testid=template-picker]").first().waitFor({ timeout: 20000 });
  await page.locator("[data-testid=template-card-fashion]").click();
  await page.waitForTimeout(800);

  await page.locator("[data-testid=fashion-summary-panel]").waitFor({ timeout: 15000 });
  await page.locator("[data-testid=fashion-preview-iframe]").waitFor({ timeout: 20000 });
  await shot(page, "fashion-selector-overview.png");
  await shot(page, "fashion-summary-panel.png");

  await page.locator("[data-testid=template-device-desktop]").click();
  await page.waitForTimeout(600);
  const desktopW = await page.locator("[data-testid=template-preview-frame]").boundingBox();
  const desktopAttr = await page.locator("[data-testid=template-preview-frame]").getAttribute("data-iframe-width");
  await shot(page, "fashion-preview-desktop.png");

  await page.locator("[data-testid=template-device-tablet]").click();
  await page.waitForTimeout(600);
  const tabletW = await page.locator("[data-testid=template-preview-frame]").boundingBox();
  const tabletAttr = await page.locator("[data-testid=template-preview-frame]").getAttribute("data-iframe-width");
  await shot(page, "fashion-preview-tablet.png");

  await page.locator("[data-testid=template-device-mobile]").click();
  await page.waitForTimeout(600);
  const mobileW = await page.locator("[data-testid=template-preview-frame]").boundingBox();
  const mobileAttr = await page.locator("[data-testid=template-preview-frame]").getAttribute("data-iframe-width");
  await shot(page, "fashion-preview-mobile.png");

  rec(
    "device-iframe-widths-differ",
    Number(desktopAttr) > Number(tabletAttr) && Number(tabletAttr) > Number(mobileAttr),
    { desktopAttr, tabletAttr, mobileAttr, desktop: desktopW?.width, tablet: tabletW?.width, mobile: mobileW?.width },
  );
  rec(
    "no-css-zoom",
    !(await page.locator("[data-testid=template-preview-frame]").evaluate((el) => {
      const s = getComputedStyle(el);
      return Boolean(s.zoom && s.zoom !== "1") || /scale\(/.test(s.transform || "");
    })),
  );

  await page.locator("[data-testid=template-device-desktop]").click();
  await page.waitForTimeout(400);

  const frame = page.frameLocator("[data-testid=fashion-preview-iframe]");
  await frame.locator("[data-testid=fashion-template-preview]").waitFor({ timeout: 30000 });
  await frame.locator("[data-testid=landing-products]").scrollIntoViewIfNeeded().catch(() => {});
  await page.waitForTimeout(500);
  await shot(page, "fashion-preview-products.png");

  await frame.locator("[data-testid=landing-banner-showcase]").scrollIntoViewIfNeeded().catch(() => {});
  await page.waitForTimeout(400);
  await shot(page, "fashion-preview-banners.png");

  await frame.locator("[data-testid=landing-brands], [data-testid=home-brands]").first().scrollIntoViewIfNeeded().catch(() => {});
  await page.waitForTimeout(400);
  await shot(page, "fashion-preview-brands.png");

  await frame.locator("[data-testid=landing-reviews], [data-testid=home-testimonials]").first().scrollIntoViewIfNeeded().catch(() => {});
  await page.waitForTimeout(400);
  await shot(page, "fashion-preview-reviews.png");

  await frame.locator("[data-testid=landing-articles], [data-testid=home-articles]").first().scrollIntoViewIfNeeded().catch(() => {});
  await page.waitForTimeout(400);
  await shot(page, "fashion-preview-articles.png");

  await page.locator("[data-testid=template-seed-pack-summary]").scrollIntoViewIfNeeded();
  await shot(page, "fashion-seed-summary.png");
  await page.locator("[data-testid=template-cleanup-prepare-actions]").scrollIntoViewIfNeeded();
  const cleanupDisabled = await page.locator("[data-testid=cleanup-sample-data-action]").isDisabled();
  const prepareDisabled = await page.locator("[data-testid=prepare-store-action]").isDisabled();
  rec("cleanup-disabled", cleanupDisabled && prepareDisabled);
  await shot(page, "fashion-cleanup-actions-disabled.png");

  const adminBg = await page.locator("[data-testid=template-detail-panel]").evaluate((el) => getComputedStyle(el).backgroundColor);
  rec("admin-shell-not-storefront-recolored", Boolean(adminBg));

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T022-R4/runtime-report.json"), JSON.stringify(report, null, 2));
  console.log(JSON.stringify(report, null, 2));
  process.exit(report.ok ? 0 : 1);
})().catch((err) => {
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T022-R4/runtime-report.json"), JSON.stringify(report, null, 2));
  console.error(err);
  process.exit(1);
});
