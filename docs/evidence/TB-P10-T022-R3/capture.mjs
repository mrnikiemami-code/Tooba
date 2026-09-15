import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R3/screenshots");
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
  page.setDefaultTimeout(60000);

  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "domcontentloaded", timeout: 120000 });
  await page.locator("[data-testid=start-from-template]").click();
  await page.locator("[data-testid=template-selection-workspace], [data-testid=template-picker]").first().waitFor({ timeout: 20000 });
  await page.locator("[data-testid=template-card-fashion]").click().catch(() => {});
  await page.waitForTimeout(500);

  await page.locator("[data-testid=template-device-desktop]").click();
  await page.waitForTimeout(300);
  const desktopW = await page.locator("[data-testid=template-preview-frame]").boundingBox();
  await shot(page, "template-wireframe-desktop.png");
  await shot(page, "template-wireframe-fashion.png");

  await page.locator("[data-testid=template-device-tablet]").click();
  await page.waitForTimeout(300);
  const tabletW = await page.locator("[data-testid=template-preview-frame]").boundingBox();
  await shot(page, "template-wireframe-tablet.png");

  await page.locator("[data-testid=template-device-mobile]").click();
  await page.waitForTimeout(300);
  const mobileW = await page.locator("[data-testid=template-preview-frame]").boundingBox();
  await shot(page, "template-wireframe-mobile.png");

  rec(
    "device-frame-widths-differ",
    Boolean(desktopW && tabletW && mobileW && desktopW.width > tabletW.width && tabletW.width > mobileW.width),
    { desktop: desktopW?.width, tablet: tabletW?.width, mobile: mobileW?.width },
  );

  await page.locator("[data-testid=template-card-auto-parts]").click();
  await page.waitForTimeout(400);
  await shot(page, "template-wireframe-autoparts.png");
  const autoKind = await page.locator("[data-testid=template-wireframe-canvas]").getAttribute("data-industry");

  await page.locator("[data-testid=template-card-interior-decor]").click();
  await page.waitForTimeout(400);
  await shot(page, "template-wireframe-interior.png");
  const interiorKind = await page.locator("[data-testid=template-wireframe-canvas]").getAttribute("data-industry");
  rec("industry-previews-distinct", autoKind !== interiorKind && Boolean(autoKind) && Boolean(interiorKind), { autoKind, interiorKind });

  await page.locator("[data-testid=template-seed-pack-summary]").scrollIntoViewIfNeeded();
  await shot(page, "template-wireframe-seed-summary.png");
  await page.locator("[data-testid=template-cleanup-prepare-actions]").scrollIntoViewIfNeeded();
  await shot(page, "template-wireframe-cleanup-actions.png");

  const cleanupDisabled = await page.locator("[data-testid=cleanup-sample-data-action]").isDisabled();
  const prepareDisabled = await page.locator("[data-testid=prepare-store-action]").isDisabled();
  rec("cleanup-non-destructive", cleanupDisabled && prepareDisabled);

  await page.locator("[data-testid=start-blank-from-templates]").scrollIntoViewIfNeeded();
  await shot(page, "template-wireframe-start-blank.png");
  rec("start-blank-visible", await page.locator("[data-testid=start-blank-from-templates]").count() > 0);

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T022-R3/runtime-report.json"), JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ ok: report.ok, errors: report.errors, files: Object.keys(report.files), steps: report.steps.length }, null, 2));
  process.exit(0);
})().catch((error) => {
  report.ok = false;
  report.errors.push(String(error));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T022-R3/runtime-report.json"), JSON.stringify(report, null, 2));
  console.error(error);
  console.log(JSON.stringify({ ok: false, errors: report.errors }, null, 2));
  process.exit(0);
});
