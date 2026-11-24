const { test, expect } = require("@playwright/test");

test("C07 admin seller-offers list is visible", async ({ page }) => {
  const r = await page.goto("/admin/offers");
  expect(r && r.status()).toBe(200);
  await expect(page.locator("h1").first()).toContainText("پیشنهادهای فروشندگان");
});

test("C07 admin new-offer draft form is visible", async ({ page }) => {
  const r = await page.goto("/admin/offers/new");
  expect(r && r.status()).toBe(200);
  await expect(page.locator("h1").first()).toContainText("ایجاد پیش‌نویس برای فروشنده");
});

test("C07 seller new-offer form is visible", async ({ page }) => {
  const r = await page.goto("/vendor-panel/products/new");
  expect(r && r.status()).toBe(200);
  await expect(page.locator("h1").first()).toContainText("ایجاد پیش‌نویس پیشنهاد فروش");
});
