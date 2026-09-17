import { createRequire } from "node:module";
import { dirname, join } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const pwUrl = pathToFileURL(join(ROOT, ".tmp-r24r1r2-pw/node_modules/playwright/index.mjs")).href;
const { chromium } = await import(pwUrl);
console.log("ROOT", ROOT);
const browser = await chromium.launch({ headless: true });
const page = await (await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: "fa-IR" })).newPage();
await page.goto("http://127.0.0.1:3000/admin/landing-pages/new", { waitUntil: "domcontentloaded", timeout: 90000 });
await page.waitForTimeout(1000);
await page.locator("[data-testid=start-blank]").click();
await page.waitForSelector("[data-testid=add-first-section-cta]", { timeout: 30000 });
await page.locator("label").filter({ hasText: /^عنوان$/ }).locator("input").fill("probe");
await page.locator("[data-testid=add-first-section-cta]").click();
await page.waitForTimeout(2000);
if (await page.locator("text=امکان ذخیره نیست").count()) {
  console.log("SAVE_MODAL");
  await page.getByRole("button", { name: "بستن" }).last().click();
  await page.locator("label").filter({ hasText: /^عنوان$/ }).locator("input").fill("probe-r13");
  await page.locator("[data-testid=add-first-section-cta]").click();
}
await page.waitForSelector("[data-testid=section-wizard]", { timeout: 45000 });
await page.locator("[data-testid=add-section-hero] [data-select-choice='1']").click();
await page.locator("[data-testid=section-wizard-next]").click();
await page.waitForSelector("[data-testid=composition-variant-picker]", { timeout: 30000 });
const keys = await page.locator("[data-testid^=pick-variant-]").evaluateAll((els) => els.map((e) => e.getAttribute("data-testid")));
const design = await page.locator("[data-variant-design-name]").evaluateAll((els) =>
  els.map((e) => `${e.getAttribute("data-variant-design-name")}:${e.textContent?.trim()}`)
);
console.log(JSON.stringify({ keys, design, count: keys.length }, null, 2));
await page.screenshot({ path: join(ROOT, "docs/evidence/TB-P10-T022-R13-R1/screenshots/slider-six-variants.png") });
await browser.close();
