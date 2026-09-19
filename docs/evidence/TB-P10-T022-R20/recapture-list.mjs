import { mkdirSync, statSync } from "node:fs";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const SHOT = "docs/evidence/TB-P10-T022-R20/screenshots";
mkdirSync(SHOT, { recursive: true });

const browser = await chromium.launch({ headless: true });
const page = await (
  await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: "fa-IR" })
).newPage();
await page.goto(`${FE}/admin`, { waitUntil: "domcontentloaded", timeout: 90000 });
await page.evaluate((a) => localStorage.setItem("tooba.adminActorUserId", a), ACTOR);
await page.goto(`${FE}/admin/campaigns`, { waitUntil: "domcontentloaded", timeout: 90000 });
await page.waitForFunction(() => !document.body.innerText.includes("در حال آماده‌سازی"), {
  timeout: 90000,
});
await page.waitForSelector("[data-testid='admin-campaigns']", { timeout: 90000 });
await page.waitForTimeout(1200);
await page.screenshot({ path: `${SHOT}/01-campaign-list.png`, fullPage: false });
console.log("list", statSync(`${SHOT}/01-campaign-list.png`).size);
await page.goto(`${FE}/admin/campaigns/new`, { waitUntil: "networkidle", timeout: 90000 });
await page.waitForFunction(() => !document.body.innerText.includes("در حال آماده‌سازی"), {
  timeout: 90000,
});
await page.waitForTimeout(1000);
await page.screenshot({ path: `${SHOT}/02-create-basic.png`, fullPage: false });
console.log("create", statSync(`${SHOT}/02-create-basic.png`).size);
await browser.close();
