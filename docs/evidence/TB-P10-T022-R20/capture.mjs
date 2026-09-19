/**
 * TB-P10-T022-R20 browser capture — Admin campaign workspace (dev actor; no BFF password).
 */
import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R20");
const SHOTS = join(OUT, "screenshots");
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const report = { ok: true, steps: [], files: {}, errors: [], consoleErrors: [] };

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
  rec(`shot:${name}`, size > 800, { bytes: size });
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    locale: "fa-IR",
  });
  const page = await context.newPage();
  page.on("console", (msg) => {
    if (msg.type() === "error") {
      const t = msg.text();
      if (!/401|favicon|hydration/i.test(t)) report.consoleErrors.push(t);
    }
  });

  try {
    await page.goto(`${FE}/admin`, { waitUntil: "domcontentloaded", timeout: 90000 });
    await page.evaluate((actor) => {
      localStorage.setItem("tooba.adminActorUserId", actor);
    }, ACTOR);
    rec("A-actor", true, { actor: ACTOR });

    await page.goto(`${FE}/admin/campaigns`, { waitUntil: "networkidle", timeout: 90000 });
    await page.waitForTimeout(1000);
    const listOk = (await page.locator('[data-testid="admin-campaigns"]').count()) > 0;
    rec("B-list", listOk);
    await shot(page, "01-campaign-list.png");

    await page.goto(`${FE}/admin/campaigns/new`, { waitUntil: "networkidle", timeout: 90000 });
    await page.waitForTimeout(800);
    rec("C-create", (await page.locator("h1, [data-testid='campaign-tabs']").count()) > 0);
    await shot(page, "02-create-basic.png");

    await page.goto(`${FE}/admin/campaigns`, { waitUntil: "networkidle", timeout: 90000 });
    await page.waitForTimeout(600);
    const href = await page.locator('a[href^="/admin/campaigns/"]').evaluateAll((as) => {
      const hit = as.map((a) => a.getAttribute("href")).find((h) => h && !h.endsWith("/new") && h.split("/").length > 3);
      return hit || null;
    });
    if (href) {
      await page.goto(`${FE}${href}`, { waitUntil: "networkidle", timeout: 90000 });
      await page.waitForTimeout(800);
      await shot(page, "03-workspace-info.png");
      const tabs = page.locator("[data-testid='campaign-tabs'] button, [role='tab']");
      const n = await tabs.count();
      for (let i = 0; i < n; i++) {
        const label = ((await tabs.nth(i).innerText()) || "").trim();
        await tabs.nth(i).click();
        await page.waitForTimeout(400);
        if (/کالا/.test(label)) await shot(page, "04-offer-membership.png");
        if (/قیمت/.test(label)) await shot(page, "05-pricing-ui.png");
      }
      await shot(page, "06-status.png");
      rec("D-workspace", true, { href });
    } else {
      rec("D-workspace", false, { note: "no edit href" });
    }

    await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded", timeout: 90000 });
    await page.waitForTimeout(1200);
    await shot(page, "07-storefront.png");
    rec("E-storefront", true);

    rec("W-no-console-errors", report.consoleErrors.length === 0, {
      errors: report.consoleErrors.slice(0, 8),
    });
  } catch (e) {
    report.ok = false;
    report.errors.push(String(e));
    try {
      await shot(page, "99-error.png");
    } catch {
      /* ignore */
    }
  } finally {
    await browser.close();
  }

  writeFileSync(join(OUT, "browser-capture-report.json"), JSON.stringify(report, null, 2));
  process.exit(report.ok ? 0 : 1);
}

main();
