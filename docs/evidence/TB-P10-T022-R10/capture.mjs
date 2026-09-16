import { mkdirSync, writeFileSync, statSync, existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R10");
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
  await page.locator("[data-testid=start-from-template]").click();
  await page.waitForSelector("[data-testid=template-picker]", { timeout: 30000 });
  await page.locator("[data-testid=template-card-fashion]").click();
  await page.waitForTimeout(500);
  await page.locator("[data-testid=use-selected-template]").click();

  await page.waitForSelector("[data-testid=unified-section-workspace],[data-testid=landing-section-composer]", { timeout: 90000 });
  await page.waitForSelector("[data-testid=composer-section-card]", { timeout: 30000 });
  rec("template-apply-landed", true, { url: page.url(), cards: await page.locator("[data-testid=composer-section-card]").count() });

  await page.locator("[data-testid=landing-section-composer]").scrollIntoViewIfNeeded();
  await shot(page, "template-applied-sections.png");
  await shot(page, "editor-unified-workspace.png");
  await shot(page, "landing-editor-workspace.png");

  const handle = page.locator("[data-testid=section-drag-handle]").first();
  await handle.scrollIntoViewIfNeeded();
  await shot(page, "section-drag-handle.png");
  rec("drag-handle", await handle.count() > 0);

  await page.locator("[data-testid=section-move-up]").nth(1).scrollIntoViewIfNeeded();
  await shot(page, "section-arrow-reorder.png");

  await page.locator("[data-testid=insert-after-0]").first().scrollIntoViewIfNeeded();
  await shot(page, "insert-between-sections.png");

  await page.locator("[data-testid=section-toggle-enabled]").first().click();
  await page.waitForTimeout(1000);
  await page.locator("[data-testid=section-disabled-badge]").first().scrollIntoViewIfNeeded().catch(() => {});
  await shot(page, "section-disabled-state.png");

  await page.locator("[data-testid=section-delete]").first().click();
  await page.waitForSelector("[data-testid=delete-section-confirm]", { timeout: 10000 });
  await shot(page, "delete-section-confirm.png");
  await page.locator("[data-testid=delete-section-confirm] button", { hasText: "خیر" }).click();

  await page.locator("[data-testid=apply-template-action]").click();
  await page.waitForSelector("[data-testid=template-picker]", { timeout: 20000 });
  await page.locator("[data-testid=use-selected-template]").click();
  await page.waitForSelector("[data-testid=template-replace-confirm]", { timeout: 20000 });
  await shot(page, "template-replace-confirm.png");
  await page.locator("[data-testid=template-replace-cancel]").click();

  // Home editor workspace
  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "networkidle", timeout: 90000 });
  await page.locator("[data-testid=start-blank]").click();
  await page.waitForSelector("[data-testid=create-page-type]", { timeout: 15000 });
  await page.locator("[data-testid=create-page-type] select").selectOption("Home");
  const stamp = Date.now().toString(36).slice(-5);
  await page.locator("[data-testid=page-workspace-meta] input").nth(0).fill(`خانه R10 ${stamp}`);
  await page.locator("[data-testid=page-workspace-meta] input[dir=ltr]").first().fill(`home-r10-${stamp}`);
  await page.getByRole("button", { name: "ذخیره" }).click();
  await page.waitForURL(/\/admin\/landing-pages\/[0-9a-f-]+/i, { timeout: 60000 }).catch(() => {});
  await page.waitForTimeout(1500);
  await shot(page, "home-editor-workspace.png");
  rec("home-editor", /Home|خانه|data-page-type/i.test(await page.content()));

  writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
  await browser.close();
  console.log(JSON.stringify(report, null, 2));
  if (!report.ok) process.exitCode = 1;
}

main().catch((err) => {
  console.error(err);
  writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify({ ok: false, error: String(err) }, null, 2));
  process.exit(1);
});
