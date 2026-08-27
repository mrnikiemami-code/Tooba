const { chromium } = require("playwright");
const fs = require("fs");
const path = require("path");

const outDir = path.join(__dirname, "captures");
fs.mkdirSync(outDir, { recursive: true });

(async () => {
  const browser = await chromium.launch({ headless: true, channel: "msedge" });
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    locale: "fa-IR",
  });
  const page = await context.newPage();

  await page.addInitScript(() => {
    window.localStorage.setItem("tooba.customerActorUserId", "aaaaaaaa-aaaa-4aaa-8aaa-000000000009");
    window.localStorage.setItem("tooba.sellerActorUserId", "01a03628-3f68-7000-844d-99f1cadb54b0");
    window.localStorage.setItem("tooba.sellerPartyId", "01a030d1-40cb-7000-8abe-6d31739956c5");
  });

  // Tooba customer
  await page.goto("http://127.0.0.1:3000/customer-panel/notifications", { waitUntil: "domcontentloaded", timeout: 60000 });
  await page.waitForSelector('[data-testid="notifications-inbox-customer"], [data-testid="notifications-inbox-customer-loading"]', { timeout: 30000 });
  await page.waitForSelector('[data-testid="notifications-inbox-customer"]', { timeout: 30000 });
  await page.waitForTimeout(800);
  await page.screenshot({ path: path.join(outDir, "01-tooba-customer-notifications.png"), fullPage: true });

  // Trigger toast: mark all or mark one if unread button exists
  const markAll = page.locator('[data-testid="notifications-mark-all-read"]');
  if (await markAll.count()) {
    await markAll.first().click();
    await page.waitForTimeout(1200);
    await page.screenshot({ path: path.join(outDir, "01b-tooba-customer-toast.png"), fullPage: true });
  }

  // Tooba seller
  await page.goto("http://127.0.0.1:3000/vendor-panel/notifications", { waitUntil: "domcontentloaded", timeout: 60000 });
  await page.waitForSelector('[data-testid="notifications-inbox-seller"], [data-testid="notifications-inbox-seller-loading"], [data-testid="vendor-shell-loading"]', { timeout: 45000 });
  await page.waitForSelector('[data-testid="notifications-inbox-seller"]', { timeout: 45000 });
  await page.waitForTimeout(1000);
  await page.screenshot({ path: path.join(outDir, "02-tooba-seller-notifications.png"), fullPage: true });

  // Shopeiva customer notifications
  await page.goto("http://127.0.0.1:3001/user-panel/notifications", { waitUntil: "domcontentloaded", timeout: 60000 });
  await page.waitForTimeout(2000);
  await page.screenshot({ path: path.join(outDir, "03-shopeiva-user-notifications.png"), fullPage: true });

  // Mobile width comparison
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("http://127.0.0.1:3000/customer-panel/notifications", { waitUntil: "domcontentloaded", timeout: 60000 });
  await page.waitForSelector('[data-testid="notifications-inbox-customer"]', { timeout: 30000 });
  await page.waitForTimeout(500);
  await page.screenshot({ path: path.join(outDir, "04-tooba-customer-mobile.png"), fullPage: true });
  await page.goto("http://127.0.0.1:3001/user-panel/notifications", { waitUntil: "domcontentloaded", timeout: 60000 });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(outDir, "05-shopeiva-user-mobile.png"), fullPage: true });

  // Shopeiva vendor root (no dedicated notifications route)
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("http://127.0.0.1:3001/vendor-panel", { waitUntil: "domcontentloaded", timeout: 60000 });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(outDir, "06-shopeiva-vendor-panel-root.png"), fullPage: true });

  await browser.close();
  fs.writeFileSync(
    path.join(__dirname, "browser-proof.json"),
    JSON.stringify(
      {
        at: new Date().toISOString(),
        shopeivaUserNotifications: "http://127.0.0.1:3001/user-panel/notifications",
        shopeivaVendorNotifications: "ABSENT (404); vendor panel root captured",
        captures: fs.readdirSync(outDir),
      },
      null,
      2,
    ),
    "utf8",
  );
  console.log("OK");
})().catch((e) => {
  console.error(e);
  process.exit(1);
});
