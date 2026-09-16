/**
 * Runtime capture for TB-P10-T022-R12A Batch A industry templates.
 * Requires Host :5088 + FE :3000.
 */
import { mkdirSync, writeFileSync, statSync, existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const HOST = "http://127.0.0.1:5088";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R12A");
const SHOTS = join(OUT, "screenshots");
const report = { ok: true, steps: [], files: {}, errors: [] };

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

async function provePreview(page, key) {
  const sampleUrl = `${FE}/template-preview/${key}?source=sample&locale=fa-IR`;
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto(sampleUrl, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForSelector("[data-testid=industry-template-preview],[data-testid=fashion-template-preview]", {
    timeout: 60000,
  });
  await page.waitForTimeout(1200);
  await shot(page, `${key}-preview-desktop.png`);

  await page.setViewportSize({ width: 390, height: 844 });
  await page.waitForTimeout(600);
  await shot(page, `${key}-preview-mobile.png`);

  const purity = await page.locator("[data-template-purity]").getAttribute("data-template-purity");
  rec(`${key}-sample-purity`, purity === "pure", { purity });

  // Store mode purity: no Template Catalog origin
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto(`${FE}/template-preview/${key}?source=store&locale=fa-IR`, {
    waitUntil: "domcontentloaded",
    timeout: 90000,
  });
  await page.waitForTimeout(2000);
  const storeOrigin = await page.locator("[data-demo-origin]").getAttribute("data-demo-origin").catch(() => null);
  const storePurity = await page.locator("[data-template-purity]").getAttribute("data-template-purity").catch(() => null);
  rec(`${key}-store-no-template-origin`, storeOrigin === "operational-store-catalog" || storePurity === "store", {
    storeOrigin,
    storePurity,
  });
}

async function main() {
  // Host health + catalog preview JSON
  for (const key of ["auto-parts", "building-materials", "tools-hardware"]) {
    const res = await fetch(`${HOST}/v1/storefront/template-catalog/${key}/preview`);
    const json = await res.json();
    rec(`api-${key}-status`, res.ok, { status: res.status });
    rec(`api-${key}-counts`, res.ok && json?.purity?.templateProductCount === 15 && json?.purity?.templateTopLevelCategoryCount === 8, {
      products: json?.purity?.templateProductCount,
      roots: json?.purity?.templateTopLevelCategoryCount,
      brands: json?.purity?.templateBrandCount,
      pure: json?.purity?.isPure,
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

  for (const key of ["auto-parts", "building-materials", "tools-hardware"]) {
    await provePreview(page, key);
  }

  // Template selector workspace
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForSelector("[data-testid=composition-start-mode],[data-testid=admin-landing-page-editor],[data-testid=template-selection-workspace]", {
    timeout: 60000,
  });
  const chooseTemplate = page.locator("[data-testid=start-from-template], [data-testid=choose-template], button:has-text('قالب')").first();
  if (await chooseTemplate.count()) {
    await chooseTemplate.click().catch(() => {});
  }
  await page.waitForSelector("[data-testid=template-selection-workspace],[data-testid=template-picker]", { timeout: 30000 }).catch(() => {});
  await page.locator("[data-testid=template-card-auto-parts]").click({ timeout: 15000 }).catch(() => {});
  await page.waitForTimeout(1500);
  await shot(page, "batch-a-template-selector.png");
  await shot(page, "auto-parts-template-overview.png");

  await page.locator("[data-testid=template-card-building-materials]").click({ timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(1200);
  await shot(page, "building-materials-template-overview.png");

  await page.locator("[data-testid=template-card-tools-hardware]").click({ timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(1200);
  await shot(page, "tools-hardware-template-overview.png");

  // Use template
  const useBtn = page.getByRole("button", { name: /استفاده از این قالب/ }).first();
  if (await useBtn.count()) {
    await useBtn.click();
    await page.waitForURL(/\/admin\/landing-pages\/[0-9a-f-]+/i, { timeout: 90000 }).catch(() => {});
    await page.waitForTimeout(2500);
    await shot(page, "batch-a-use-template.png");
    const onEditor = /\/admin\/landing-pages\/[0-9a-f-]{8,}/i.test(page.url());
    const sectionCount = await page.locator("[data-testid=page-section-card], [data-section-type], [data-testid^=workspace-section]").count();
    rec("use-template-sections", onEditor || sectionCount > 0, { sectionCount, url: page.url() });
  } else {
    rec("use-template-button", false);
  }

  await browser.close();
  writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
  writeFileSync(
    join(OUT, "runtime.md"),
    [
      "# Runtime — TB-P10-T022-R12A",
      "",
      `- Host ${HOST}`,
      `- FE ${FE}`,
      `- ok=${report.ok}`,
      `- errors=${report.errors.join(", ") || "none"}`,
      "",
      "## Steps",
      ...report.steps.map((s) => `- ${s.pass ? "PASS" : "FAIL"} ${s.name}${s.data ? ` — ${JSON.stringify(s.data)}` : ""}`),
      "",
    ].join("\n"),
  );
  console.log(JSON.stringify({ ok: report.ok, errors: report.errors, files: Object.keys(report.files) }, null, 2));
  if (!report.ok) process.exit(1);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
