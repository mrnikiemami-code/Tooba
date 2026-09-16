/**
 * Runtime visual capture for TB-P11-T001 Admin gap audit.
 * Requires Host :5088 + FE :3000 + admin cookies in .tmp-r2-browser-cookies.json.
 */
import { mkdirSync, writeFileSync, statSync, existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const HOST = "http://127.0.0.1:5088";
const OUT = join(ROOT, "docs/evidence/TB-P11-T001");
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

async function gotoAdmin(page, path) {
  for (let i = 0; i < 4; i++) {
    try {
      await page.goto(`${FE}${path}`, { waitUntil: "commit", timeout: 90000 });
      await page.waitForSelector("[data-testid=admin-panel-shell],[data-testid=admin-shell-loading]", { timeout: 30000 });
      await page.waitForSelector("[data-testid=admin-panel-shell]", { timeout: 60000 });
      await page.getByText("در حال بارگذاری...").waitFor({ state: "hidden", timeout: 20000 }).catch(() => {});
      await page.waitForTimeout(800);
      return;
    } catch (err) {
      if (i === 3) throw err;
      await page.waitForTimeout(2000);
    }
  }
}

async function main() {
  const hostHealth = await fetch(`${HOST}/health`).then((r) => r.ok).catch(() => false);
  rec("host-health", hostHealth);
  const feHealth = await fetch(`${FE}/fa`).then((r) => r.status < 500).catch(() => false);
  rec("fe-reachable", feHealth);

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

  await gotoAdmin(page, "/admin");
  rec("dashboard-not-login", !(await page.url()).includes("/login") && !(await page.url()).includes("/account"));
  await shot(page, "admin-dashboard-current.png");

  await gotoAdmin(page, "/admin/orders");
  await shot(page, "admin-orders-canonical-grid.png");

  await gotoAdmin(page, "/admin/products");
  await shot(page, "admin-products-current.png");

  await gotoAdmin(page, "/admin/catalog/categories");
  await shot(page, "admin-categories-current.png");

  await gotoAdmin(page, "/admin/products");
  await shot(page, "admin-brands-current.png");

  await gotoAdmin(page, "/admin/access-control");
  await shot(page, "admin-access-control-current.png");

  await gotoAdmin(page, "/admin/settings");
  await shot(page, "admin-store-settings-current.png");

  await gotoAdmin(page, "/admin/landing-pages");
  await shot(page, "admin-store-pages-current.png");

  await gotoAdmin(page, "/admin/settings");
  await page.getByRole("button", { name: "ظاهر فروشگاه" }).click().catch(() => {});
  await page.waitForTimeout(800);
  await shot(page, "admin-appearance-current.png");

  await gotoAdmin(page, "/admin/sellers");
  await shot(page, "admin-edition-boundary-current.png");

  await browser.close();
  writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
  process.exit(report.ok ? 0 : 1);
}

main().catch((err) => {
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
  console.error(err);
  process.exit(1);
});
