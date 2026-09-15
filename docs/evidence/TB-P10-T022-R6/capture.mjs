import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../..");
const out = path.join(root, "docs/evidence/TB-P10-T022-R6/screenshots");
fs.mkdirSync(out, { recursive: true });

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
await page.goto("http://127.0.0.1:3000/template-preview/fashion", {
  waitUntil: "networkidle",
  timeout: 90000,
});
await page.waitForTimeout(1500);

await page.screenshot({ path: path.join(out, "fashion-preview-desktop-r6.png"), fullPage: false });

await page.locator("text=دسته‌بندی پوشاک").first().scrollIntoViewIfNeeded();
await page.waitForTimeout(500);
await page.screenshot({ path: path.join(out, "fashion-categories-real-media.png"), fullPage: false });

await page.locator("text=منتخب پوشاک").first().scrollIntoViewIfNeeded();
await page.waitForTimeout(500);
await page.screenshot({ path: path.join(out, "fashion-products-real-media.png"), fullPage: false });

const banner = page.locator("a", { hasText: "کمپین فصل جدید" }).first();
if ((await banner.count()) > 0) {
  await banner.scrollIntoViewIfNeeded();
  await page.waitForTimeout(500);
}
await page.screenshot({ path: path.join(out, "fashion-banners-real-media.png"), fullPage: false });

await page.locator("text=برندهای محبوب").first().scrollIntoViewIfNeeded();
await page.waitForTimeout(500);
await page.screenshot({ path: path.join(out, "fashion-brands-real-media.png"), fullPage: false });

await page.evaluate(() => window.scrollTo(0, 0));
await page.waitForTimeout(400);
await page.screenshot({ path: path.join(out, "fashion-no-store-mixing-r6.png"), fullPage: false });

await page.setViewportSize({ width: 390, height: 844 });
await page.evaluate(() => window.scrollTo(0, 0));
await page.waitForTimeout(600);
await page.screenshot({ path: path.join(out, "fashion-preview-mobile-r6.png"), fullPage: false });

const purity = await page.locator("[data-template-purity]").first().getAttribute("data-template-purity");
const cat = await page.locator('[data-category-media="template"]').count();
console.log(
  JSON.stringify(
    {
      purity,
      templateCategoryMediaCount: cat,
      files: fs.readdirSync(out).map((f) => ({ f, bytes: fs.statSync(path.join(out, f)).size })),
    },
    null,
    2,
  ),
);
await browser.close();
