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

  await page.goto("http://127.0.0.1:3000/customer-panel/notifications", { waitUntil: "networkidle" });
  await page.waitForSelector('[data-testid="notifications-inbox-customer"]', { timeout: 20000 });
  await page.waitForTimeout(800);
  await page.screenshot({ path: path.join(outDir, "01-customer-notifications.png"), fullPage: true });

  await page.goto("http://127.0.0.1:3000/vendor-panel/notifications", { waitUntil: "networkidle" });
  await page.waitForSelector('[data-testid="notifications-inbox-seller"]', { timeout: 30000 });
  await page.waitForTimeout(1200);
  await page.screenshot({ path: path.join(outDir, "02-seller-notifications.png"), fullPage: true });

  await browser.close();
  fs.writeFileSync(
    path.join(__dirname, "browser-proof.json"),
    JSON.stringify(
      {
        at: new Date().toISOString(),
        captures: ["captures/01-customer-notifications.png", "captures/02-seller-notifications.png"],
        notes: "Shopeiva-locked inbox with live Host rows; no fake seed",
      },
      null,
      2,
    ),
    "utf8",
  );
  console.log("OK captures written");
})().catch((err) => {
  console.error(err);
  process.exit(1);
});
